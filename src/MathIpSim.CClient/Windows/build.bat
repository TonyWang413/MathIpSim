@echo off
:: Automatically change directory to where the script is located
cd /d "%~dp0"
echo =======================================
echo    Building Math IP C Demo (Windows)
echo =======================================

:: Check for MSVC Compiler (cl.exe)
where cl >nul 2>nul
if %ERRORLEVEL% equ 0 (
    echo [INFO] Found MSVC Compiler (cl). Building...
    cl /O2 main.c math_ip_driver.c /I . /Fe:c_demo.exe
    goto done
)

:: Check for MinGW/GCC Compiler (gcc.exe)
where gcc >nul 2>nul
if %ERRORLEVEL% equ 0 (
    echo [INFO] Found MinGW GCC Compiler. Building...
    gcc main.c math_ip_driver.c -I . -o c_demo.exe
    goto done
)

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

:done
if %ERRORLEVEL% equ 0 (
    echo ---------------------------------------
    echo Build SUCCESSFUL! Created executable: c_demo.exe
    echo ---------------------------------------
    echo To run the demo (make sure the C# Daemon is running in the background):
    echo   c_demo.exe
) else (
    echo [ERROR] Build FAILED!
)
pause
