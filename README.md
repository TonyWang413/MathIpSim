# Math IP Software Simulator

This project implements a pure-software version of an Integrated Circuit (IC) mathematical IP block. The core IP logic is developed in C# targeting **.NET Standard 2.0** for cross-platform compatibility (macOS and Windows). 

It is designed using a **Shared Memory Daemon** architecture to achieve process and fault isolation between the C# IP engine and its calling clients.

---

## Architecture Overview

```
 ┌───────────────────────────────────┐        ┌───────────────────────────────────┐
 │       Host Process (Caller)       │        │     Daemon Process (C# Engine)    │
 │   (C# Application or C Program)   │        │     (Runs core math IP logic)     │
 └─────────────────┬─────────────────┘        └─────────────────┬─────────────────┘
                   │                                            │
  1. Write Inputs  │                                            │ 3. Detects GO=1
  2. Set Regs      │        ┌────────────────────────┐          │    Executes math
  & Trigger GO=1   └───────►│  Shared Memory (256KB) │◄─────────┘    Updates STATUS
                            │ - Data Space (0x20000) │               Clears GO=0
  5. Read Outputs  ┌◄───────│ - Reg Space  (0x39000) │
  & Check STATUS   │        └────────────────────────┘
                   ▼
```

### Shared Memory Layout
- **Data Space (`0x20000` ~ `0x30000`)**: Stores input arrays `a`, `b` (at offsets `A_ADDRESS`, `B_ADDRESS`) and outputs `c` (at offset `C_ADDRESS`).
- **Register Space (`0x39000` ~ `0x390FF`)**: Maps control/status parameters:
  - `0x39000`: `A_ADDRESS` (32-bit uint offset)
  - `0x39004`: `B_ADDRESS` (32-bit uint offset)
  - `0x39008`: `C_ADDRESS` (32-bit uint offset)
  - `0x3900C`: `DATA_LEN` (32-bit uint count)
  - `0x39010`: `GO` (32-bit uint trigger, write `1` to start, engine clears it to `0` when done)
  - `0x39014`: `STATUS` (32-bit uint status: **Bit 0** = Division by zero occurred, **Bit 1** = Calculation overflow occurred and saturated)

For detailed specifications, see [docs/specs/2026-06-25-software-ip-sim.md](docs/specs/2026-06-25-software-ip-sim.md).

---

## Prerequisites

- **.NET 10.0 SDK** (Installed on macOS/Windows)
- **C Compiler**:
  - *macOS*: `clang` (via Xcode Command Line Tools)
  - *Windows*: MSVC (`cl.exe`) via Visual Studio, or MinGW (`gcc.exe`)

---

## Directory Structure

- [src/MathIpSim.Core/](src/MathIpSim.Core/): Core C# Library containing the math engine and the safe `MathIpDriver`.
- [src/MathIpSim.Daemon/](src/MathIpSim.Daemon/): C# Console App hosting the shared memory and the engine runner loop.
- [src/MathIpSim.CClient/macOS/](src/MathIpSim.CClient/macOS/): macOS-specific C driver (`math_ip_driver`) and build script.
- [src/MathIpSim.CClient/Windows/](src/MathIpSim.CClient/Windows/): Windows-specific C driver (`math_ip_driver`) and build script.
- [tests/](tests/): C# Unit/Integration tests and C-specific automated tests.

---

## Scenario 1: C# Direct calling

In this scenario, a C# client directly consumes the simulated IP using the safe `MathIpDriver`.

### 1. Build
Build the core library from the root directory:
```bash
dotnet build src/MathIpSim.Core/MathIpSim.Core.csproj
```

### 2. Execution Code Example
```csharp
using MathIpSim.Core;

// 1. Instantiate the driver
using (var driver = new MathIpDriver())
{
    // Initialize connection to Shared Memory (Daemon must be running)
    driver.Init("MathIpSharedMemory");

    // 2. Prepare and write data arrays
    short[] aData = { 1, 2, 3 };
    short[] bData = { 4, 5, 6 };
    driver.WriteData(0x1000, aData, 3);
    driver.WriteData(0x2000, bData, 3);

    // 3. Set control registers
    driver.A_ADDRESS = 0x1000;
    driver.B_ADDRESS = 0x2000;
    driver.C_ADDRESS = 0x3000;
    driver.DATA_LEN  = 3;

    // 4. Trigger GO
    driver.GO = 1;

    // 5. Poll for completion
    while (driver.GO == 1)
    {
        System.Threading.Thread.Sleep(1);
    }

    // 6. Check for status flags
    if ((driver.STATUS & 0x01) != 0) Console.WriteLine("Warning: Div-by-zero!");
    if ((driver.STATUS & 0x02) != 0) Console.WriteLine("Warning: Saturation overflow!");

    // 7. Read outputs (3 elements * 4 operations = 12 results)
    short[] results = new short[12];
    driver.ReadData(0x3000, results, 12);
}
```

---

## Scenario 2: macOS C Integration

In this scenario, a macOS C program calls the C# IP engine.

### 1. Build and Run the C# Daemon (Server)
Start the background Daemon to host the simulated hardware:
```bash
dotnet run --project src/MathIpSim.Daemon/MathIpSim.Daemon.csproj
```

### 2. Compile the macOS C Demo (Client)
Open a new terminal window, navigate to the macOS client directory, and run the build script:
```bash
cd src/MathIpSim.CClient/macOS
sh build.sh
```
This compiles `math_ip_driver.c` and `main.c` into a native executable `c_demo`.

### 3. Run the Demo
```bash
./c_demo
```
The program will connect to the C# Daemon via `/tmp/MathIpSharedMemory`, execute the demo operations, and print the results step-by-step.

---

## Scenario 3: Windows C Integration

In this scenario, a Windows C program calls the C# IP engine.

### 1. Build and Run the C# Daemon (Server)
Open Windows Command Prompt/PowerShell and run:
```cmd
dotnet run --project src/MathIpSim.Daemon/MathIpSim.Daemon.csproj
```

### 2. Compile the Windows C Demo (Client)
Open the **Developer Command Prompt for Visual Studio** (for MSVC `cl.exe`) or a command window with MinGW `gcc.exe` in the PATH. Navigate to the Windows client directory and execute the build batch file:
```cmd
cd src\MathIpSim.CClient\Windows
build.bat
```
This compiles `math_ip_driver.c` and `main.c` into `c_demo.exe`.

### 3. Run the Demo
```cmd
c_demo.exe
```
The program will connect to the C# Daemon via Windows Named Shared Memory `Local\MathIpSharedMemory`, execute operations, and print results.

---

## Verification & Tests

### Run C# Core & Integration Tests
Run all unit and integration tests (validating engine math, boundary overflows, and C# client-server IPC):
```bash
dotnet test
```

### Run C Language Integration Test (macOS/Unix)
With the C# Daemon running in the background, compile and run the automated C assertions:
```bash
clang tests/c_test/main.c src/MathIpSim.CClient/macOS/math_ip_driver.c -I src/MathIpSim.CClient/macOS/ -o tests/c_test/c_test_runner
./tests/c_test/c_test_runner
```
If successful, it will output `All assertions PASSED successfully!`.
