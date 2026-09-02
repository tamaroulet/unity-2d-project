# SPDX-AI-Disclosure: ai-generated
# Claude Code の公式 /usage 出力から、5時間セッション枠および週間枠の正確な使用率(%)・リセット日時・残り秒数を確定抽出するスクリプト

$rawLines = claude -p "/usage" 2>$null
$output = $rawLines -join "`n"

$sessionUsed = $null
$sessionResetText = $null
$sessionResetIso = $null
$sessionRemainingSec = $null

$weeklyUsed = $null
$weeklyResetText = $null
$weeklyResetIso = $null
$weeklyRemainingSec = $null

$isAvailable = $false

# 1. 5時間セッション枠のパース (例: "Current session: 100% used · resets Sep 2, 8:49pm (Asia/Tokyo)")
if ($output -match "Current session:\s*(\d+)%\s*used(?:[^\r\n]*resets\s*([^(\r\n]+))?") {
    $sessionUsed = [int]$matches[1]
    if ($matches[2]) {
        $sessionResetText = $matches[2].Trim()
        try {
            $year = (Get-Date).Year
            $fullDateStr = "$sessionResetText $year"
            $dt = [DateTime]::ParseExact(
                $fullDateStr,
                "MMM d, h:mmtt yyyy",
                [System.Globalization.CultureInfo]::InvariantCulture
            )
            if ($dt -lt (Get-Date).AddHours(-1)) { $dt = $dt.AddYears(1) }
            $sessionRemainingSec = [Math]::Max(0, [int]($dt - (Get-Date)).TotalSeconds)
            $sessionResetIso = $dt.ToString("yyyy-MM-ddTHH:mm:ss")
        } catch {
            $sessionRemainingSec = $null
            $sessionResetIso = $null
        }
    } else {
        $sessionRemainingSec = 0
    }
}

# 2. 週間枠のパース (例: "Current week (all models): 0% used" または "Current week (all models): 16% used · resets Sep 2, 6:59pm (Asia/Tokyo)")
if ($output -match "Current week\s*\([^)]*\):\s*(\d+)%\s*used(?:[^\r\n]*resets\s*([^(\r\n]+))?") {
    $weeklyUsed = [int]$matches[1]
    if ($matches[2]) {
        $weeklyResetText = $matches[2].Trim()
        try {
            $year = (Get-Date).Year
            $fullDateStr = "$weeklyResetText $year"
            $dt = [DateTime]::ParseExact(
                $fullDateStr,
                "MMM d, h:mmtt yyyy",
                [System.Globalization.CultureInfo]::InvariantCulture
            )
            if ($dt -lt (Get-Date).AddHours(-1)) { $dt = $dt.AddYears(1) }
            $weeklyRemainingSec = [Math]::Max(0, [int]($dt - (Get-Date)).TotalSeconds)
            $weeklyResetIso = $dt.ToString("yyyy-MM-ddTHH:mm:ss")
        } catch {
            $weeklyRemainingSec = $null
            $weeklyResetIso = $null
        }
    } else {
        $weeklyRemainingSec = 0
    }
}

# 実行可否判定: セッション枠が85%未満 かつ 週間枠が95%未満
if ($sessionUsed -ne $null -and $sessionUsed -lt 85 -and ($weeklyUsed -eq $null -or $weeklyUsed -lt 95)) {
    $isAvailable = $true
} else {
    $isAvailable = $false
}

$result = [PSCustomObject]@{
    SessionUsedPercent   = $sessionUsed
    SessionResetTimeText = $sessionResetText
    SessionResetIso      = $sessionResetIso
    SessionRemainingSec  = $sessionRemainingSec
    WeeklyUsedPercent    = $weeklyUsed
    WeeklyResetTimeText  = $weeklyResetText
    WeeklyResetIso       = $weeklyResetIso
    WeeklyRemainingSec   = $weeklyRemainingSec
    IsAvailable          = $isAvailable
    TimestampUtc         = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    RawOutput            = $output.Trim()
}

$result | ConvertTo-Json
