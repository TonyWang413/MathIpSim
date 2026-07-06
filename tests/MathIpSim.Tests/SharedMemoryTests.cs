using Xunit;
using MathIpSim.Simulator;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MathIpSim.Tests;

public class SharedMemoryTests
{
    [Fact]
    public void Test_SharedMemoryWriteAndRead_Success()
    {
        string shmName = "TestMathIpSharedMemory_" + Guid.NewGuid().ToString("N");
        long capacity = 1024;

        // 1. Create first MemoryMappedFile and view accessor, then write data
        using (var mmf1 = SharedMemoryFactory.CreateOrOpen(shmName, capacity))
        using (var accessor1 = mmf1.CreateViewAccessor(0, capacity))
        {
            accessor1.Write(0, (byte)0xAA);
            accessor1.Write(100, (byte)0xBB);
            accessor1.Write(1023, (byte)0xCC);

            // 2. Create second mapping to the same name
            using (var mmf2 = SharedMemoryFactory.CreateOrOpen(shmName, capacity))
            using (var accessor2 = mmf2.CreateViewAccessor(0, capacity))
            {
                // Assert values match across mappings
                Assert.Equal((byte)0xAA, accessor2.ReadByte(0));
                Assert.Equal((byte)0xBB, accessor2.ReadByte(100));
                Assert.Equal((byte)0xCC, accessor2.ReadByte(1023));

                // Modify in accessor2
                accessor2.Write(500, (byte)0xDD);
            }

            // Assert change is visible back in accessor1
            Assert.Equal((byte)0xDD, accessor1.ReadByte(500));
        }

        // Cleanup the backing file on non-Windows platforms
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string filePath = $"/tmp/{shmName}";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
