using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Models;
using System.Linq;

namespace HospitalManagementSystem.Tests
{
    [TestClass]
    public class UserModelValidationTests
    {
        [TestMethod]
        public void MissingRequiredFields_YieldsValidationErrors()
        {
            var user = new User(); // no required fields set
            var results = ValidationTestHelper.ValidateObject(user);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void Salt_StringLength_IsEnforced()
        {
            var user = new User
            {
                Username = "demo",
                PasswordHash = new string('x', 64),
                Role = "admin",
                Salt = new string('a', 200) // exceeds [StringLength(50)]
            };

            var results = ValidationTestHelper.ValidateObject(user);
            Assert.IsTrue(results.Any(), "Expected a validation error due to Salt length.");
        }
    }
}
