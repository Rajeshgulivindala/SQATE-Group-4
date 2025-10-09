using System;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Tests.Infrastructure;

namespace HospitalManagementSystem.Tests.Db
{
    [TestClass, TestCategory("Integration-DB")]
    public class AppointmentsConstraintTests : BaseIntegrationTest
    {
        [TestMethod]
        public void Invalid_ForeignKeys_Should_Fail()
        {
            using var con = new SqlConnection(Cs); con.Open();
            using var cmd = new SqlCommand(@"
                INSERT INTO dbo.Appointments
                (PatientID, StaffID, RoomID, AppointmentDate, Duration, Type, Status, Reason, Notes, CreatedDate, CreatedBy)
                VALUES (@p,@s,@r,@d,@dur,@t,@st,@re,@no,@cd,@cb);", con);
            cmd.Parameters.AddWithValue("@p", -999);
            cmd.Parameters.AddWithValue("@s", -999);
            cmd.Parameters.AddWithValue("@r", -999);
            cmd.Parameters.AddWithValue("@d", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@dur", 30);
            cmd.Parameters.AddWithValue("@t", "Checkup");
            cmd.Parameters.AddWithValue("@st", "Pending");
            cmd.Parameters.AddWithValue("@re", "Test");
            cmd.Parameters.AddWithValue("@no", "");
            cmd.Parameters.AddWithValue("@cd", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@cb", 1);
            try { cmd.ExecuteNonQuery(); Assert.Fail("Expected FK violation"); }
            catch (SqlException ex) { StringAssert.Contains(ex.Message, "FOREIGN KEY"); }
        }
    }
}
