@echo off
setlocal
cd /d "%~dp0"

echo Closing running ConvertLanguage.exe if any...
taskkill /F /IM ConvertLanguage.exe >nul 2>&1
ping 127.0.0.1 -n 2 >nul

echo Building ConvertLanguage.exe with app.ico and manifest...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:"%~dp0app.ico" /win32manifest:"%~dp0app.manifest" /res:"%~dp0app.ico",ConvertLanguageTray.app.ico /out:"%~dp0ConvertLanguage.exe" /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll "%~dp0src\Program.cs"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Packaging ConvertLanguage-v1.0.0.zip...
    powershell -NoProfile -Command "Compress-Archive -Path '%~dp0ConvertLanguage.exe', '%~dp0README.md', '%~dp0LICENSE' -DestinationPath '%~dp0ConvertLanguage-v1.0.0.zip' -Force"
    echo.
    echo ==============================================================
    echo  Build successful!
    echo  - Output: ConvertLanguage.exe
    echo  - Package: ConvertLanguage-v1.0.0.zip
    echo ==============================================================
) else (
    echo.
    echo ==============================================================
    echo  Build failed!
    echo ==============================================================
)
echo.
pause
