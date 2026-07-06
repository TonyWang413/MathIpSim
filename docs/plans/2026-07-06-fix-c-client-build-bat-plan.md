# Implementation Plan: Fix & Enhance C Client Windows Build Script

This plan details the steps to restructure and enhance compiler detection in [build.bat](file:///D:/Git/MathIpSim/src/Clients/MathIpSim.Client.C/Windows/build.bat).

## Tasks

### Task 1: Add Visual Studio auto-detection in `build.bat`
- **File**: [build.bat](file:///D:/Git/MathIpSim/src/Clients/MathIpSim.Client.C/Windows/build.bat)
- **Action**: Modify the script to include the `vswhere.exe` search and environment setup via `vcvarsall.bat x64` before checking for compilers.
- **Verification**: Read the file to ensure the structure matches Part 2 in the spec.

### Task 2: Verify compilation and execution flow
- **Action**: Run `cmd /c "build.bat < NUL"` from the script's directory.
- **Verification**:
  - Check that the output prints:
    ```
    [INFO] Found Visual Studio at: C:\Program Files\Microsoft Visual Studio\2022\Community
    [INFO] Initializing MSVC environment (x64)...
    [INFO] Found MSVC Compiler (cl). Building...
    ```
  - Verify that `c_demo.exe` is successfully created in `src/Clients/MathIpSim.Client.C/Windows`.
  - Verify that the exit code is `0`.
