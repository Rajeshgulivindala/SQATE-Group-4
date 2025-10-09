
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HospitalManagementSystem.Load
{
    public static class Loader
    {
        private static string Cs => ConfigurationManager.ConnectionStrings["HMSDatabaseConnectionString"]?.ConnectionString
            ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public static async Task Run(int users, int seconds)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
            var latencies = new ConcurrentBag<long>();
            var errors = new ConcurrentBag<string>();

            Task[] workers = Enumerable.Range(0, users).Select(i => Task.Run(async () =>
            {
                var rnd = new Random(i*7919);
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        await ScenarioAsync(rnd);
                        sw.Stop();
                        latencies.Add(sw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.GetType().Name + ": " + ex.Message);
                    }
                }
            }, cts.Token)).ToArray();

            await Task.WhenAll(workers);

            var arr = latencies.ToArray();
            Array.Sort(arr);
            double P(int p) => arr.Length == 0 ? 0 : arr[(int)Math.Min(arr.Length - 1, Math.Ceiling(p / 100.0 * arr.Length) - 1)];

            Console.WriteLine("\n=== Load Results ===");
            Console.WriteLine($"Requests: {arr.Length}");
            Console.WriteLine($"Avg (ms): {(arr.Length==0?0:arr.Average()):F1}");
            Console.WriteLine($"P50: {P(50):F0}  P90: {P(90):F0}  P95: {P(95):F0}  P99: {P(99):F0}");
            Console.WriteLine($"Errors: {errors.Count}");
        }

        private static async Task ScenarioAsync(Random rnd)
        {
            using (var con = new SqlConnection(Cs))
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Appointments WITH (NOLOCK)", con))
                { await cmd.ExecuteScalarAsync(); }
                using (var cmd = new SqlCommand("SELECT TOP (1) * FROM dbo.Patients WITH (NOLOCK) ORDER BY PatientID DESC", con))
                {
                    using (var r = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow))
                    { if (await r.ReadAsync()) { var _ = r[0]; } }
                }
            }
        }
    }
}
