using System;
using System.Diagnostics;

namespace WpfApp1
{
    public class SystemUsageStats
    {
        public float GetCpuUsage()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return (float)(process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / (DateTime.Now - process.StartTime).TotalMilliseconds * 100);
            }
        }

        public long GetTotalMemory()
        {
            return GC.GetTotalMemory(false) / 1024 / 1024; // Return in MB
        }

        public long GetPrivateMemorySize()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return process.PrivateMemorySize64 / 1024 / 1024; // Return in MB
            }
        }
    }
}
