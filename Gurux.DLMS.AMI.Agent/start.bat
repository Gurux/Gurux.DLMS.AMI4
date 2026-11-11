@echo off
setlocal
cd /d "%~dp0agent" || exit /b 1
dotnet Gurux.DLMS.AMI.Agent.dll %*
exit /b %errorlevel%