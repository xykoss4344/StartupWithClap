@echo off
echo Waiting 3 seconds for system stability...
timeout /t 3 /nobreak >nul
cd /d "%~dp0bin\Release\net10.0"
start "" "ProjectStarkCS.exe"
exit
