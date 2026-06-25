using System;
using System.Threading;
using MathIpSim.Core;

namespace MathIpSim.Daemon
{
    class Program
    {
        private const string ShmName = "MathIpSharedMemory";
        private const int ShmSize = 0x40000; // 256 KB

        private const int RegBase = 0x39000;
        private const int OffsetGo = RegBase + 16;
        private const int OffsetStatus = RegBase + 20;

        static unsafe void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("       Math IP Daemon Simulator         ");
            Console.WriteLine("========================================");

            try
            {
                using (var shm = new SharedMemoryWrapper(ShmName, ShmSize))
                {
                    byte* ptr = shm.Pointer;

                    // Initialize registers to 0
                    *(uint*)(ptr + OffsetGo) = 0;
                    *(uint*)(ptr + OffsetStatus) = 0;

                    Console.WriteLine($"[INFO] Shared Memory '{ShmName}' initialized at size {ShmSize} bytes.");
                    Console.WriteLine("[INFO] Unix Backing File (if macOS): /tmp/MathIpSharedMemory");
                    Console.WriteLine("[INFO] Registers mapped at offset 0x39000");
                    Console.WriteLine("[INFO] Data space mapped at offset 0x20000");
                    Console.WriteLine("[INFO] Polling GO register...");

                    bool running = true;
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        Console.WriteLine("\n[INFO] Shutting down Daemon...");
                        running = false;
                        e.Cancel = true;
                    };

                    while (running)
                    {
                        // Check if GO is set to 1
                        uint goValue = *(uint*)(ptr + OffsetGo);
                        if ((goValue & 1) != 0)
                        {
                            Console.WriteLine($"[IP] Trigger detected (GO = {goValue}). Executing calculation...");
                            
                            try
                            {
                                MathIpEngine.Execute(ptr);
                                uint status = *(uint*)(ptr + OffsetStatus);
                                Console.WriteLine($"[IP] Calculation completed. STATUS = 0x{status:X2}. GO cleared to 0.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Engine exception: {ex.Message}");
                                // Clear GO to prevent infinite loop on failure
                                *(uint*)(ptr + OffsetGo) = 0;
                            }
                        }

                        // Sleep to yield CPU, keeping latency low (~1ms polling)
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] Daemon failed to start: {ex.Message}");
            }
        }
    }
}
