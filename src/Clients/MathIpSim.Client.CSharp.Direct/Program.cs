using System;
using MathIpSim.Simulator;

namespace MathIpSim.Client.CSharp.Direct
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    C# Direct Client (Scenario 2)       ");
            Console.WriteLine("========================================");

            try
            {
                Console.WriteLine("[INFO] Instantiating MathIpEngine directly in-process...");
                using (var engine = new MathIpEngine())
                {
                    short[] aData = { 10, 20, 30 };
                    short[] bData = { 2, 4, 5 };

                    Console.WriteLine("[INFO] Writing inputs via high-level API...");
                    engine.WriteInputs(aData, bData);

                    Console.WriteLine("[INFO] Executing calculation synchronously...");
                    engine.Execute();

                    uint status = engine.GetStatus();
                    Console.WriteLine($"[INFO] Completed. STATUS: 0x{status:X8}");

                    short[] results = engine.ReadOutputs();

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
                Console.WriteLine($"[FATAL] Direct Client Error: {ex.Message}");
            }
        }
    }
}
