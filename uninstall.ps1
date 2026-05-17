#Requires -RunAsAdministrator

$ServiceName = "WireViewSensorService"
$InstallDir = "$env:ProgramFiles\WireViewSensorService"

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $existing) {
    Write-Host "$ServiceName not found. Nothing to uninstall."
    exit 0
}

Write-Host "Stopping $ServiceName..."
if ($existing.Status -eq "Running") {
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

# Force-kill the process in case WebSocket connections are keeping it alive
$proc = Get-Process -Name "WireViewMonitor.Service" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Force-killing residual process..."
    $proc | Stop-Process -Force
    Start-Sleep -Seconds 1
}

Write-Host "Removing $ServiceName..."
sc.exe delete $ServiceName | Out-Null
Start-Sleep -Seconds 1

Write-Host "Removing files..."
if (Test-Path $InstallDir) {
    Remove-Item -Path $InstallDir -Recurse -Force
}

Write-Host "Removing config..."
Remove-Item -Path "HKLM:\SOFTWARE\WireViewSensorService" -Recurse -ErrorAction SilentlyContinue

Write-Host "Uninstall complete. If you have installed the accompanied Corsair Xeneon Edge widget, you will need to manually delete it." -ForegroundColor Green