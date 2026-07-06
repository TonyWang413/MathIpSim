# Specification: Fix & Enhance C Client Windows Build Script

This specification outlines the plan to fix syntax errors and enhance compiler detection in `src/Clients/MathIpSim.Client.C/Windows/build.bat` when executing on Windows.

## Problem Description
1. **Syntax Error**: The batch file uses parenthesized `if` blocks containing characters like `)` (e.g., inside `echo` statements). This causes `cmd.exe` to prematurely close the `if` block, leading to syntax errors like `At this time, . was unexpected.` (此時不應有 .).
2. **Usability/Compiler Detection**: The script fails with a warning when the user runs it from a regular command prompt (or PowerShell) because MSVC compiler paths (`cl.exe`) are not configured in the standard system `PATH` by default.

## Proposed Solution

### Part 1: Flat Label/Goto Control Flow (Already Approved & Implemented)
Instead of using multi-line parenthesized blocks, restructure the script using flat `if` conditions and `goto` jumps to labels. This avoids the nested parentheses issue entirely and makes the script more robust.

### Part 2: Automatic Visual Studio & MSVC Path Detection (New)
To improve developer experience, the script will automatically check if MSVC (`cl.exe`) is in the PATH. If not, it will look for the Visual Studio installer helper `vswhere.exe` (typically at `C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe`). It will query the installation path of the latest Visual Studio instance containing C++ build tools, and call `vcvarsall.bat x64` to load the environment variables dynamically.

### New Script Design
```batch
@echo off
:: Automatically change directory to where the script is located
cd /d "%~dp0"
echo =======================================
echo    Building Math IP C Demo (Windows)
echo =======================================

:: 1. Check if cl is already in the PATH
where cl >nul 2>nul
if %ERRORLEVEL% equ 0 goto build_msvc

:: 2. Try to automatically locate Visual Studio using vswhere
if exist "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" (
    for /f "usebackq tokens=*" %%i in (`"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do (
        set "VS_PATH=%%i"
    )
)

:: 3. If VS path found, try to initialize MSVC compiler environment
if defined VS_PATH (
    if exist "%VS_PATH%\VC\Auxiliary\Build\vcvarsall.bat" (
        echo [INFO] Found Visual Studio at: %VS_PATH%
        echo [INFO] Initializing MSVC environment (x64)...
        call "%VS_PATH%\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul
        where cl >nul 2>nul
        if %ERRORLEVEL% equ 0 goto build_msvc
    )
)

:: 4. Check for MinGW/GCC Compiler (gcc.exe)
where gcc >nul 2>nul
if %ERRORLEVEL% equ 0 goto build_gcc

:: No compiler found
echo [ERROR] No C compiler (cl.exe or gcc.exe) found in your PATH.
echo.
echo To build with MSVC:
echo   Please run this script from the "Developer Command Prompt for VS".
echo.
echo To build with GCC:
echo   Please install MinGW/MSYS2 and add gcc to your system PATH.
echo.
pause
exit /b 1

:build_msvc
echo [INFO] Found MSVC Compiler (cl). Building...
cl /O2 main.c math_ip_driver.c /I . /Fe:c_demo.exe
goto check_result

:build_gcc
echo [INFO] Found MinGW GCC Compiler. Building...
gcc main.c math_ip_driver.c -I . -o c_demo.exe
goto check_result

:check_result
if %ERRORLEVEL% equ 0 goto build_success
echo [ERROR] Build FAILED!
pause
exit /b %ERRORLEVEL%

:build_success
echo ---------------------------------------
echo Build SUCCESSFUL! Created executable: c_demo.exe
echo ---------------------------------------
echo To run the demo (make sure the C# Daemon is running in the background):
echo   c_demo.exe
pause
exit /b 0
```

## Verification Plan
1. **VS Detection & Compilation Verification**: Run `cmd /c "build.bat < NUL"` in the workspace. Since Visual Studio 2022 Community is installed, it should:
   - Successfully find Visual Studio via `vswhere.exe`.
   - Call `vcvarsall.bat x64` to initialize the MSVC environment.
   - Successfully compile `c_demo.exe` using `cl`.
   - Exit with code 0.
2. **Fallback Verification**: If no compiler is found, verify it gracefully exits with code 1.
