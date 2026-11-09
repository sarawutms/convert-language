@echo off
echo Building ConvertLanguage.exe with app.ico...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:"%~dp0app.ico" /out:"%~dp0ConvertLanguage.exe" /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll "%~dp0src\Program.cs"
if %ERRORLEVEL% EQU 0 (
    echo Build successful! Output: ConvertLanguage.exe
) else (
    echo Build failed!
)
pause
