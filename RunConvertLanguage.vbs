Set fso = CreateObject("Scripting.FileSystemObject")
currentPath = fso.GetParentFolderName(WScript.ScriptFullName)

Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "powershell.exe -NoProfile -sta -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & currentPath & "\ConvertLanguage.ps1""", 0, False
