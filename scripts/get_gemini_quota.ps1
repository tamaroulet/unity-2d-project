# SPDX-AI-Disclosure: ai-generated
# Antigravity IDE のローカル Language Server から、Gemini の週間枠および5時間枠の正確な残量(%)・リセット日時・残り秒数を完全自動抽出するスクリプト

$lsAddress = $env:ANTIGRAVITY_LS_ADDRESS
$csrfToken = $env:ANTIGRAVITY_CSRF_TOKEN

if (-not $lsAddress -or -not $csrfToken) {
    Write-Error "ANTIGRAVITY_LS_ADDRESS or ANTIGRAVITY_CSRF_TOKEN is not set."
    exit 1
}

$headers = @{
    "x-codeium-csrf-token" = $csrfToken
    "Content-Type"         = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri "http://$lsAddress/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary" -Method Post -Headers $headers -Body "{}" -TimeoutSec 5
} catch {
    Write-Error "Failed to call RetrieveUserQuotaSummary RPC: $_"
    exit 1
}

$geminiGroup = $response.response.groups | Where-Object { $_.displayName -match "Gemini" }
$weeklyBucket = $geminiGroup.buckets | Where-Object { $_.window -eq "weekly" }
$fiveHourBucket = $geminiGroup.buckets | Where-Object { $_.window -eq "5h" }

$weeklyRemaining = if ($weeklyBucket) { [Math]::Round($weeklyBucket.remainingFraction * 100, 1) } else { $null }
$fiveHourRemaining = if ($fiveHourBucket) { [Math]::Round($fiveHourBucket.remainingFraction * 100, 1) } else { $null }

$result = [PSCustomObject]@{
    GeminiWeeklyRemainingPercent   = $weeklyRemaining
    GeminiWeeklyResetTimeUtc       = $weeklyBucket.resetTime
    Gemini5hRemainingPercent       = $fiveHourRemaining
    Gemini5hResetTimeUtc           = $fiveHourBucket.resetTime
    TimestampUtc                   = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
}

$result | ConvertTo-Json
