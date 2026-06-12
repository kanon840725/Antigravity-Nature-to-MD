@echo off
title Nature to MD Compiler
echo Checking C# compiler...

set "csc64=C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
set "csc32=C:\Windows\Microsoft.NET\Framework\v4.0.30319"
set "cscPath="

if exist "%csc64%\csc.exe" (
    set "cscPath=%csc64%"
) else if exist "%csc32%\csc.exe" (
    set "cscPath=%csc32%"
)

if "%cscPath%"=="" (
    echo [ERROR] .NET Framework 4.0 or higher compiler (csc.exe) was not found!
    echo Please make sure .NET Framework is installed.
    pause
    exit /b 1
)

echo Found C# compiler under: %cscPath%
echo Compiling Nature to MD C# code...

"%cscPath%\csc.exe" /r:"%cscPath%\WPF\PresentationCore.dll","%cscPath%\WPF\PresentationFramework.dll","%cscPath%\WPF\WindowsBase.dll","%cscPath%\System.Xaml.dll",System.dll,System.Xml.dll /target:winexe /out:NatureToMD.exe /utf8output App.cs

if %errorlevel% equ 0 (
    echo.
    echo =======================================================
    echo [SUCCESS] Compilation completed! Created NatureToMD.exe
    echo =======================================================
    echo.
) else (
    echo.
    echo [ERROR] Compilation failed with error code %errorlevel%
    echo.
    pause
)
