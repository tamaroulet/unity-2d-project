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

$trigger = New-ScheduledTaskTrigger -Daily -At "01:00"

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
    -Description "unity-2d-project Autonomous Background Runner (Night-only: 01:00-06:00, ${IntervalMinutes}m interval)" `
    -Force | Out-Null

# Configure Repetition via COM object to bypass PowerShell cmdlet parameter set limitations
$service = New-Object -ComObject Schedule.Service
$service.Connect()
$root = $service.GetFolder("\")
$task = $root.GetTask($TaskName)
$def = $task.Definition
$def.Triggers.Item(1).Repetition.Interval = "PT${IntervalMinutes}M"
$def.Triggers.Item(1).Repetition.Duration = "PT5H"
$root.RegisterTaskDefinition($TaskName, $def, 6, $null, $null, 3) | Out-Null

Write-Host "========================================"
Write-Host " Task Scheduler Registration Successful"
Write-Host "========================================"
Write-Host "  Task Name:    $TaskName"
Write-Host "  Active Hours: 01:00 - 06:00 (Night Only)"
Write-Host "  Interval:     $IntervalMinutes min"
Write-Host "  Python:       $pythonExe"
Write-Host "  Script:       $scriptPath"
Write-Host "  Working Dir:  $projectRoot"
Write-Host "========================================"
