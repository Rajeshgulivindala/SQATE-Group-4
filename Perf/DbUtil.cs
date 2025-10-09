
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace HospitalManagementSystem.Perf
{
    public static class DbUtil
    {
        public static string Cs =>
            ConfigurationManager.ConnectionStrings["HMSDatabaseConnectionString"]?.ConnectionString
            ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public static async Task<int> ExecScalarAsync(string sql)
        {
            using (var con = new SqlConnection(Cs))
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand(sql, con))
                {
                    var o = await cmd.ExecuteScalarAsync();
                    return (o == null || o is DBNull) ? 0 : Convert.ToInt32(o);
                }
            }
        }
    }
}
