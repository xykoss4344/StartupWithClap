@echo off
echo Waiting 30 seconds for specific system drivers to load...
timeout /t 30 /nobreak >nul
cd /d "%~dp0bin\Release\net10.0"
start "" "ProjectStarkCS.exe"
exit
