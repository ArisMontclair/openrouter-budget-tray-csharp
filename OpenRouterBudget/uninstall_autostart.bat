@echo off
echo Removing OpenRouter Budget from Startup...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "OpenRouterBudget" /f
echo Done.
pause
