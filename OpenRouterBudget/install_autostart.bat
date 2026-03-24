@echo off
REM Add OpenRouter Budget to Windows Startup (Registry)
echo.
echo  Installing auto-start via Registry...
echo.

set "EXE_PATH=%~dp0OpenRouterBudget.exe"

if not exist "%EXE_PATH%" (
    echo ERROR: OpenRouterBudget.exe not found next to this script.
    echo Build first with build.bat, then copy the exe here.
    pause
    exit /b 1
)

reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "OpenRouterBudget" /t REG_SZ /d "%EXE_PATH%" /f

if errorlevel 1 (
    echo FAILED. Try running as administrator.
    pause
    exit /b 1
)

echo Done! OpenRouter Budget will now start with Windows.
echo.
echo To remove: run uninstall_autostart.bat
echo            or delete the "OpenRouterBudget" value in
echo            HKCU\Software\Microsoft\Windows\CurrentVersion\Run
echo.
pause
