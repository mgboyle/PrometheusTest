﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            Task.Run(() =>
            {
                while (true)
                {
                    // get the total CPU time used by all processes on the system
                    var cpuTime = new TimeSpan();
                    var processes = new List<Process>();
                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            cpuTime += process.TotalProcessorTime;
                            processes.Add(process);
                        }
                        catch
                        {
                            // ignore any exceptions and move on to the next process
                        }
                    }

                    // calculate the CPU usage as a percentage of the total available CPU capacity on the system
                    var totalCpuTime = Environment.TickCount * Environment.ProcessorCount;
                    var cpuUsagePercentageTotal = (float)(cpuTime.TotalMilliseconds / totalCpuTime);

                    // update the total CPU usage gauge
                    cpuUsageTotal.Set(cpuUsagePercentageTotal);

                    // get the top 5 processes by CPU usage
                    var top5Processes = processes.OrderByDescending(p => p.TotalProcessorTime).Take(5);

                    // update the CPU usage gauge for each of the top 5 processes
                    foreach (var process in top5Processes)
                    {
                        var cpuUsagePercentageTop5 = (float)(process.TotalProcessorTime.TotalMilliseconds / totalCpuTime);
                        cpuUsageTop5.WithLabels(process.ProcessName).Set(cpuUsagePercentageTop5);
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