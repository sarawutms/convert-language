@echo off
echo Closing running ConvertLanguage.exe if any...
taskkill /F /IM ConvertLanguage.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo Building ConvertLanguage.exe with app.ico...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:"%~dp0app.ico" /out:"%~dp0ConvertLanguage.exe" /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll "%~dp0src\Program.cs"
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo  Build successful! Output: ConvertLanguage.exe
    echo ========================================
) else (
    echo.
    echo Build failed!
)
echo.
pause
