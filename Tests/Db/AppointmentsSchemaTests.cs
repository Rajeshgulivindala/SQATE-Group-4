using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Tests.Infrastructure; // BaseIntegrationTest with Cs

namespace HospitalManagementSystem.Tests.Db
{
    [TestClass, TestCategory("Integration-DB")]
    public class AppointmentsSchemaTests : BaseIntegrationTest
    {
        private static bool ColumnExists(SqlConnection con, string table, string column)
        {
            const string sql = @"
SELECT 1
FROM sys.columns c
JOIN sys.tables  t ON t.object_id = c.object_id
WHERE t.name = @t AND c.name = @c;";
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@c", column);
                return cmd.ExecuteScalar() != null;
            }
        }

        [TestMethod]
        public void Appointments_Has_Core_Columns()
        {
            using (var con = new SqlConnection(Cs))
            {
                con.Open();
                const string T = "Appointments";

                // Core columns typically present in your XAML/view code-behind:
                string[] required = { "AppointmentID", "PatientID", "StaffID", "RoomID", "AppointmentDate" };

                foreach (var col in required)
                {
                    Assert.IsTrue(ColumnExists(con, T, col), $"Missing column {col} in table {T}.");
                }
            }
        }
    }
}
