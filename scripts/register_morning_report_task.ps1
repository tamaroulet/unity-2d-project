param(
    [string]$TaskName = "UnityProject_MorningReport",
    [string]$At = "06:10"
)

# SPDX-AI-Disclosure: ai-generated
# 朝刊レポートを 06:10 に自動生成する Task Scheduler 登録スクリプト。
# 夜間ランナー（01:00-06:00）の終了直後に走る。

$projectRoot = "c:\dev\unity-2d-project"
$pythonExe = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $pythonExe) {
    $pythonExe = "C:\Users\tamar\AppData\Local\Programs\Python\Python312\python.exe"
}
$scriptPath = Join-Path $projectRoot "scripts\morning_report.py"

$existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "[INFO] Unregistering existing task '$TaskName'..."
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

$action = New-ScheduledTaskAction `
    -Execute $pythonExe `
    -Argument "`"$scriptPath`"" `
    -WorkingDirectory $projectRoot

$trigger = New-ScheduledTaskTrigger -Daily -At $At

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 10)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Description "unity-2d-project Morning Report (docs/nightly/YYYY-MM-DD.md)" `
    -Force | Out-Null

Write-Host "========================================"
Write-Host " Morning Report Task Registered"
Write-Host "========================================"
Write-Host "  Task Name: $TaskName"
Write-Host "  Runs at:   $At daily"
Write-Host "  Output:    docs\nightly\<date>.md"
Write-Host "========================================"
