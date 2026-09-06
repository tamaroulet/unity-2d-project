[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$mapPath = "docs/rules-migration-map.md"
if (-not (Test-Path $mapPath)) {
    $mapPath = Join-Path $PSScriptRoot "../docs/rules-migration-map.md"
}

if (-not (Test-Path $mapPath)) {
    Write-Error "対応表が見つかりません: $mapPath"
    exit 1
}

$lines = [System.IO.File]::ReadAllLines((Resolve-Path $mapPath).Path, [System.Text.Encoding]::UTF8)

$failedCount = 0
$checkedCount = 0

foreach ($line in $lines) {
    $trimmed = $line.Trim()
    if (-not $trimmed.StartsWith("|")) {
        continue
    }

    $cols = $trimmed.Split('|')
    if ($cols.Length -lt 4) {
        continue
    }

    $section = $cols[1].Trim()
    $filePath = $cols[2].Trim()
    $term = $cols[3].Trim()

    if ($section -eq "節" -or $section -eq "" -or $section.StartsWith("-")) {
        continue
    }
    if ($filePath.StartsWith("-") -or $term.StartsWith("-")) {
        continue
    }

    $checkedCount++

    if (-not (Test-Path $filePath)) {
        Write-Host "FAIL: ファイルが存在しません: $filePath (節: $section)"
        $failedCount++
        continue
    }

    $content = [System.IO.File]::ReadAllText((Resolve-Path $filePath).Path, [System.Text.Encoding]::UTF8)
    if ($content.Contains($term)) {
        Write-Host "PASS: [$section] in $filePath -> '$term'"
    } else {
        Write-Host "FAIL: [$section] in $filePath -> '$term' (見つかりません)"
        $failedCount++
    }
}

Write-Host "検査終了: $checkedCount 件中 $($checkedCount - $failedCount) 件成功, $failedCount 件失敗"

if ($failedCount -gt 0 -or $checkedCount -eq 0) {
    exit 1
}

exit 0
