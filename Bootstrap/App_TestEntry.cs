
using System;
using System.Linq;
using BenchmarkDotNet.Running;
using HospitalManagementSystem.Load;
using HospitalManagementSystem.Perf;

namespace HospitalManagementSystem.Bootstrap
{
    public static class TestEntry
    {
        public static bool TryHandleSpecialModes(string[] args)
        {
            if (args == null || args.Length == 0) return false;
            if (args.Contains("--bench", StringComparer.OrdinalIgnoreCase))
            {
                BenchmarkRunner.Run(typeof(PerfMarker).Assembly);
                return true;
            }
            if (args.Contains("--loadgen", StringComparer.OrdinalIgnoreCase))
            {
                int users = GetIntArg(args, "--users", 10);
                int seconds = GetIntArg(args, "--seconds", 30);
                Console.WriteLine($"Inline LoadGen - Users={users}, Seconds={seconds}");
                Loader.Run(users, seconds).GetAwaiter().GetResult();
                return true;
            }
            return false;
        }
        private static int GetIntArg(string[] args, string name, int def)
        {
            var idx = Array.IndexOf(args, name);
            if (idx >= 0 && idx + 1 < args.Length && int.TryParse(args[idx + 1], out var v)) return v;
            return def;
        }
    }
}
