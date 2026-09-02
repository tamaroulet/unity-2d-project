# SPDX-AI-Disclosure: ai-generated
# Claude Code および Gemini の両方の残量・リセット時刻を完全自動で一括取得・表示する総合チェッカースクリプト

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$claudeJson = & powershell -File "$scriptDir\get_claude_quota.ps1" | Out-String
$claude = $claudeJson | ConvertFrom-Json

$geminiJson = & powershell -File "$scriptDir\get_gemini_quota.ps1" | Out-String
$gemini = $geminiJson | ConvertFrom-Json

$summary = [PSCustomObject]@{
    Claude = [PSCustomObject]@{
        SessionUsedPercent   = $claude.SessionUsedPercent
        SessionResetTimeText = $claude.SessionResetTimeText
        SessionRemainingSec  = $claude.SessionRemainingSec
        WeeklyUsedPercent    = $claude.WeeklyUsedPercent
        IsAvailable          = $claude.IsAvailable
    }
    Gemini = [PSCustomObject]@{
        WeeklyRemainingPercent = $gemini.GeminiWeeklyRemainingPercent
        WeeklyResetTimeUtc     = $gemini.GeminiWeeklyResetTimeUtc
        FiveHourRemainingPercent = $gemini.Gemini5hRemainingPercent
        FiveHourResetTimeUtc     = $gemini.Gemini5hResetTimeUtc
    }
    TimestampUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
}

$summary | ConvertTo-Json -Depth 5
