@echo off
cd Core
dotnet publish -c Release
pause
taskkill /F /IM dotnet.exe