# SPDX-AI-Disclosure: ai-generated
# Claude Code の公式 /usage 出力から、正確な使用率(%)とリセット日時、残り秒数を確定抽出するスクリプト

$rawLines = claude -p "/usage" 2>$null
$output = $rawLines -join "`n"

$usedPercent = $null
$resetText = $null
$remainingSeconds = $null
$resetIso = $null
$isAvailable = $false

if ($output -match "Current session:\s*(\d+)%\s*used[^\r\n]*resets\s*([^(\r\n]+)") {
    $usedPercent = [int]$matches[1]
    $rawDateStr = $matches[2].Trim()
    $resetText = $rawDateStr

    try {
        $year = (Get-Date).Year
        $fullDateStr = "$rawDateStr $year"
        $resetDateTime = [DateTime]::ParseExact(
            $fullDateStr,
            "MMM d, h:mmtt yyyy",
            [System.Globalization.CultureInfo]::InvariantCulture
        )

        if ($resetDateTime -lt (Get-Date).AddHours(-1)) {
            $resetDateTime = $resetDateTime.AddYears(1)
        }

        $timeRemaining = $resetDateTime - (Get-Date)
        $remainingSeconds = [Math]::Max(0, [int]$timeRemaining.TotalSeconds)
        $resetIso = $resetDateTime.ToString("yyyy-MM-ddTHH:mm:ss")
    } catch {
        $remainingSeconds = $null
        $resetIso = $null
    }

    if ($usedPercent -lt 85) {
        $isAvailable = $true
    } else {
        $isAvailable = $false
    }
}

$result = [PSCustomObject]@{
    SessionUsedPercent = $usedPercent
    ResetTimeText      = $resetText
    ResetIso           = $resetIso
    RemainingSeconds   = $remainingSeconds
    IsAvailable        = $isAvailable
    RawOutput          = $output.Trim()
}

$result | ConvertTo-Json
