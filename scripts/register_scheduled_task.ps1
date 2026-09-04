# SPDX-AI-Disclosure: ai-generated
# 稼働窓の終端は auto_runner.py 側の AUTO_RUN_END_HOUR が持つ。
# ここで反復間隔を指定しないのは、外側の反復と内側のループが多重起動を招くため。
param(
    [string]$TaskName = "UnityProject_AutoRunner"
)

$projectRoot = "c:\dev\unity-2d-project"
$pythonExe = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $pythonExe) {
    $pythonExe = "C:\Users\tamar\AppData\Local\Programs\Python\Python312\python.exe"
}

$scriptPath = Join-Path $projectRoot "scripts\auto_runner.py"

# Unregister existing task if present
$existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "[INFO] Unregistering existing task '$TaskName'..."
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

$action = New-ScheduledTaskAction `
    -Execute $pythonExe `
    -Argument "`"$scriptPath`"" `
    -WorkingDirectory $projectRoot

$trigger = New-ScheduledTaskTrigger -Daily -At "01:00"

# ExecutionTimeLimit は稼働窓 (01:00-06:00) と同じ 5 時間。
# 45 分では EditMode/PlayMode の batchmode テスト（各最大 30 分）の途中で
# タスクが強制終了され、隔離・巻き戻しが完走せずツリーが汚れたまま朝を迎える。
#
# RestartCount / Repetition は付けない。auto_runner.py が内部で AUTO_RUN_END_HOUR まで
# ループするため、外側から再起動・反復すると多重起動して同じブランチを奪い合う。
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 5)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Description "unity-2d-project Autonomous Background Runner (single run at 01:00; auto_runner loops internally until AUTO_RUN_END_HOUR=06)" `
    -Force | Out-Null

Write-Host "========================================"
Write-Host " Task Scheduler Registration Successful"
Write-Host "========================================"
Write-Host "  Task Name:    $TaskName"
Write-Host "  Starts At:    01:00 (single launch, no repetition)"
Write-Host "  Runs Until:   06:00 (auto_runner internal loop / AUTO_RUN_END_HOUR)"
Write-Host "  Time Limit:   5 hours"
Write-Host "  Python:       $pythonExe"
Write-Host "  Script:       $scriptPath"
Write-Host "  Working Dir:  $projectRoot"
Write-Host "========================================"
