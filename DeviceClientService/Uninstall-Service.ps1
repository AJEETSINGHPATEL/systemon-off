#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Uninstalls the DeviceClientService Windows Service.

.DESCRIPTION
    This script stops and removes the DeviceClientService Windows Service.

.PARAMETER ServiceName
    The name of the Windows Service to uninstall. Default is "DeviceClientService".

.EXAMPLE
    .\Uninstall-Service.ps1
    Uninstalls the service with the default name.
#>

param(
    [string]$ServiceName = "DeviceClientService"
)

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator. Please right-click and select 'Run as Administrator'."
    exit 1
}

Write-Host "Uninstalling Device Client Service..." -ForegroundColor Cyan
Write-Host "Service Name: $ServiceName" -ForegroundColor White
Write-Host ""

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Warning "Service '$ServiceName' not found. Nothing to uninstall."
    exit 0
}

# Stop the service if running
if ($service.Status -eq 'Running') {
    Write-Host "Stopping service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

# Remove the service
Write-Host "Removing service..." -ForegroundColor Yellow
$result = sc.exe delete $ServiceName

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Service uninstalled successfully!" -ForegroundColor Green
} else {
    Write-Error "Failed to uninstall service. Error code: $LASTEXITCODE"
    exit 1
}
