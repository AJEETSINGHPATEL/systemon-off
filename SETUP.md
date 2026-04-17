# Remote Control System - Setup Guide

## Overview
This system allows remote **Wake Up (Wake-on-LAN)**, **Shutdown**, and **Restart** of Windows machines via a web interface.

## Architecture
- **BackendServer**: ASP.NET Core SignalR hub (port 5285)
- **DeviceClientService**: Windows Service that runs on target machines
- **Frontend**: HTML/JS control panel

## Features
- ✅ **Wake Up**: Turn on remote PCs using Wake-on-LAN (WOL)
- ✅ **Shutdown**: Turn off remote PCs
- ✅ **Restart**: Restart remote PCs
- ✅ **Device Status**: See which devices are online/offline
- ✅ **MAC Address Tracking**: Automatic MAC address detection for WOL

## Prerequisites
- .NET 6.0 SDK or Runtime
- Windows machine (for service)
- Administrator privileges (for service installation)
- Wake-on-LAN enabled in BIOS and network adapter settings (for wake up feature)

---

## 1. Build the Backend Server

```powershell
cd d:\RemoteControlSystem\BackendServer\BackendServer
dotnet build -c Release
dotnet run --urls "http://localhost:5285"
```

The backend will start on `http://localhost:5285`

---

## 2. Build and Install the Device Client Service

### Build the service:
```powershell
cd d:\RemoteControlSystem\DeviceClientService
dotnet build -c Release
```

### Install the service (Run as Administrator):
```powershell
cd d:\RemoteControlSystem\DeviceClientService
.\Install-Service.ps1
```

This will:
- Install the service to run as **LocalSystem** (Administrator privileges)
- Configure auto-restart on failure
- Start the service automatically

### Verify service is running:
```powershell
Get-Service DeviceClientService
```

### Uninstall the service:
```powershell
.\Uninstall-Service.ps1
```

---

## 3. Open the Control Panel

Open either of these files in a web browser:
- `d:\RemoteControlSystem\RemoteControlSystem\index.html` (modern UI with dropdown)
- `d:\RemoteControlSystem\BackendServer\index.html` (simple UI with text input)

Both connect to `http://localhost:5285/deviceHub`

---

## Configuration

### DeviceClientService appsettings.json
```json
{
  "HubUrl": "http://localhost:5285/deviceHub",
  "DeviceName": ""
}
```

- `HubUrl`: Backend server URL
- `DeviceName`: Custom device name (empty = uses computer name)

### Changing the Backend URL

If the backend runs on a different machine:

1. Update `appsettings.json` in DeviceClientService
2. Update the `hubUrl` in the frontend HTML files
3. Rebuild and reinstall the service

---

## Troubleshooting

### Service won't start
Check Event Viewer > Windows Logs > Application for errors.

### Cannot connect to backend
1. Verify backend is running: `http://localhost:5285`
2. Check Windows Firewall allows port 5285
3. Verify CORS is configured in Program.cs

### Shutdown/Restart doesn't work
The service must run as Administrator (LocalSystem). Verify:
```powershell
(Get-WmiObject Win32_Service -Filter "Name='DeviceClientService'").StartName
# Should return: LocalSystem
```

### Wake Up doesn't work
1. **Enable WOL in BIOS**: Check BIOS settings for "Wake on LAN" or "PCI-E Wake"
2. **Enable WOL in Windows**:
   - Device Manager → Network adapters → Your adapter → Properties
   - Power Management → Check "Allow this device to wake the computer"
   - Advanced → "Wake on Magic Packet" → Enabled
3. **Check MAC address**: The device must have registered its MAC address (shown in web panel)
4. **Network configuration**: Some routers block WOL packets. Try from the same subnet.

### Device not showing in dropdown
1. Check service is running: `Get-Service DeviceClientService`
2. Check backend logs for connection attempts
3. Verify HubUrl in appsettings.json is correct

### Wake Up button is disabled
The Wake Up button is disabled if the device doesn't have a MAC address registered. Make sure:
1. The device was online at least once (to register its MAC)
2. The device has a network adapter with a valid MAC address

---

## Security Notes

⚠️ **WARNING**: This system allows remote shutdown of computers.

- Only run on trusted networks
- The backend currently has NO authentication
- Anyone with access to the backend can shutdown connected devices
- Consider adding authentication for production use

---

## File Structure

```
RemoteControlSystem/
├── BackendServer/
│   ├── BackendServer/
│   │   ├── Hubs/DeviceHub.cs       # SignalR hub
│   │   ├── Program.cs              # Backend entry point
│   │   └── BackendServer.csproj
│   └── index.html                  # Simple control panel
├── DeviceClientService/
│   ├── Worker.cs                   # Service logic
│   ├── Program.cs                  # Service entry point
│   ├── appsettings.json            # Configuration
│   ├── Install-Service.ps1         # Install script
│   ├── Uninstall-Service.ps1       # Uninstall script
│   └── DeviceClientService.csproj
└── RemoteControlSystem/
    └── index.html                  # Modern control panel
```
