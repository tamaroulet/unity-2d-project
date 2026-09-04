# SPDX-AI-Disclosure: ai-generated
# Claude Code 安全実行ラッパー
# 実行前後に公式 /usage を検査し、使用率85%以上の場合は強制遮断して課金・枯渇を防ぐ。

param (
    [Parameter(Mandatory=$true)]
    [string]$Prompt,
    [string]$Model = "opus",
    [switch]$Force
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$logsDir = Join-Path $projectRoot "logs"
if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir -Force | Out-Null }
$sessionIdFile = Join-Path $logsDir "claude_session_id.txt"

$quotaJson = & powershell -File "$scriptDir\get_claude_quota.ps1" | Out-String
$quota = $quotaJson | ConvertFrom-Json

if (-not $Force -and (-not $quota.IsAvailable -or $quota.SessionUsedPercent -ge 85)) {
    Write-Error "[BLOCKED] Claude Code is currently restricted ($($quota.SessionUsedPercent)% used). Resets at: $($quota.ResetTimeText) (in $($quota.RemainingSeconds)s). Execution blocked to prevent paid overage. Delegate task to Gemini."
    exit 1
}

Write-Host "[ALLOWED] Claude Code session usage is $($quota.SessionUsedPercent)%. Executing prompt with model: $Model..."

$sessionResumed = $false
if (Test-Path $sessionIdFile) {
    $existingSessionId = (Get-Content $sessionIdFile -Raw).Trim()
    if ($existingSessionId) {
        Write-Host "[SESSION] Resuming existing session: $existingSessionId"
        $null | & claude --resume $existingSessionId --dangerously-skip-permissions --model $Model -p $Prompt
        if ($LASTEXITCODE -eq 0) {
            $sessionResumed = $true
        } else {
            Write-Warning "[SESSION] Failed to resume session $existingSessionId. Falling back to new session."
        }
    }
}

if (-not $sessionResumed) {
    $newSessionId = [guid]::NewGuid().ToString()
    Write-Host "[SESSION] Starting new session: $newSessionId"
    $null | & claude --session-id $newSessionId --dangerously-skip-permissions --model $Model -p $Prompt
    if ($LASTEXITCODE -eq 0) {
        Set-Content -Path $sessionIdFile -Value $newSessionId -Encoding utf8
    } else {
        Write-Warning "[SESSION] Failed with --session-id. Falling back to simple -p."
        $null | & claude --dangerously-skip-permissions --model $Model -p $Prompt
    }
}

# 実行後の最新残量を再取得
Write-Host "`n[POST-EXECUTION] Checking updated Claude Code quota..."
& powershell -File "$scriptDir\get_claude_quota.ps1"
