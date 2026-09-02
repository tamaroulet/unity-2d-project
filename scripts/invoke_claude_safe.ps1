# SPDX-AI-Disclosure: ai-generated
# Claude Code 安全実行ラッパー
# 実行前に公式 /usage を検査し、使用率90%以上の場合は強制遮断して課金・枯渇を防ぐ。

param (
    [Parameter(Mandatory=$true)]
    [string]$Prompt,
    [switch]$Force
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$quotaJson = & powershell -File "$scriptDir\get_claude_quota.ps1" | Out-String
$quota = $quotaJson | ConvertFrom-Json

if (-not $Force -and (-not $quota.IsAvailable -or $quota.SessionUsedPercent -ge 90)) {
    Write-Error "[BLOCKED] Claude Code is currently restricted ($($quota.SessionUsedPercent)% used). Resets at: $($quota.ResetTimeText). Execution blocked to prevent paid overage. Delegate task to Gemini."
    exit 1
}

Write-Host "[ALLOWED] Claude Code session usage is $($quota.SessionUsedPercent)%. Executing prompt..."
& claude --dangerously-skip-permissions -p $Prompt
