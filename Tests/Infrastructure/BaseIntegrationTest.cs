using System.Configuration;
using System.Data.SqlClient;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalManagementSystem.Tests.Infrastructure
{
    public abstract class BaseIntegrationTest
    {
        protected string Cs = string.Empty;
        private TransactionScope _scope;

        [TestInitialize]
        public void Begin()
        {
            Cs = ConfigurationManager.ConnectionStrings["HMSDatabaseConnectionString"]?.ConnectionString
                 ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;MultipleActiveResultSets=True;";
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [TestCleanup]
        public void End() => _scope?.Dispose();

        protected static bool TableExists(SqlConnection con, string table)
        {
            using var cmd = new SqlCommand(
                "SELECT 1 FROM sys.tables WHERE name=@t AND schema_id = SCHEMA_ID('dbo')", con);
            cmd.Parameters.AddWithValue("@t", table);
            return cmd.ExecuteScalar() != null;
        }
    }
}
