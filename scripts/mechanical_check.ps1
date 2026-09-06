#Requires -Version 5.1
# SPDX-AI-Disclosure: ai-generated
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
$root = Split-Path -Parent $PSScriptRoot

$results = [System.Collections.Generic.List[string]]::new()

# HEAD commit timestamp
Push-Location $root
$headUnix = 0
$headTimeStr = "unknown"
try {
    $headUnix = [int](& git log -1 --format=%ct 2>$null)
    $headTimeStr = (& git log -1 --format=%cd --date=iso-strict 2>$null).Trim()
} finally { Pop-Location }

# 1. Build
Push-Location $root
try {
    $buildOutput = & dotnet build Game/Game.sln -v q --nologo 2>&1
    $exitCode = $LASTEXITCODE
    $warningCount = 0
    $errorCount = 0
    foreach ($line in $buildOutput) {
        if ($line -match '(\d+)\s*(?:個の警告|Warning\(s\))' -or $line -match '^\s*(\d+)\s+.*(?:警告|Warning)') {
            $warningCount = [int]$matches[1]
        }
        if ($line -match '(\d+)\s*(?:エラー|Error\(s\))' -or $line -match '^\s*(\d+)\s+.*(?:エラー|Error)') {
            $errorCount = [int]$matches[1]
        }
    }
    if ($warningCount -eq 0) {
        $warningCount = ($buildOutput | Where-Object { $_ -match 'warning\s+MSB' }).Count
    }

    if ($exitCode -eq 0 -and $errorCount -eq 0) {
        $results.Add("PASS Build: 0 errors (warnings: ${warningCount})")
    } else {
        $results.Add("FAIL Build: ${errorCount} errors (warnings: ${warningCount})")
    }
} catch {
    $results.Add("FAIL Build: Exception ($_)")
} finally { Pop-Location }

# 2. Changed lines (git diff --numstat HEAD -- '*.cs')
Push-Location $root
try {
    $numstat = & git diff --numstat HEAD -- '*.cs' 2>&1
    $added = 0
    $deleted = 0
    foreach ($line in $numstat) {
        if ($line -match '^(\d+)\s+(\d+)\s+') {
            $added += [int]$matches[1]
            $deleted += [int]$matches[2]
        }
    }
    if ($added -le 300) {
        $results.Add("PASS Changed lines: ${added} added lines (deleted ${deleted} <= 300)")
    } else {
        $results.Add("FAIL Changed lines: ${added} added lines (deleted ${deleted} > 300)")
    }
} catch {
    $results.Add("FAIL Changed lines: Exception ($_)")
} finally { Pop-Location }

# 3. Protected files
Push-Location $root
try {
    $statusLines = & git status --porcelain 2>&1
    $protectedPatterns = @(
        '^docs/spec/',
        '^docs/decisions/',
        '^\.agents/rules/development-rules\.md$',
        '^\.claude/hooks/guard\.js$',
        '^\.github/workflows/',
        '^scripts/nightly_gate\.py$',
        '^scripts/auto_runner\.py$',
        '^Game/Packages/manifest\.json$',
        '^\.mcp\.json$'
    )
    $violated = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $statusLines) {
        if ($line.Length -ge 4) {
            $filePath = $line.Substring(3).Trim()
            if ($filePath -match '->\s+(.+)$') { $filePath = $matches[1] }
            $filePath = $filePath -replace '\\', '/'
            foreach ($pat in $protectedPatterns) {
                if ($filePath -match $pat) {
                    $violated.Add($filePath)
                    break
                }
            }
        }
    }
    if ($violated.Count -eq 0) {
        $results.Add("PASS Protected files: None modified (0)")
    } else {
        $results.Add("FAIL Protected files: Violations found ($($violated -join ', '))")
    }
} catch {
    $results.Add("FAIL Protected files: Exception ($_)")
} finally { Pop-Location }

# 4. Test count (logs/*_results.xml)
function Parse-TestRunAttributes([string]$filePath) {
    if (-not (Test-Path $filePath)) { return $null }
    $fileItem = Get-Item $filePath
    $mtimeUnix = [int]([DateTimeOffset]$fileItem.LastWriteTimeUtc).ToUnixTimeSeconds()
    $mtimeIso = $fileItem.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
    $header = (Get-Content $filePath -TotalCount 5) -join " "
    if ($header -match '<test-run\b([^>]+)>') {
        $attrs = $matches[1]
        $total = if ($attrs -match '\btotal="(\d+)"') { $matches[1] } else { "?" }
        $passed = if ($attrs -match '\bpassed="(\d+)"') { $matches[1] } else { "?" }
        $failed = if ($attrs -match '\bfailed="(\d+)"') { $matches[1] } else { "?" }
        $skipped = if ($attrs -match '\bskipped="(\d+)"') { $matches[1] } else { "?" }
        return @{
            Total = $total
            Passed = $passed
            Failed = $failed
            Skipped = $skipped
            MtimeUnix = $mtimeUnix
            MtimeIso = $mtimeIso
        }
    }
    return $null
}

$editXml = Join-Path $root 'logs/editmode_results.xml'
$playXml = Join-Path $root 'logs/playmode_results.xml'

$editRes = Parse-TestRunAttributes $editXml
$playRes = Parse-TestRunAttributes $playXml

if ($null -eq $editRes -and $null -eq $playRes) {
    $results.Add("SKIP Tests: logs/*_results.xml not found")
} else {
    $testParts = @()
    $hasStale = $false
    $hasFail = $false

    if ($editRes) {
        $isStale = ($headUnix -gt 0 -and $editRes.MtimeUnix -lt $headUnix)
        if ($isStale) { $hasStale = $true }
        $staleMark = if ($isStale) { " [STALE mtime: $($editRes.MtimeIso) < HEAD: $headTimeStr]" } else { " [mtime: $($editRes.MtimeIso)]" }
        $testParts += "EditMode: total=$($editRes.Total) passed=$($editRes.Passed) failed=$($editRes.Failed) skipped=$($editRes.Skipped)${staleMark}"
        if ($editRes.Failed -ne "?" -and [int]$editRes.Failed -gt 0) { $hasFail = $true }
    } else {
        $testParts += "EditMode: not found"
    }

    if ($playRes) {
        $isStale = ($headUnix -gt 0 -and $playRes.MtimeUnix -lt $headUnix)
        if ($isStale) { $hasStale = $true }
        $staleMark = if ($isStale) { " [STALE mtime: $($playRes.MtimeIso) < HEAD: $headTimeStr]" } else { " [mtime: $($playRes.MtimeIso)]" }
        $testParts += "PlayMode: total=$($playRes.Total) passed=$($playRes.Passed) failed=$($playRes.Failed) skipped=$($playRes.Skipped)${staleMark}"
        if ($playRes.Failed -ne "?" -and [int]$playRes.Failed -gt 0) { $hasFail = $true }
    } else {
        $testParts += "PlayMode: not found"
    }

    $status = if ($hasFail) { "FAIL" } elseif ($hasStale) { "STALE" } else { "PASS" }
    $results.Add("${status} Tests: $($testParts -join ' / ')")
}

# 5. Snapshot (docs/snapshot/scene_bindings.txt diff)
$snapPath = Join-Path $root 'docs/snapshot/scene_bindings.txt'
if (-not (Test-Path $snapPath)) {
    $results.Add("SKIP Snapshot: docs/snapshot/scene_bindings.txt not found")
} else {
    Push-Location $root
    try {
        $snapDiff = (& git diff --stat HEAD -- docs/snapshot/scene_bindings.txt 2>&1 | Out-String).Trim()
        if (-not $snapDiff) {
            $results.Add("PASS Snapshot: docs/snapshot/scene_bindings.txt no diff (0 lines)")
        } else {
            $results.Add("SKIP Snapshot: docs/snapshot/scene_bindings.txt diff ($snapDiff)")
        }
    } catch {
        $results.Add("FAIL Snapshot: Exception ($_)")
    } finally { Pop-Location }
}

# Output
foreach ($r in $results) {
    Write-Output $r
}
