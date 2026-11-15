@echo off
setlocal
echo Building Split Counter Pro...

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo Error: .NET Framework 64-bit C# compiler not found at "%CSC%".
    exit /b 1
)

"%CSC%" /nologo /unsafe /optimize+ /target:winexe /win32icon:"%~dp0src\app.ico" /out:"%~dp0SplitCounter.exe" "%~dp0src\Program.cs"
if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Build succeeded: SplitCounter.exe
exit /b 0
