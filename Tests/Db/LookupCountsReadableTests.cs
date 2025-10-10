using System;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Tests.Infrastructure; // BaseIntegrationTest

namespace HospitalManagementSystem.Tests.Db
{
    [TestClass, TestCategory("Integration-DB")]
    public class LookupCountsReadableTests : BaseIntegrationTest
    {
        private static int? TryCount(SqlConnection con, string table)
        {
            try
            {
                using (var cmd = new SqlCommand($"SELECT COUNT(*) FROM dbo.{table};", con))
                {
                    var o = cmd.ExecuteScalar();
                    return (o == null || o == DBNull.Value) ? (int?)null : Convert.ToInt32(o);
                }
            }
            catch { return null; }
        }

        [TestMethod]
        public void Can_Read_Counts_From_Staff_And_Rooms_If_Present()
        {
            using (var con = new SqlConnection(Cs))
            {
                con.Open();

                // Staff can be named Staffs or Staff in different schemas
                var staffCount = TryCount(con, "Staffs") ?? TryCount(con, "Staff");
                var roomsCount = TryCount(con, "Rooms") ?? TryCount(con, "Room");

                // Pass as long as we can query at least one of them; zero rows are OK.
                Assert.IsTrue(staffCount.HasValue || roomsCount.HasValue,
                    "Expected to be able to query either Staffs/Staff or Rooms/Room.");
            }
        }
    }
}
