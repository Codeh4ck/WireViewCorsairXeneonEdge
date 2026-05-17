#Requires -RunAsAdministrator
$ServiceName  = "WireViewSensorService"
$DisplayName  = "WireView Sensor Service"
$Description  = "Reads WireView Pro 2 sensors and exposes them on http://localhost:59273"
$InstallDir   = "$env:ProgramFiles\WireViewSensorService"
$ExeName      = "WireViewMonitor.Service.exe"
$DllName      = "aspnetcorev2_inprocess.dll"
$ExeSource    = Join-Path $PSScriptRoot $ExeName
$DllSource    = Join-Path $PSScriptRoot $DllName
$ExeDest      = Join-Path $InstallDir $ExeName
# ── Validate ──────────────────────────────────────────────────────────────────
if (-not (Test-Path $ExeSource)) {
    Write-Error "$ExeName not found. Please redownload the installation package and make sure you have extracted all files. Aborting..."
    exit 1
}

# if (-not (Test-Path $DllSource)) {
    # Write-Error "$DllName not found. Please redownload the installation package and make sure you have extracted all files. Aborting..."
    # exit 1
# }
# ── Stop and remove existing installation ────────────────────────────────────
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Removing existing service..."
    if ($existing.Status -eq "Running") {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
}
# ── Install ───────────────────────────────────────────────────────────────────
Write-Host "Installing to $InstallDir ..."
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Path $ExeSource -Destination $ExeDest -Force
Copy-Item -Path $DllSource -Destination $InstallDir -Force
Write-Host "Registering service..."
sc.exe create $ServiceName binPath= `"$ExeDest`" start= auto DisplayName= `"$DisplayName`" | Out-Null
sc.exe description $ServiceName $Description | Out-Null
# Configure recovery: restart on any stop including clean exit (3 attempts, 5s delay)
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/5000 | Out-Null
sc.exe failureflag $ServiceName 1 | Out-Null
Write-Host "Starting service..."
Start-Service -Name $ServiceName
$svc = Get-Service -Name $ServiceName
Write-Host "Service status: $($svc.Status)"
if ($svc.Status -eq "Running") {
    Write-Host ""
    Write-Host "Installation complete. Corsair Xeneon Edge widget needs to be installed manually." -ForegroundColor Green
    Write-Host ""
    Write-Host "Available endpoints:"
    Write-Host "  Sensors:       http://localhost:59273/sensors"
    Write-Host "  Health:        http://localhost:59273/health"
    Write-Host "  Config:        http://localhost:59273/config"
    Write-Host "  Device config: http://localhost:59273/device/config"
    Write-Host "  Faults clear:  http://localhost:59273/faults/clear"
    Write-Host "  Restart:       http://localhost:59273/restart"
    Write-Host "  WebSocket:     ws://localhost:59273/ws"
} else {
    Write-Warning "Service did not start. Check Event Viewer > Windows Logs > Application for details."
}