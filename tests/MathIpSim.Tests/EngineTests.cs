using Xunit;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using MathIpSim.Simulator;

namespace MathIpSim.Tests;

public class EngineTests
{
    private const int ShmSize = 0x40000; // 256 KB
    private const int RegBase = 0x39000;
    private const int DataBase = 0x20000;

    // Registers offset constants
    private const int RegAAddress = RegBase + 0;
    private const int RegBAddress = RegBase + 4;
    private const int RegCAddress = RegBase + 8;
    private const int RegDataLen = RegBase + 12;
    private const int RegGo = RegBase + 16;
    private const int RegStatus = RegBase + 20;

    private void CleanupBackingFile(string shmName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string filePath = $"/tmp/{shmName}";
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch {}
            }
        }
    }

    // =========================================================================
    // Scenario 1: Register-Based / Daemon Mode Tests (Simulates Raw Shared Memory Clients)
    // =========================================================================

    [Fact]
    public void Test_RegisterMode_NormalCalculation_Success()
    {
        string shmName = "TestEngineRegNormal_" + Guid.NewGuid().ToString("N");
        try
        {
            // Arrange - Client writes directly to shared memory offsets
            using (var engine = new MathIpEngine(shmName, ShmSize))
            using (var mmf = SharedMemoryFactory.CreateOrOpen(shmName, ShmSize))
            using (var accessor = mmf.CreateViewAccessor(0, ShmSize))
            {
                // Set registers
                accessor.Write(RegAAddress, 0x1000u); // 0x20000 + 0x1000 = 0x21000
                accessor.Write(RegBAddress, 0x2000u); // 0x20000 + 0x2000 = 0x22000
                accessor.Write(RegCAddress, 0x3000u); // 0x20000 + 0x3000 = 0x23000
                accessor.Write(RegDataLen, 3u);
                accessor.Write(RegGo, 1u);
                accessor.Write(RegStatus, 0xDEADBEEFu); // Initial garbage to check reset

                // Set inputs
                short[] aData = { 1, 2, 3 };
                short[] bData = { 4, 5, 6 };
                accessor.WriteArray(DataBase + 0x1000, aData, 0, aData.Length);
                accessor.WriteArray(DataBase + 0x2000, bData, 0, bData.Length);

                // Act
                engine.Execute();

                // Assert
                uint goValue = accessor.ReadUInt32(RegGo);
                uint statusValue = accessor.ReadUInt32(RegStatus);
                
                short[] cResults = new short[12];
                accessor.ReadArray(DataBase + 0x3000, cResults, 0, cResults.Length);

                Assert.Equal(0u, goValue);
                Assert.Equal(0u, statusValue);

                short[] expected = {
                    5, -3, 4, 0,     // 1, 4 calculations
                    7, -3, 10, 0,    // 2, 5 calculations
                    9, -3, 18, 0     // 3, 6 calculations
                };
                Assert.Equal(expected, cResults);
            }
        }
        finally
        {
            CleanupBackingFile(shmName);
        }
    }

    [Fact]
    public void Test_RegisterMode_DivisionByZero_SetsErrorFlag()
    {
        string shmName = "TestEngineRegDivByZero_" + Guid.NewGuid().ToString("N");
        try
        {
            // Arrange
            using (var engine = new MathIpEngine(shmName, ShmSize))
            using (var mmf = SharedMemoryFactory.CreateOrOpen(shmName, ShmSize))
            using (var accessor = mmf.CreateViewAccessor(0, ShmSize))
            {
                accessor.Write(RegAAddress, 0x1000u);
                accessor.Write(RegBAddress, 0x2000u);
                accessor.Write(RegCAddress, 0x3000u);
                accessor.Write(RegDataLen, 1u);
                accessor.Write(RegGo, 1u);

                accessor.WriteArray(DataBase + 0x1000, new short[] { 5 }, 0, 1);
                accessor.WriteArray(DataBase + 0x2000, new short[] { 0 }, 0, 1); // Divisor is 0

                // Act
                engine.Execute();

                // Assert
                uint goValue = accessor.ReadUInt32(RegGo);
                uint statusValue = accessor.ReadUInt32(RegStatus);
                
                short[] cResults = new short[4];
                accessor.ReadArray(DataBase + 0x3000, cResults, 0, cResults.Length);

                Assert.Equal(0u, goValue);
                Assert.Equal(1u, statusValue & 0x01); // Bit 0 (DIV_BY_ZERO) must be 1

                short[] expected = { 5, 5, 0, 0 };
                Assert.Equal(expected, cResults);
            }
        }
        finally
        {
            CleanupBackingFile(shmName);
        }
    }

    [Fact]
    public void Test_RegisterMode_OverflowAndUnderflow_SaturatesAndSetsFlag()
    {
        string shmName = "TestEngineRegOverflow_" + Guid.NewGuid().ToString("N");
        try
        {
            // Arrange
            using (var engine = new MathIpEngine(shmName, ShmSize))
            using (var mmf = SharedMemoryFactory.CreateOrOpen(shmName, ShmSize))
            using (var accessor = mmf.CreateViewAccessor(0, ShmSize))
            {
                accessor.Write(RegAAddress, 0x1000u);
                accessor.Write(RegBAddress, 0x2000u);
                accessor.Write(RegCAddress, 0x3000u);
                accessor.Write(RegDataLen, 3u);
                accessor.Write(RegGo, 1u);

                short[] aData = { 32000, -32000, 1000 };
                short[] bData = { 1000, 1000, 100 };
                accessor.WriteArray(DataBase + 0x1000, aData, 0, aData.Length);
                accessor.WriteArray(DataBase + 0x2000, bData, 0, bData.Length);

                // Act
                engine.Execute();

                // Assert
                uint goValue = accessor.ReadUInt32(RegGo);
                uint statusValue = accessor.ReadUInt32(RegStatus);
                
                short[] cResults = new short[12];
                accessor.ReadArray(DataBase + 0x3000, cResults, 0, cResults.Length);

                Assert.Equal(0u, goValue);
                Assert.Equal(2u, statusValue & 0x02); // Bit 1 (OVERFLOW) must be 1
                Assert.Equal(32767, cResults[0]); 
                Assert.Equal(-32768, cResults[5]); 
                Assert.Equal(32767, cResults[10]); 
            }
        }
        finally
        {
            CleanupBackingFile(shmName);
        }
    }

    [Fact]
    public void Test_RegisterMode_GoNotSet_DoesNotExecute()
    {
        string shmName = "TestEngineRegNoGo_" + Guid.NewGuid().ToString("N");
        try
        {
            // Arrange
            using (var engine = new MathIpEngine(shmName, ShmSize))
            using (var mmf = SharedMemoryFactory.CreateOrOpen(shmName, ShmSize))
            using (var accessor = mmf.CreateViewAccessor(0, ShmSize))
            {
                accessor.Write(RegGo, 0u); // GO is 0

                // Act
                engine.Execute();

                // Assert
                uint goValue = accessor.ReadUInt32(RegGo);
                Assert.Equal(0u, goValue);
            }
        }
        finally
        {
            CleanupBackingFile(shmName);
        }
    }

    // =========================================================================
    // Scenario 2: Direct API Mode Tests (Zero Shared Memory / Accessor leaks in Calling Code)
    // =========================================================================

    [Fact]
    public void Test_DirectAPI_NormalCalculation_Success()
    {
        // Arrange - Scenario 2 direct API calling using default constructor (no naming or capacity specified)
        using (var engine = new MathIpEngine())
        {
            short[] aData = { 1, 2, 3 };
            short[] bData = { 4, 5, 6 };

            // Act
            engine.WriteInputs(aData, bData);
            engine.Execute();
            short[] results = engine.ReadOutputs();
            uint status = engine.GetStatus();

            // Assert
            Assert.Equal(0u, status);
            short[] expected = {
                5, -3, 4, 0,
                7, -3, 10, 0,
                9, -3, 18, 0
            };
            Assert.Equal(expected, results);
        }
    }

    [Fact]
    public void Test_DirectAPI_DivisionByZero_SetsErrorFlag()
    {
        // Arrange
        using (var engine = new MathIpEngine())
        {
            short[] aData = { 5 };
            short[] bData = { 0 };

            // Act
            engine.WriteInputs(aData, bData);
            engine.Execute();
            short[] results = engine.ReadOutputs();
            uint status = engine.GetStatus();

            // Assert
            Assert.Equal(1u, status & 0x01); // Bit 0 (DIV_BY_ZERO)
            short[] expected = { 5, 5, 0, 0 };
            Assert.Equal(expected, results);
        }
    }

    [Fact]
    public void Test_DirectAPI_OverflowAndUnderflow_SaturatesAndSetsFlag()
    {
        // Arrange
        using (var engine = new MathIpEngine())
        {
            short[] aData = { 32000, -32000, 1000 };
            short[] bData = { 1000, 1000, 100 };

            // Act
            engine.WriteInputs(aData, bData);
            engine.Execute();
            short[] results = engine.ReadOutputs();
            uint status = engine.GetStatus();

            // Assert
            Assert.Equal(2u, status & 0x02); // Bit 1 (OVERFLOW)
            Assert.Equal(32767, results[0]);  // Addition saturated Max
            Assert.Equal(-32768, results[5]); // Subtraction saturated Min
            Assert.Equal(32767, results[10]); // Multiplication saturated Max
        }
    }

    [Fact]
    public void Test_DirectAPI_RegisterProperties_Success()
    {
        // Arrange - Scenario 2 direct API calling, but customizing register offsets and length
        using (var engine = new MathIpEngine())
        {
            // Configure registers directly via properties
            engine.A_ADDRESS = 0x1000;
            engine.B_ADDRESS = 0x2000;
            engine.C_ADDRESS = 0x3000;
            engine.DATA_LEN = 3;

            // Write input data directly to those offsets
            short[] aData = { 10, 20, 30 };
            short[] bData = { 2, 4, 5 };
            Assert.True(engine.WriteData(engine.A_ADDRESS, aData, 3));
            Assert.True(engine.WriteData(engine.B_ADDRESS, bData, 3));

            // Act
            engine.Execute();

            // Assert
            Assert.Equal(0u, engine.STATUS);
            
            short[] results = new short[12];
            Assert.True(engine.ReadData(engine.C_ADDRESS, results, 12));

            short[] expected = {
                12, 8, 20, 5,
                24, 16, 80, 5,
                35, 25, 150, 6
            };
            Assert.Equal(expected, results);
        }
    }
}
