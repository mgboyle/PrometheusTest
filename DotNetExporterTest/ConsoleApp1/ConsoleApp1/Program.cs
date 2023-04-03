using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Prometheus;

namespace MyNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            // create a new HTTP server to expose metrics
            var server = new MetricServer(port: 1234);
            server.Start();

            // create a gauge to track the total CPU usage of the system
            var cpuUsageTotal = Metrics.CreateGauge("cpu_usage_total", "Total CPU usage of the system");

            // create a gauge to track the CPU usage of the top 5 processes by CPU usage
            var cpuUsageTop5 = Metrics.CreateGauge("cpu_usage_top_5", "CPU usage of the top 5 processes by CPU usage", new GaugeConfiguration
            {
                LabelNames = new[] { "process_name" }
            });

            // start a background task to update the metrics
            Task.Run(async () =>
            {
                var processTimes = new Dictionary<int, TimeSpan>();
                var lastTotalTime = TimeSpan.MinValue;
                while (true)
                {
                    // Get the total system time
                    var totalTime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                    var systemTimeDelta = totalTime - lastTotalTime;
                    lastTotalTime = totalTime;

                    // Get the processes and their previous TotalProcessorTimes
                    var processes = Process.GetProcesses().ToDictionary(p => p.Id, p => p);

                    // Update the processTimes dictionary with the new TotalProcessorTimes
                    processTimes = processes.ToDictionary(p => p.Key, p => p.Value.TotalProcessorTime);

                    // Calculate the CPU usage for each process and the total CPU usage
                    float totalCpuUsage = 0;
                    foreach (var process in processes)
                    {
                        try
                        {
                            var currentProcessorTime = process.Value.TotalProcessorTime;
                            var previousProcessorTime = processTimes[process.Key];
                            var cpuUsage = (currentProcessorTime - previousProcessorTime).TotalMilliseconds / systemTimeDelta.TotalMilliseconds * 100;
                            totalCpuUsage += (float)cpuUsage;

                            // Update the processTimes dictionary with the new TotalProcessorTime
                            processTimes[process.Key] = currentProcessorTime;

                            // Update the CPU usage gauge for the process
                            cpuUsageTop5.WithLabels(process.Value.ProcessName).Set(cpuUsage);
                        }
                        catch
                        {
                            // ignore any exceptions and move on to the next process
                        }
                    }

                    // Update the total CPU usage gauge
                    cpuUsageTotal.Set(totalCpuUsage);

                    // Wait for a few seconds before updating the metrics again
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            });

            // wait for the user to press a key to exit the program
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            // stop the HTTP server
            server.Stop();
        }
    }
}
