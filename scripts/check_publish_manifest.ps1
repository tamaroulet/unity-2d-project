#Requires -Version 5.1
# scripts/check_publish_manifest.ps1
# 公開マニフェスト（docs/publish-manifest.md）の照合検査

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = (Get-Location).Path
if ($PSScriptRoot) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
}

$manifestPath = Join-Path $repoRoot "docs/publish-manifest.md"
if (-not (Test-Path $manifestPath)) {
    Write-Error "publish-manifest.md が見つかりません: $manifestPath"
    exit 1
}

$manifestLines = Get-Content -Path $manifestPath -Encoding UTF8
$definedPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

foreach ($line in $manifestLines) {
    if ($line -match '^\s*\|\s*`?([^`|]+?)`?\s*\|\s*[^|]+\|') {
        $captured = $matches[1].Trim()
        if ($captured -eq "パス" -or $captured -match '^-+$' -or $captured -match '^:?-+:?$') {
            continue
        }
        $normalized = $captured.Replace('\', '/').Trim()
        while ($normalized.EndsWith('/')) {
            $normalized = $normalized.Substring(0, $normalized.Length - 1)
        }
        if ($normalized.StartsWith('./')) {
            $normalized = $normalized.Substring(2)
        }
        if ($normalized.Length -gt 0) {
            [void]$definedPaths.Add($normalized)
        }
    }
}

$actualPaths = New-Object 'System.Collections.Generic.List[string]'

# 1. リポジトリ直下
# .git は Git 内部メタデータディレクトリのため照合の対象外
# Game/Library 等の .gitignore 除外対象はリポジトリ直下・docs/ 直下の列挙に含まれないため対象外
$rootItems = Get-ChildItem -Path $repoRoot -Force
foreach ($item in $rootItems) {
    if ($item.Name -eq ".git") {
        continue
    }
    $actualPaths.Add($item.Name.Replace('\', '/'))
}

# 2. docs/ 直下
$docsDir = Join-Path $repoRoot "docs"
if (Test-Path $docsDir) {
    $docsItems = Get-ChildItem -Path $docsDir -Force
    foreach ($item in $docsItems) {
        $actualPaths.Add(('docs/' + $item.Name).Replace('\', '/'))
    }
}

$unclassified = New-Object 'System.Collections.Generic.List[string]'
foreach ($actual in $actualPaths) {
    $normActual = $actual.Replace('\', '/').Trim()
    while ($normActual.EndsWith('/')) {
        $normActual = $normActual.Substring(0, $normActual.Length - 1)
    }
    if ($normActual.StartsWith('./')) {
        $normActual = $normActual.Substring(2)
    }

    if (-not $definedPaths.Contains($normActual)) {
        $unclassified.Add($actual)
    }
}

if ($unclassified.Count -gt 0) {
    Write-Host "FAIL: 未分類のパスが存在します ($($unclassified.Count) 件):" -ForegroundColor Red
    foreach ($p in $unclassified) {
        Write-Host "  - $p" -ForegroundColor Red
    }
    exit 1
}

Write-Host "PASS: すべての実在パスが publish-manifest.md に分類されています ($($actualPaths.Count) 件照合完了)" -ForegroundColor Green
exit 0
