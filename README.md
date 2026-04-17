# 🔌 System ON/OFF Remote Controller

A lightweight **C# + ASP.NET Core + Windows Service** project to remotely control Windows power actions like **Shutdown, Restart, Sleep, and Wake-on-LAN (Power ON)** through a simple web panel.

---

## 🚀 Overview

This system allows you to control a Windows PC remotely using:

* 🌐 Web Browser (Mobile / Desktop)
* ⚙️ ASP.NET Core API Server
* 🖥️ C# Windows Service (Client Agent)

---

## ⚡ Features

* 🔌 Remote Shutdown
* 🔄 Restart System
* 💤 Sleep Mode
* ⚡ Power ON (Wake-on-LAN)
* 📡 Works on LAN & Internet
* 📱 Mobile friendly web UI
* 🔐 Extendable authentication support

---

## 🏗️ System Flow

```
Browser / Mobile
       ↓
ASP.NET Core API
       ↓
Windows Service (C#)
       ↓
System Power Actions
```

---

## 🛠️ Tech Stack

* Backend: ASP.NET Core Web API
* Client: C# Windows Service (.NET Worker Service)
* Frontend: HTML, CSS, JavaScript
* Communication: REST API (HTTP)

---

## ⚙️ How It Works

1. Web panel sends a command (shutdown/restart/sleep)
2. API stores or forwards the command
3. Windows service polls the API
4. Service executes system command on the machine

---

## 📡 API Endpoints

### Send Command

```
POST /api/device/command
```

```json
{
  "action": "shutdown"
}
```

---

### Get Command

```
GET /api/device/command?deviceId=PC-01
```

---

## 🌐 Web Panel

* Simple HTML, CSS, JavaScript interface
* Buttons for power control actions
* Calls backend API using fetch()

---

## ⚙️ Windows Service

The service runs in background and executes system commands:

* shutdown → `shutdown /s /t 0`
* restart → `shutdown /r /t 0`
* sleep → `rundll32.exe powrprof.dll,SetSuspendState`

---

## 🔐 Security Notes

For production use, add:

* JWT Authentication
* HTTPS (SSL)
* API Key validation
* Device ID validation

---

## 📱 Mobile Support

Fully responsive web panel allows control from:

* Android
* iOS
* Any browser device

---

## 💡 Future Improvements

* Real-time WebSocket control
* Multi-device dashboard
* Scheduled shutdown/restart
* Push notifications
* Android/iOS app

---

## 👨‍💻 Author

**Ajeet (c) 2026**

---

## 📄 License

This project is licensed under **MIT License**
See `LICENSE` file for details.

---

## ⭐ Support

If you like this project:

* ⭐ Star the repo
* 🍴 Fork it
* 🔧 Improve it

---

