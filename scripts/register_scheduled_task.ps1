# SPDX-AI-Disclosure: ai-generated
param(
    [int]$IntervalMinutes = 30,
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
    -Description "unity-2d-project Autonomous Background Runner (Antigravity SDK / ${IntervalMinutes}m interval)" `
    -Force | Out-Null

Write-Host "========================================"
Write-Host " Task Scheduler Registration Successful"
Write-Host "========================================"
Write-Host "  Task Name:    $TaskName"
Write-Host "  Interval:     $IntervalMinutes min"
Write-Host "  Python:       $pythonExe"
Write-Host "  Script:       $scriptPath"
Write-Host "  Working Dir:  $projectRoot"
Write-Host "========================================"
