using System;
using System.Threading;

namespace MathIpSim.Client.CSharp.Ipc
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    C# IPC Client (Scenario 1)          ");
            Console.WriteLine("========================================");

            try
            {
                using (var driver = new MathIpDriver())
                {
                    const string shmName = "MathIpSharedMemory";
                    Console.WriteLine($"[INFO] Connecting to Shared Memory '{shmName}'...");
                    
                    if (!driver.Init(shmName))
                    {
                        Console.WriteLine("[ERROR] Failed to connect to Shared Memory. Is the Daemon running?");
                        return;
                    }

                    Console.WriteLine("[INFO] Connected successfully.");

                    short[] aData = { 10, 20, 30 };
                    short[] bData = { 2, 4, 5 };

                    Console.WriteLine("[INFO] Writing inputs to MMF...");
                    driver.WriteData(0x1000, aData, 3);
                    driver.WriteData(0x2000, bData, 3);

                    driver.A_ADDRESS = 0x1000;
                    driver.B_ADDRESS = 0x2000;
                    driver.C_ADDRESS = 0x3000;
                    driver.DATA_LEN = 3;

                    Console.WriteLine("[INFO] Triggering calculation (setting GO = 1)...");
                    driver.GO = 1;

                    Console.WriteLine("[INFO] Polling GO register for completion...");
                    while (driver.GO == 1)
                    {
                        Thread.Sleep(1);
                    }

                    uint status = driver.STATUS;
                    Console.WriteLine($"[INFO] Completed. STATUS: 0x{status:X8}");

                    short[] results = new short[12];
                    driver.ReadData(0x3000, results, 12);

                    Console.WriteLine("[INFO] Results:");
                    for (int i = 0; i < 3; i++)
                    {
                        int baseIdx = i * 4;
                        Console.WriteLine($"  Item {i}: A={aData[i]}, B={bData[i]} -> Add={results[baseIdx]}, Sub={results[baseIdx + 1]}, Mul={results[baseIdx + 2]}, Div={results[baseIdx + 3]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] IPC Client Error: {ex.Message}");
            }
        }
    }
}
