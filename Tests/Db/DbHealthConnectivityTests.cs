using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Tests.Infrastructure; // BaseIntegrationTest with Cs

namespace HospitalManagementSystem.Tests.Db
{
    [TestClass, TestCategory("Integration-DB")]
    public class DbHealthConnectivityTests : BaseIntegrationTest
    {
        [TestMethod]
        public void Can_Open_Connection_And_Run_Scalar()
        {
            using (var con = new SqlConnection(Cs))
            {
                con.Open();
                using (var cmd = new SqlCommand("SELECT CAST(1 AS INT)", con))
                {
                    var val = (int)cmd.ExecuteScalar();
                    Assert.AreEqual(1, val);
                }
            }
        }
    }
}
