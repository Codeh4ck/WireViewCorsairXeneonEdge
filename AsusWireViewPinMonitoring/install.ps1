#Requires -RunAsAdministrator
$ServiceName  = "AsusWireviewPinMonitoringService"
$DisplayName  = "Asus - WirevView Pin Monitoring Service"
$Description  = "Reads ASUS ROG RTX 5090 Astral OC sensors and exposes them on http://localhost:59275"
$InstallDir   = "$env:ProgramFiles\AsusWireviewPinMonitoringService"

$ExeName      = "AsusWireviewPinMonitoring.Service.exe"

#$DllName = "aspnetcorev2_inprocess.dll"
$LibPosixHelperDll = "libMonoPosixHelper.dll"
$MonoPosixHelperDll = "MonoPosixHelper.dll"

$ExeSource    = Join-Path $PSScriptRoot $ExeName
$DllSource    = Join-Path $PSScriptRoot $DllName
$LibPosixHelperSource = Join-Path $PSScriptRoot $LibPosixHelperDll
$MonoPosixHelperSource = Join-Path $PSScriptRoot $MonoPosixHelperDll

$ExeDest      = Join-Path $InstallDir $ExeName
# ── Validate ──────────────────────────────────────────────────────────────────
if (-not (Test-Path $ExeSource)) {
    Write-Error "$ExeName not found. Please redownload the installation package and make sure you have extracted all files. Aborting..."
    exit 1
}

#if (-not (test-path $DllSource)) {
#	write-error "$DllName not found. please redownload the installation package and make sure you have extracted all files. aborting..."
#	exit 1
#}

if (-not (test-path $LibPosixHelperSource)) {
	write-error "$LibPosixHelperDll not found. please redownload the installation package and make sure you have extracted all files. aborting..."
	exit 1
}

if (-not (test-path $MonoPosixHelperSource)) {
	write-error "$MonoPosixHelperDll not found. please redownload the installation package and make sure you have extracted all files. aborting..."
	exit 1
}
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
#Copy-Item -Path $DllSource -Destination $InstallDir -Force
Copy-Item -Path $LibPosixHelperSource -Destination $InstallDir -Force
Copy-Item -Path $MonoPosixHelperSource -Destination $InstallDir -Force
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
    Write-Host "  Sensors:       http://localhost:59275/sensors"
    Write-Host "  Config:        http://localhost:59275/config"
    Write-Host "  Restart:       http://localhost:59275/restart"
    Write-Host "  WebSocket:     ws://localhost:59275/ws"
} else {
    Write-Warning "Service did not start. Check Event Viewer > Windows Logs > Application for details."
}