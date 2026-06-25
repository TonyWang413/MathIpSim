using Xunit;
using MathIpSim.Core;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MathIpSim.Tests;

public class IntegrationTests
{
    [Fact]
    public void Test_DaemonAndDriverIntegration_Success()
    {
        string shmName = "MathIpSharedMemory"; // Matches Daemon default

        // Find the Daemon DLL path
        // During dotnet test, current directory is: tests/MathIpSim.Tests/bin/Debug/net10.0/
        // Daemon path should be: ../../../../src/MathIpSim.Daemon/bin/Debug/net10.0/MathIpSim.Daemon.dll
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string daemonDllPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "MathIpSim.Daemon", "bin", "Debug", "net10.0", "MathIpSim.Daemon.dll"));

        // Let's ensure the Daemon is built
        Assert.True(File.Exists(daemonDllPath), $"Daemon DLL not found at: {daemonDllPath}. Make sure the daemon project is built.");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{daemonDllPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };

        Process? daemonProcess = null;
        try
        {
            // 1. Start the Daemon
            daemonProcess = Process.Start(psi);
            Assert.NotNull(daemonProcess);

            // Wait a moment for the Daemon to initialize the Shared Memory
            Thread.Sleep(800);

            // 2. Instantiate and initialize Driver
            using (var driver = new MathIpDriver())
            {
                bool initOk = driver.Init(shmName);
                Assert.True(initOk, "Driver failed to connect to Shared Memory.");

                // 3. Write input arrays
                short[] aData = { 10, 20, 30 };
                short[] bData = { 2, 4, 5 };
                
                // Write inputs to offsets relative to Data Space
                driver.WriteData(0x1000, aData, 3);
                driver.WriteData(0x2000, bData, 3);

                // 4. Configure register parameters
                driver.A_ADDRESS = 0x1000;
                driver.B_ADDRESS = 0x2000;
                driver.C_ADDRESS = 0x3000;
                driver.DATA_LEN = 3;

                // 5. Trigger calculation
                driver.GO = 1;

                // 6. Poll for completion with a timeout (max 2 seconds)
                int timeoutMs = 2000;
                int elapsedMs = 0;
                while (driver.GO == 1 && elapsedMs < timeoutMs)
                {
                    Thread.Sleep(10);
                    elapsedMs += 10;
                }

                // Verify calculation completed in time
                Assert.Equal(0u, driver.GO);
                Assert.Equal(0u, driver.STATUS);

                // 7. Read output results (3 datasets * 4 operations = 12 elements)
                short[] cData = new short[12];
                driver.ReadData(0x3000, cData, 12);

                // Expected results:
                // 10 + 2 = 12, 10 - 2 = 8,  10 * 2 = 20, 10 / 2 = 5
                // 20 + 4 = 24, 20 - 4 = 16, 20 * 4 = 80, 20 / 4 = 5
                // 30 + 5 = 35, 30 - 5 = 25, 30 * 5 = 150, 30 / 5 = 6
                short[] expected = {
                    12, 8, 20, 5,
                    24, 16, 80, 5,
                    35, 25, 150, 6
                };

                Assert.Equal(expected, cData);
            }
        }
        finally
        {
            // Clean up the Daemon process
            if (daemonProcess != null && !daemonProcess.HasExited)
            {
                daemonProcess.Kill();
                daemonProcess.WaitForExit();
            }
        }
    }
}
