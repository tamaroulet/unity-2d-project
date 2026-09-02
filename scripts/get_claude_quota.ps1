# SPDX-AI-Disclosure: ai-generated
# Claude Code の公式 /usage コマンドから正確な残量とリセット時刻を取得するスクリプト

$rawLines = claude -p "/usage" 2>$null
$output = $rawLines -join "`n"

$usedPercent = $null
$resetText = $null
$isAvailable = $false

if ($output -match "Current session:\s*(\d+)%\s*used[^\r\n]*resets\s*([^\r\n]+)") {
    $usedPercent = [int]$matches[1]
    $resetText = $matches[2].Trim()
    
    if ($usedPercent -lt 90) {
        $isAvailable = $true
    }
}

$result = [PSCustomObject]@{
    SessionUsedPercent = $usedPercent
    ResetTimeText      = $resetText
    IsAvailable        = $isAvailable
    RawOutput          = $output.Trim()
}

$result | ConvertTo-Json
