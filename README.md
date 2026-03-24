# OpenRouter Budget Tray Icon (.NET / C#)

Native Windows 11 system tray icon showing your OpenRouter credits and daily spend. Single self-contained `.exe`, no install needed.

## Features

- Color-coded tray icon (green/yellow/orange/red by remaining budget)
- Hover shows remaining balance + today's spend
- Right-click: today / week / month / last 7 days
- 30-day spend history with bar chart dashboard
- Auto-refreshes every 2 minutes
- Single instance (no duplicates)
- Auto-start on boot

## Requirements

- Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (build-time only)

## Setup

```
1. Run build.bat                  (compiles to single .exe)
2. Edit publish\config.json       (paste your API key)
3. Run publish\OpenRouterBudget.exe
```

To auto-start with Windows: run `install_autostart.bat`.

## Get Your API Key

https://openrouter.ai/settings/keys → create a key → paste in `config.json`

## Files

| File | Purpose |
|---|---|
| `build.bat` | Compile to single `.exe` |
| `install_autostart.bat` | Add to Windows Startup |
| `uninstall_autostart.bat` | Remove from Startup |
| `config.json` | API key (created on build) |
| `history.json` | Daily spend log (auto-generated) |
| `dashboard.html` | 30-day chart (auto-generated) |
