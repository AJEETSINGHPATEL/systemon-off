#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs the DeviceClientService as a Windows Service with Administrator privileges.

.DESCRIPTION
    This script installs the DeviceClientService Windows Service and configures it to run as Administrator.
    The service will have the necessary privileges to execute shutdown and restart commands.

.PARAMETER ServiceName
    The name of the Windows Service. Default is "DeviceClientService".

.PARAMETER DisplayName
    The display name of the Windows Service. Default is "Device Client Service".

.PARAMETER Description
    The description of the Windows Service. Default is "Remote control service for device management".

.PARAMETER BinaryPath
    The path to the service executable. Default is the current directory's release build.

.EXAMPLE
    .\Install-Service.ps1
    Installs the service with default settings.

.EXAMPLE
    .\Install-Service.ps1 -ServiceName "MyRemoteService" -DisplayName "My Remote Service"
    Installs the service with custom names.
#>

param(
    [string]$ServiceName = "DeviceClientService",
    [string]$DisplayName = "Device Client Service",
    [string]$Description = "Remote control service for device management - allows remote shutdown and restart",
    [string]$BinaryPath = ""
)

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator. Please right-click and select 'Run as Administrator'."
    exit 1
}

# Determine binary path if not provided
if ([string]::IsNullOrWhiteSpace($BinaryPath)) {
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    $BinaryPath = Join-Path $scriptPath "bin\Release\net6.0\DeviceClientService.exe"
    
    # Check if release build exists, otherwise use debug
    if (-not (Test-Path $BinaryPath)) {
        $BinaryPath = Join-Path $scriptPath "bin\Debug\net6.0\DeviceClientService.exe"
    }
}

# Verify the executable exists
if (-not (Test-Path $BinaryPath)) {
    Write-Error "Service executable not found at: $BinaryPath"
    Write-Host "Please build the project first using: dotnet build -c Release"
    exit 1
}

Write-Host "Installing Device Client Service..." -ForegroundColor Cyan
Write-Host "Service Name: $ServiceName" -ForegroundColor White
Write-Host "Binary Path: $BinaryPath" -ForegroundColor White
Write-Host ""

# Stop and remove existing service if it exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    
    Write-Host "Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Create the service - run as LocalSystem (has admin privileges)
Write-Host "Creating new service..." -ForegroundColor Green
$result = sc.exe create $ServiceName `
    binPath= "$BinaryPath" `
    start= auto `
    obj= "LocalSystem" `
    displayName= "$DisplayName"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create service. Error code: $LASTEXITCODE"
    exit 1
}

# Set service description
sc.exe description $ServiceName "$Description" | Out-Null

# Configure service to restart on failure
sc.exe failure $ServiceName reset= 60 actions= restart/5000/restart/10000/restart/30000 | Out-Null

# Grant service the right to log on as a service
Write-Host "Configuring service permissions..." -ForegroundColor Green

# Start the service
Write-Host "Starting service..." -ForegroundColor Green
Start-Service -Name $ServiceName

# Verify service is running
Start-Sleep -Seconds 2
$service = Get-Service -Name $ServiceName
if ($service.Status -eq 'Running') {
    Write-Host ""
    Write-Host "✅ Service installed and started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Service Details:" -ForegroundColor Cyan
    Write-Host "  Name: $($service.Name)"
    Write-Host "  Status: $($service.Status)"
    Write-Host "  Start Type: $($service.StartType)"
    Write-Host ""
    Write-Host "To manage the service:" -ForegroundColor Cyan
    Write-Host "  Stop:   Stop-Service -Name $ServiceName"
    Write-Host "  Start:  Start-Service -Name $ServiceName"
    Write-Host "  Remove: .\Uninstall-Service.ps1"
} else {
    Write-Warning "Service installed but may not be running. Status: $($service.Status)"
    Write-Host "Check Event Viewer > Windows Logs > Application for details."
}

Write-Host ""
Write-Host "Installation complete!" -ForegroundColor Green
