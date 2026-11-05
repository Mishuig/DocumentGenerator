@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-windows.ps1"
if errorlevel 1 (
  echo.
  echo Build failed. Check the error messages above.
  pause
  exit /b 1
)
echo.
echo Portable app is in: %~dp0publish\win-x64
pause
