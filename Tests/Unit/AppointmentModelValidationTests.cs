using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Models;
using System;

namespace HospitalManagementSystem.Tests
{
    [TestClass]
    public class AppointmentModelValidationTests
    {
        [TestMethod]
        public void ValidAppointment_PassesValidation()
        {
            var a = new Appointment
            {
                PatientID = 1,                           // required
                StaffID = 1,                             // required (NOT DoctorID)
                AppointmentDate = DateTime.Now.AddDays(1),// required
                Type = "Checkup",                        // required (StringLength(50))
                Reason = "Follow-up"                     // optional, <= 255
            };

            var results = ValidationTestHelper.ValidateObject(a);
            Assert.AreEqual(0, results.Count,
                "A valid Appointment should have no validation errors.");
        }

        // (Optional) prove validation triggers when Type is missing
        [TestMethod]
        public void MissingType_ShouldFailValidation()
        {
            var a = new Appointment
            {
                PatientID = 1,
                StaffID = 1,
                AppointmentDate = DateTime.Now.AddDays(1),
                // Type missing on purpose
                Reason = "Follow-up"
            };

            var results = ValidationTestHelper.ValidateObject(a);
            Assert.IsTrue(results.Count > 0, "Expected validation errors when Type is missing.");
        }
    }
}
