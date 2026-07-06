using System;
using System.Threading;
using MathIpSim.Simulator;

namespace MathIpSim.Daemon
{
    class Program
    {
        private const string ShmName = "MathIpSharedMemory";
        private const int ShmSize = 0x40000; // 256 KB

        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("       Math IP Daemon Simulator         ");
            Console.WriteLine("========================================");

            try
            {
                using (var engine = new MathIpEngine(ShmName, ShmSize))
                {
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
                        if (engine.IsGoTriggered())
                        {
                            Console.WriteLine("[IP] Trigger detected (GO = 1). Executing calculation...");
                            try
                            {
                                engine.Execute();
                                Console.WriteLine("[IP] Calculation completed. GO cleared to 0.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Engine exception during execution: {ex.Message}");
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
