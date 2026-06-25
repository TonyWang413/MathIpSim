using Xunit;
using MathIpSim.Core;
using System.Runtime.InteropServices;

namespace MathIpSim.Tests;

public class SharedMemoryTests
{
    [Fact]
    public unsafe void Test_SharedMemoryWriteAndRead_Success()
    {
        string shmName = "TestMathIpSharedMemory_" + Guid.NewGuid().ToString("N");
        long capacity = 1024;

        // 1. Create first wrapper and write data
        using (var shm1 = new SharedMemoryWrapper(shmName, capacity))
        {
            Assert.True(shm1.Pointer != null);

            // Write test bytes
            byte* ptr1 = shm1.Pointer;
            ptr1[0] = 0xAA;
            ptr1[100] = 0xBB;
            ptr1[1023] = 0xCC;

            // 2. Create second wrapper mapping to the same name
            using (var shm2 = new SharedMemoryWrapper(shmName, capacity))
            {
                Assert.True(shm2.Pointer != null);

                byte* ptr2 = shm2.Pointer;
                
                // Assert values match across mappings
                Assert.Equal(0xAA, ptr2[0]);
                Assert.Equal(0xBB, ptr2[100]);
                Assert.Equal(0xCC, ptr2[1023]);

                // Modify in shm2
                ptr2[500] = 0xDD;
            }

            // Assert change is visible back in shm1
            Assert.Equal(0xDD, ptr1[500]);
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
