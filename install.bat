@echo off
echo Installing Battery Manager...
echo.

REM Create installation directory
set INSTALL_DIR=%LOCALAPPDATA%\BatteryManager
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

REM Copy published files
xcopy /Y /E /I "BatteryManagerService\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\*" "%INSTALL_DIR%\"

REM Create startup shortcut
set STARTUP_FOLDER=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
set SHORTCUT=%STARTUP_FOLDER%\BatteryManager.lnk

echo Creating startup shortcut...
powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT%'); $Shortcut.TargetPath = '%INSTALL_DIR%\BatteryManagerService.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Description = 'Battery Manager - Controls charging at 80%%/20%%'; $Shortcut.Save()"

echo.
echo Installation complete!
echo Location: %INSTALL_DIR%
echo.
echo The app will start automatically on next Windows login.
echo To start now, run: "%INSTALL_DIR%\BatteryManagerService.exe"
echo To exit the app, press: Ctrl+Shift+X
echo.
pause
