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

            // create a PerformanceCounter to track the total CPU usage of the system
            PerformanceCounter totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            // start a background task to update the metrics
            Task.Run(() =>
            {
                while (true)
                {
                    // update the total CPU usage gauge using the PerformanceCounter
                    cpuUsageTotal.Set(totalCpuCounter.NextValue());

                    // get the top 5 processes by CPU usage
                    var processes = new List<Process>();
                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            processes.Add(process);
                        }
                        catch
                        {
                            // ignore any exceptions and move on to the next process
                        }
                    }

                    var top5Processes = processes.OrderByDescending(p =>
                    {
                        try
                        {
                            using (PerformanceCounter processCpuCounter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName))
                            {
                                return processCpuCounter.NextValue();
                            }
                        }
                        catch
                        {
                            return 0.0f;
                        }
                    }).Take(5);

                    // update the CPU usage gauge for each of the top 5 processes
                    foreach (var process in top5Processes)
                    {
                        try
                        {
                            using (PerformanceCounter processCpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName))
                            {
                                var cpuUsagePercentageTop5 = processCpuCounter.NextValue();
                                cpuUsageTop5.WithLabels(process.ProcessName).Set(cpuUsagePercentageTop5);
                            }
                        }
                        catch
                        {
                            // ignore any exceptions and move on to the next process
                        }
                    }

                    // wait for a few seconds before updating the metrics again
                    Task.Delay(TimeSpan.FromSeconds(5)).Wait();
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
