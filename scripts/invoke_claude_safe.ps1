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
$quotaJson = & powershell -File "$scriptDir\get_claude_quota.ps1" | Out-String
$quota = $quotaJson | ConvertFrom-Json

if (-not $Force -and (-not $quota.IsAvailable -or $quota.SessionUsedPercent -ge 85)) {
    Write-Error "[BLOCKED] Claude Code is currently restricted ($($quota.SessionUsedPercent)% used). Resets at: $($quota.ResetTimeText) (in $($quota.RemainingSeconds)s). Execution blocked to prevent paid overage. Delegate task to Gemini."
    exit 1
}

Write-Host "[ALLOWED] Claude Code session usage is $($quota.SessionUsedPercent)%. Executing prompt with model: $Model..."
$null | & claude --dangerously-skip-permissions --model $Model -p $Prompt

# 実行後の最新残量を再取得
Write-Host "`n[POST-EXECUTION] Checking updated Claude Code quota..."
& powershell -File "$scriptDir\get_claude_quota.ps1"
