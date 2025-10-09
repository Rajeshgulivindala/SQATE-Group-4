using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Tests.Infrastructure;

namespace HospitalManagementSystem.Tests.Db
{
    [TestClass, TestCategory("Integration-DB")]
    public class SchemaDiagnosticsTests : BaseIntegrationTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void List_Columns_For_Patients()
        {
            var cols = new List<string>();
            using var con = new SqlConnection(Cs);
            using var cmd = new SqlCommand(@"
                SELECT c.name, ty.name AS type_name, c.is_nullable
                FROM sys.columns c
                JOIN sys.tables t ON t.object_id=c.object_id
                JOIN sys.types  ty ON ty.user_type_id=c.user_type_id
                WHERE t.name=@t ORDER BY c.column_id;", con);
            cmd.Parameters.AddWithValue("@t", "Patients");
            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                cols.Add($"{r.GetString(0)}:{r.GetString(1)}:{(r.GetBoolean(2) ? "NULL" : "NOT NULL")}");
            TestContext.WriteLine("Patients -> " + string.Join(", ", cols));
            Assert.IsTrue(cols.Count > 0, "Patients table not found.");
        }
    }
}
