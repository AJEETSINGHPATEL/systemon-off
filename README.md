# 🔌 Remote System Power Control

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-blue" />
  <img src="https://img.shields.io/badge/Platform-Windows-success" />
  <img src="https://img.shields.io/badge/License-Custom-green" />
  <img src="https://img.shields.io/badge/Status-Active-brightgreen" />
</p>

---

## 📖 Overview

A lightweight system to **remotely control Windows power actions**:

* 🔌 Shutdown
* 🔄 Restart
* 💤 Sleep
* ⚡ Power ON (Wake-on-LAN)

Control your system from:

* 🌐 Same Network (LAN)
* 🌍 Different Network (Internet)
* 📱 Mobile Browser

---

## 🚀 Features

* 🔌 Full power control (ON/OFF/Restart/Sleep)
* ⚡ Wake-on-LAN support (Power ON)
* 🌐 Works across networks
* 📱 Mobile-friendly web panel
* ⚙️ Windows background service
* ⚡ Fast and lightweight
* 🔐 Extendable security system

---

## ⚙️ How It Works

```id="0gm4xr"
[ 📱 Browser / Mobile ]
            ↓
   [ 🌐 ASP.NET Core API ]
            ↓
   [ ⚙️ Windows Service ]
            ↓
   [ 💻 Target Machine ]
```

* Web panel sends commands to API
* Windows service polls server
* Service executes system-level commands
* Wake-on-LAN packet used for power ON

---

## 🛠️ Technologies Used

* **Backend:** ASP.NET Core Web API
* **Client Service:** C# (.NET Worker Service)
* **Frontend:** HTML, CSS, JavaScript
* **Protocol:** HTTP (REST API)

---

## 🚀 Installation

### 1️⃣ Clone Repository

```bash id="6u6u4r"
git clone https://github.com/your-username/remote-power-control.git
cd remote-power-control
```

---

### 2️⃣ Run Server

```bash id="lt6y6q"
cd server
dotnet restore
dotnet run
```

---

### 3️⃣ Setup Windows Service

Update configuration:

```json id="r5qv5y"
{
  "ServerUrl": "http://YOUR_SERVER_IP:5000",
  "DeviceId": "PC-01"
}
```

Build and install:

```bash id="dr1jyd"
dotnet publish -c Release
sc create RemotePowerService binPath= "C:\path\to\service.exe"
sc start RemotePowerService
```

---

### 4️⃣ Run Web Panel

Open `index.html` and update API URL:

```javascript id="xbrq1c"
const API_URL = "http://YOUR_SERVER_IP:5000/api/device/command";
```

---

## 📡 API Reference

### 🔹 Send Command

```id="7r0t8o"
POST /api/device/command
```

```json id="e5v8vw"
{
  "action": "shutdown"
}
```

---

### 🔹 Get Command

```id="x8z1qv"
GET /api/device/command?deviceId=PC-01
```

---

## ⚡ Wake-on-LAN (Power ON)

To enable remote startup:

* Enable **Wake-on-LAN** in BIOS
* Enable in Windows Network Adapter settings
* Use MAC address to send WoL packet
* Ensure router allows WoL packets (for WAN use)

---

## 🔐 Security Recommendations

* Use HTTPS (SSL)
* Add API authentication (JWT / API key)
* Restrict unknown devices
* Add logging & monitoring

---

## 💡 Future Improvements

* 📊 Dashboard UI
* 👥 Multi-device control
* ⏰ Scheduling
* 🔔 Notifications
* 📱 Mobile App

---

## 🤝 Contributing

Pull requests are welcome.
For major changes, open an issue first.

---

## 📄 License

See LICENSE file for details.

---

🔥 *Built for practical remote system control*
