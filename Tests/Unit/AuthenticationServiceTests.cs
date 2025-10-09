using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace HospitalManagementSystem.Tests
{
    [TestClass]
    public class AuthenticationServiceTests
    {
        // The concrete AuthenticationService uses EF + BCrypt.
        // Keep unit tests DB-free now; real login tests will be integration tests.
        [TestMethod]
        public void AuthenticationService_ClassExists()
        {
            var t = Type.GetType("HospitalManagementSystem.Services.Authentication.AuthenticationService, HospitalManagementSystem");
            Assert.IsNotNull(t, "AuthenticationService class should be present in the app assembly.");
        }
    }
}
