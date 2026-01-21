@echo off
REM Build script for PS3 Quick Disc Decryptor
REM Builds the project in Release configuration

setlocal enabledelayedexpansion

set "CONFIGURATION=Release"
set "PROJECT_PATH=Source\PS3 Quick Disc Decryptor\PS3 Quick Disc Decryptor.vbproj"
set "SOLUTION_PATH=Source\PS3 Quick Disc Decryptor.sln"

echo ================================================
echo   PS3 Quick Disc Decryptor - Build Script
echo ================================================
echo.

REM Check if .NET SDK is installed
echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK not found!
    echo Please install .NET 6.0 SDK or later from:
    echo https://dotnet.microsoft.com/download/dotnet/6.0
    exit /b 1
)

for /f "delims=" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo [OK] .NET SDK found: %DOTNET_VERSION%
echo.

REM Restore dependencies
echo Restoring NuGet packages...
dotnet restore "%SOLUTION_PATH%"
if errorlevel 1 (
    echo [ERROR] Restore failed!
    exit /b 1
)
echo [OK] Restore completed
echo.

REM Build
echo Building project (%CONFIGURATION%)...
dotnet build "%SOLUTION_PATH%" --configuration %CONFIGURATION% --no-restore
if errorlevel 1 (
    echo [ERROR] Build failed!
    exit /b 1
)
echo [OK] Build completed successfully!
echo.

REM Show output location
set "OUTPUT_PATH=Source\PS3 Quick Disc Decryptor\bin\%CONFIGURATION%\net6.0-windows"
echo ================================================
echo Build Output Location:
echo   %OUTPUT_PATH%
echo ================================================
echo.
echo [OK] All operations completed successfully!

endlocal
