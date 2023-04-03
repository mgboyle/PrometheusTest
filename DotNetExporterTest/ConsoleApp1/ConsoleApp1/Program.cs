using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

            // create a gauge to track the available disk space for each drive
            var diskSpaceAvailable = Metrics.CreateGauge("disk_space_available", "Available disk space for each drive", new GaugeConfiguration
            {
                LabelNames = new[] { "drive" }
            });

            // create a gauge to track the I/O read time for each process
            var ioReadTime = Metrics.CreateGauge("io_read_time", "I/O read time for each process", new GaugeConfiguration
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

                    // update the available disk space gauge for each drive
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            diskSpaceAvailable.WithLabels(drive.Name).Set(drive.AvailableFreeSpace);
                        }
                    }

                    // update the I/O read time gauge for each process
                    foreach (var process in processes)
                    {
                        try
                        {
                            var ioCounter = new PerformanceCounter("Process", "IO Read Bytes/sec", process.ProcessName, true);
                            ioReadTime.WithLabels(process.ProcessName).Set(ioCounter.NextValue());
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
