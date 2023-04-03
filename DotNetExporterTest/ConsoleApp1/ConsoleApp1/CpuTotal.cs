using System;
using System.Diagnostics;

namespace CpuUsageExample
{
    class Program
    {
        static void Main(string[] args)
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            while (true)
            {
                float cpuUsagePercentage = cpuCounter.NextValue();
                Console.WriteLine("CPU Usage: {0}%", cpuUsagePercentage);
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
