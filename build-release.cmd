@echo off

REM This build script is not necessarily for general consumption.
REM We use it internally to build fitSharp for .NET 3.5 and 4.0, in Release and targetted for x86, which
REM happens to be a local requirement for our system under test.
setlocal

set TAG_NAME=%1
if "%TAG_NAME%"=="" (
  echo You must set a git tag before building, and use that tag name for traceability.
  echo.
  echo Usage: build-release.cmd ^<git tag name^>
  exit /B 1
) else (
  REM Generate InternalVersionInfo.cs based on tag name
  call :WriteInternalVersionInfoHeader
  echo [assembly: AssemblyInformationalVersion^("Built from http://github.com/kimgr/fitsharp/tree/%TAG_NAME%"^)] >> source\InternalVersionInfo.cs
)

call build.cmd /p:TargetFrameworkVersion=v3.5;PlatformTarget=x86;Config=Release
call build.cmd /p:TargetFrameworkVersion=v4.0;PlatformTarget=x86;Config=Release
set EXIT_CODE=%ERRORLEVEL%

REM Reset InternalVersionInfo.cs to empty state
call :WriteInternalVersionInfoHeader

exit /B %EXIT_CODE%

:WriteInternalVersionInfoHeader
  echo using System.Reflection; > source\InternalVersionInfo.cs
  echo using System.Runtime.InteropServices; >> source\InternalVersionInfo.cs
  echo. >> source\InternalVersionInfo.cs
  goto :eof