# [SUPERSEDED] Implementation Plan: Pure-Software Math IP Simulator

> [!WARNING]
> **此版本計畫已廢棄 (SUPERSEDED by July 6 refactor)**。最新的實施與驗證計畫請參閱：[2026-07-06-restructured-ip-sim-plan.md](2026-07-06-restructured-ip-sim-plan.md)。

This plan breaks down the approved specification ([2026-06-25-software-ip-sim.md](../specs/2026-06-25-software-ip-sim.md)) into bite-sized tasks.
We will follow **Test-Driven Development (TDD)** for each task.

---

## Task List

### Phase 1: Project Setup & Core Logic (TDD)
- [ ] **Task 1: Project Directory Structure & C# Projects Setup**
  - Create directory structure: `src/` and `tests/`.
  - Create `src/MathIpSim.Core/MathIpSim.Core.csproj` targeting `netstandard2.0`.
  - Create `tests/MathIpSim.Tests/MathIpSim.Tests.csproj` targeting `.NET 8.0` (for testing the netstandard2.0 core).
  - *Verification*: Run `dotnet restore` successfully.
- [ ] **Task 2: Write Math Engine Unit Tests (TDD - RED)**
  - Write test cases in `tests/MathIpSim.Tests/EngineTests.cs` for math operations (add, sub, mul, div, divide-by-zero, saturation, status flags).
  - *Verification*: Run `dotnet test` and watch tests fail.
- [ ] **Task 3: Implement Core Math IP Calculation Logic (TDD - GREEN)**
  - Implement `MathIpEngine.cs` in `src/MathIpSim.Core/` that accepts a memory span/array, reads input data, performs math, applies overflow/underflow clamps, updates STATUS, and clears GO.
  - *Verification*: Run `dotnet test` and verify all core math logic tests pass.

### Phase 2: Shared Memory & Daemon Implementation
- [ ] **Task 4: Implement Shared Memory Wrapper**
  - Implement `SharedMemoryWrapper.cs` using `System.IO.MemoryMappedFiles.MemoryMappedFile` supporting Windows Named Memory and macOS POSIX/file-backed MemoryMappedFile.
  - *Verification*: Write a test in `tests/MathIpSim.Tests/SharedMemoryTests.cs` to write/read across mappings.
- [ ] **Task 5: Implement C# Daemon Process**
  - Create `src/MathIpSim.Daemon/` project and implement the main loop that spins up the shared memory, runs a polling loop on the `GO` register, and invokes the `MathIpEngine`.
  - *Verification*: Run daemon in background; verify it creates the shared memory segment.

### Phase 3: Client Drivers & Integration Testing
- [ ] **Task 6: Implement C# Client Driver (`MathIpDriver`)**
  - Implement `MathIpDriver.cs` in `src/MathIpSim.Client/` exposing properties for registers and helper methods for writing/reading data.
  - Write integration test `tests/MathIpSim.Tests/IntegrationTests.cs` that launches the daemon, runs math via the driver, and asserts results.
  - *Verification*: Run integration tests successfully.
- [ ] **Task 7: Implement C Client Library (`math_ip_client`)**
  - Implement `src/MathIpSim.CClient/math_ip_client.h` and `math_ip_client.c` with cross-platform shared memory mapping (`shm_open` for macOS, `OpenFileMapping` for Windows).
  - *Verification*: Compile C code using `clang`/`gcc`.
- [ ] **Task 8: Write C Client Integration Test & Verify**
  - Create a C test client `tests/c_test/main.c`.
  - Run the C# Daemon in the background.
  - Run the C client executable to verify complete end-to-end flow, including overflow and divide-by-zero flags.
  - *Verification*: Execution output matches expected results exactly.
