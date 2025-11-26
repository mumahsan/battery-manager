@echo off
echo Uninstalling Battery Manager...
echo.

REM Stop any running instances
taskkill /F /IM BatteryManagerService.exe 2>nul

REM Remove installation directory
set INSTALL_DIR=%LOCALAPPDATA%\BatteryManager
if exist "%INSTALL_DIR%" (
    echo Removing files from %INSTALL_DIR%...
    rmdir /S /Q "%INSTALL_DIR%"
)

REM Remove startup shortcut
set STARTUP_FOLDER=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
set SHORTCUT=%STARTUP_FOLDER%\BatteryManager.lnk
if exist "%SHORTCUT%" (
    echo Removing startup shortcut...
    del "%SHORTCUT%"
)

echo.
echo Uninstallation complete!
echo.
pause
