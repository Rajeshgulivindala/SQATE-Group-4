using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Services.Authentication;
using System.Linq;

namespace HospitalManagementSystem.Tests
{
    [TestClass]
    public class PasswordResetServiceTests
    {
        [TestMethod]
        public void GenerateTemporaryPassword_HasReasonableComplexity()
        {
            var svc = new PasswordResetService();
            var pwd = svc.GenerateTemporaryPassword();

            Assert.IsTrue(pwd.Length >= 8);
            Assert.IsTrue(pwd.Any(char.IsLower));
            Assert.IsTrue(pwd.Any(char.IsUpper));
            Assert.IsTrue(pwd.Any(char.IsDigit));
        }
    }
}
