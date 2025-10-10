using System.Linq;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Tests.Infrastructure; // BaseIntegrationTest

namespace HospitalManagementSystem.Tests.Db
{
    [TestClass, TestCategory("Integration-DB")]
    public class CoreTablesExistTests : BaseIntegrationTest
    {
        private static bool AnyTableExists(SqlConnection con, params string[] names)
        {
            const string sql = "SELECT name FROM sys.tables;";
            using (var cmd = new SqlCommand(sql, con))
            using (var r = cmd.ExecuteReader())
            {
                var all = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                while (r.Read()) all.Add(r.GetString(0));
                return names.Any(n => all.Contains(n));
            }
        }

        [TestMethod]
        public void Patients_Table_Exists_Under_Common_Names()
        {
            using (var con = new SqlConnection(Cs))
            {
                con.Open();
                Assert.IsTrue(AnyTableExists(con, "Patients", "Patient"),
                    "Expected a Patients table (Patients/Patient).");
            }
        }

        [TestMethod]
        public void Appointments_And_Rooms_Tables_Exist()
        {
            using (var con = new SqlConnection(Cs))
            {
                con.Open();
                // Rooms and Appointments are referenced throughout the app
                Assert.IsTrue(AnyTableExists(con, "Appointments", "Appointment"),
                    "Expected an Appointments table (Appointments/Appointment).");
                Assert.IsTrue(AnyTableExists(con, "Rooms", "Room"),
                    "Expected a Rooms table (Rooms/Room).");
            }
        }
    }
}
