@echo off
REM Build OpenRouter Budget Tray - Windows 11
REM Produces a single self-contained .exe in publish\
cd /d "%~dp0"
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

echo Cleaning...
if exist publish rmdir /s /q publish

echo Building release...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish

if errorlevel 1 (
    echo.
    echo BUILD FAILED. See errors above.
    pause
    exit /b 1
)

echo.
echo  Build complete!
echo.
echo  Output: publish\OpenRouterBudget.exe
echo.
echo  1. Copy publish\OpenRouterBudget.exe to a folder
echo  2. Create config.json next to it with your API key
echo  3. Double-click to run
echo.

REM Copy config template if it doesn't exist in publish
if not exist publish\config.json (
    echo {"api_key":"sk-or-v1-PASTE_YOUR_KEY_HERE"} > publish\config.json
    echo  Created publish\config.json - edit it with your API key
)

pause
