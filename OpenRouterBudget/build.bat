@echo off
setlocal EnableDelayedExpansion
REM Build OpenRouter Budget Tray - Windows 11
REM Produces a single self-contained .exe in publish\ (or publish_new\ if publish is locked)
cd /d "%~dp0"
if exist "%ProgramFiles%\dotnet\dotnet.exe" set "PATH=%ProgramFiles%\dotnet;%PATH%"
echo.
echo  Building OpenRouter Budget...
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found.
    echo Install from https://dotnet.microsoft.com/download/dotnet/8.0
    echo Choose "SDK" - x64, Windows
    pause
    exit /b 1
)

echo Stopping OpenRouterBudget if running...
taskkill /F /T /IM OpenRouterBudget.exe >nul 2>&1
timeout /t 2 /nobreak >nul

set PUBLISH_DIR=publish
echo Cleaning...
if exist publish rmdir /s /q publish 2>nul

if exist publish (
    echo.
    echo  NOTE: publish\ is still locked ^(tray app running, or exe open in Cursor/Explorer/antivirus^).
    echo  Building to publish_new\ instead — you can quit the app and delete publish\, then:
    echo    rename publish_new publish
    echo.
    set PUBLISH_DIR=publish_new
    if exist publish_new rmdir /s /q publish_new 2>nul
)

echo Building release...
dotnet publish OpenRouterBudget.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "!PUBLISH_DIR!"

if errorlevel 1 (
    echo.
    echo BUILD FAILED. See errors above.
    if "!PUBLISH_DIR!"=="publish" (
        echo Tip: Exit OpenRouterBudget from the system tray, wait a few seconds, then run this script again.
        echo Or build will use publish_new\ if publish\ stays locked.
    )
    pause
    exit /b 1
)

echo.
echo  Build complete!
echo.
echo  Output: !PUBLISH_DIR!\OpenRouterBudget.exe
echo.
echo  1. Copy !PUBLISH_DIR!\OpenRouterBudget.exe to a folder
echo  2. Create config.json next to it with your API key
echo  3. Double-click to run
echo.

if not exist "!PUBLISH_DIR!\config.json" (
    echo {"api_key":"sk-or-v1-PASTE_YOUR_KEY_HERE"} > "!PUBLISH_DIR!\config.json"
    echo  Created !PUBLISH_DIR!\config.json - edit it with your API key
)

pause
