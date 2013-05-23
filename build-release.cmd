@echo off

REM This build script is used internally to build fitSharp for .NET 4.0, in Release and targetted for x86, which
REM happens to be a local requirement for our system under test.
setlocal

set TAG_NAME=%1
if "%TAG_NAME%"=="" (
  echo WARNING: No tag name provided. Building development version. Do not put this in an internal release!
  echo To build a proper release, you must set a git tag before building, and use that tag name for traceability.
  pause
  
  set VERSION_TEXT=*** Internal development build ***
) else (
  set VERSION_TEXT=Built from http://github.com/kimgr/fitsharp/tree/%TAG_NAME%
)

set FXVER=%2
if "%FXVER%"=="" (
  set FXVER=v4.0
) else (
  if not "%FXVER:~0,1%"=="v" (
    set FXVER=v%FXVER%
  )
)

REM Generate InternalVersionInfo.cs
call :WriteInternalVersionInfoHeader
echo [assembly: AssemblyInformationalVersion^("%VERSION_TEXT%"^)] >> source\InternalVersionInfo.cs

call build.cmd "/p:TargetFrameworkVersion=%FXVER%;Config=Release"

set EXIT_CODE=%ERRORLEVEL%

REM Reset InternalVersionInfo.cs to empty state
call :WriteInternalVersionInfoHeader

exit /B %EXIT_CODE%

:WriteInternalVersionInfoHeader
  echo using System.Reflection; > source\InternalVersionInfo.cs
  echo using System.Runtime.InteropServices; >> source\InternalVersionInfo.cs
  echo. >> source\InternalVersionInfo.cs
  goto :eof
