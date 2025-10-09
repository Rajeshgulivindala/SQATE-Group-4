
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HospitalManagementSystem.Perf
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class CommonQueryBenchmarks
    {
        [Benchmark] public Task<int> CountPatients() => DbUtil.ExecScalarAsync("SELECT COUNT(*) FROM dbo.Patients WITH (NOLOCK)");
        [Benchmark] public Task<int> CountAppointments() => DbUtil.ExecScalarAsync("SELECT COUNT(*) FROM dbo.Appointments WITH (NOLOCK)");
        [Benchmark] public Task<int> CountUsers() => DbUtil.ExecScalarAsync("SELECT COUNT(*) FROM dbo.Users WITH (NOLOCK)");
    }
}
