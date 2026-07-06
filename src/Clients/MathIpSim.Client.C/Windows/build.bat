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
if not exist "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" goto check_gcc

for /f "usebackq tokens=*" %%i in (`"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do (
    set "VS_PATH=%%i"
)

:: 3. If VS path found, try to initialize MSVC compiler environment
if not defined VS_PATH goto check_gcc
if not exist "%VS_PATH%\VC\Auxiliary\Build\vcvarsall.bat" goto check_gcc

echo [INFO] Found Visual Studio at: %VS_PATH%
echo [INFO] Initializing MSVC environment (x64)...
call "%VS_PATH%\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul
where cl >nul 2>nul
if %ERRORLEVEL% equ 0 goto build_msvc

:check_gcc
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
