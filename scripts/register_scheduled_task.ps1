# SPDX-AI-Disclosure: ai-generated
# register_scheduled_task.ps1
# Windows Task Scheduler に自律継続実行タスクを登録するスクリプト。
#
# 使い方:
#   powershell -ExecutionPolicy Bypass -NoProfile -File scripts/register_scheduled_task.ps1
#
# 登録後:
#   - 30分ごとに auto_runner.py が自動実行される
#   - ユーザーがログオンしていなくても実行可能
#   - 手動実行: schtasks /run /tn "UnityProject_AutoRunner"
#   - 削除:     schtasks /delete /tn "UnityProject_AutoRunner" /f

param(
    [int]$IntervalMinutes = 30,
    [string]$TaskName = "UnityProject_AutoRunner"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $projectRoot) {
    $projectRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
}

$pythonExe = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $pythonExe) {
    Write-Error "Python が見つかりません。PATH に Python を追加してください。"
    exit 1
}

$scriptPath = Join-Path $projectRoot "scripts\auto_runner.py"
if (-not (Test-Path $scriptPath)) {
    Write-Error "auto_runner.py が見つかりません: $scriptPath"
    exit 1
}

# 既存タスクの削除（存在する場合）
$existing = schtasks /query /tn $TaskName 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "[INFO] 既存タスク '$TaskName' を削除中..."
    schtasks /delete /tn $TaskName /f
}

# タスクの登録
$action = New-ScheduledTaskAction `
    -Execute $pythonExe `
    -Argument "`"$scriptPath`"" `
    -WorkingDirectory $projectRoot

$trigger = New-ScheduledTaskTrigger `
    -Once `
    -At (Get-Date).AddMinutes(1) `
    -RepetitionInterval (New-TimeSpan -Minutes $IntervalMinutes) `
    -RepetitionDuration (New-TimeSpan -Days 365)

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 45) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 5)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Description "unity-2d-project 自律継続実行 (Antigravity SDK / ${IntervalMinutes}分間隔)" `
    -Force

Write-Host ""
Write-Host "========================================"
Write-Host " タスク登録完了"
Write-Host "========================================"
Write-Host "  タスク名:       $TaskName"
Write-Host "  実行間隔:       ${IntervalMinutes}分"
Write-Host "  Python:         $pythonExe"
Write-Host "  スクリプト:     $scriptPath"
Write-Host "  作業ディレクトリ: $projectRoot"
Write-Host ""
Write-Host "手動実行:  schtasks /run /tn `"$TaskName`""
Write-Host "状態確認:  schtasks /query /tn `"$TaskName`" /v"
Write-Host "削除:      schtasks /delete /tn `"$TaskName`" /f"
Write-Host "========================================"
