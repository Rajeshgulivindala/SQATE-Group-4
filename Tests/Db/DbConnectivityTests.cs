using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Tests.Infrastructure;

namespace HospitalManagementSystem.Tests.Db
{
    [TestClass, TestCategory("Integration-DB")]
    public class DbConnectivityTests : BaseIntegrationTest
    {
        [TestMethod]
        public void Can_Open_Connection()
        {
            using var con = new SqlConnection(Cs);
            con.Open();
            Assert.AreEqual(ConnectionState.Open, con.State);
        }

        [TestMethod]
        public void Core_Tables_Exist()
        {
            using var con = new SqlConnection(Cs);
            con.Open();
            Assert.IsTrue(TableExists(con, "Users"));
            Assert.IsTrue(TableExists(con, "Patients"));
            Assert.IsTrue(TableExists(con, "Appointments"));
        }
    }
}
