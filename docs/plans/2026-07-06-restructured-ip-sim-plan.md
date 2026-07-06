# Implementation Plan: Pure-Software Math IP Simulator (Restructured)

This plan breaks down the revised specification ([2026-07-06-restructured-ip-sim.md](../specs/2026-07-06-restructured-ip-sim.md)) into clean, bite-sized tasks using **Test-Driven Development (TDD)** and avoiding `unsafe` C# code.

---

## Task List

### Phase 1: Project Restructuring & Setup
- [ ] **Task 1: Re-create C# Projects and Solution Structure**
  - Remove old C# projects (`src/MathIpSim.Core/`, `src/MathIpSim.Daemon/`).
  - Create `src/MathIpSim.Client.CSharp/MathIpSim.Client.CSharp.csproj` targeting `netstandard2.0` (Class Library).
  - Create `src/MathIpSim.Simulator/MathIpSim.Simulator.csproj` targeting `net10.0` (Console Application), referencing `MathIpSim.Client.CSharp` for shared memory creation.
  - Update `tests/MathIpSim.Tests/MathIpSim.Tests.csproj` targeting `net10.0`, referencing both projects.
  - Update [MathIpSim.slnx](../../MathIpSim.slnx) to reflect this structure.
  - *Verification*: Run `dotnet restore` successfully.

### Phase 2: Core Safe Implementation (TDD)
- [ ] **Task 2: Implement SharedMemoryFactory (Platform-Specific MMF)**
  - Create `SharedMemoryFactory.cs` in `src/MathIpSim.Client.CSharp/`.
  - Implement Windows Named Memory and macOS `/tmp/` file-backed memory-mapped file initialization.
  - *Verification*: Write a test in `SharedMemoryTests.cs` to verify cross-platform memory file creation.
- [ ] **Task 3: Implement MathIpEngine (Non-static, Safe MemoryMappedViewAccessor)**
  - Implement `MathIpEngine.cs` in `src/MathIpSim.Simulator/` as an instantiable class owning its own `MemoryMappedViewAccessor`.
  - Implement `IsGoTriggered()`, `Execute()`, and `Dispose()`.
  - Ensure zero `unsafe` keywords are used.
  - *Verification*: Adapt unit tests in `EngineTests.cs` to test the new object-oriented engine math logic, overflow saturation, and divide-by-zero status flags. Run `dotnet test` and pass.
- [ ] **Task 4: Implement Simulator Daemon Host**
  - Implement `Program.cs` in `src/MathIpSim.Simulator/` to instantiate `MathIpEngine` and run the low-latency polling loop.
  - *Verification*: Run simulator executable; verify it creates the shared memory segment and logs initialization.

### Phase 3: Client Driver & Integration Testing
- [ ] **Task 5: Implement Safe C# Driver (`MathIpDriver`)**
  - Implement `MathIpDriver.cs` in `src/MathIpSim.Client.CSharp/` using safe `MemoryMappedViewAccessor` properties.
  - Adapt C# integration tests in `IntegrationTests.cs` to spin up the new `MathIpSim.Simulator` daemon and perform math using `MathIpDriver`.
  - *Verification*: Run integration tests successfully.
- [ ] **Task 6: Verify C Client Integration**
  - Compile the macOS C client demo/test against the new simulator daemon.
  - Run `tests/c_test/main.c` against the running `MathIpSim.Simulator` daemon.
  - *Verification*: All C assertions pass successfully.

### Phase 4: High-Level Helper APIs for Direct & IPC Calling
- [ ] **Task 7: Implement High-Level APIs in MathIpEngine**
  - Implement `WriteInputs`, `ReadOutputs`, and `GetStatus` in `MathIpEngine.cs` using default offsets.
  - *Verification*: Write a unit test in `EngineTests.cs` using these high-level APIs to perform direct in-process calling (Scenario 2).
- [ ] **Task 8: Implement High-Level APIs in MathIpDriver**
  - Implement `WriteInputs`, `ReadOutputs`, and `Execute()` (polling) in `MathIpDriver.cs`.
  - *Verification*: Write an integration test in `IntegrationTests.cs` using these high-level APIs to perform IPC calling (Scenario 1) without manually dealing with offsets.


