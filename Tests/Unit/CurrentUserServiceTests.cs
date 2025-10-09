using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Services.Authentication;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Tests
{
    [TestClass]
    public class CurrentUserServiceTests
    {
        [TestMethod]
        public void SetAndClearCurrentUser_Works()
        {
            var svc = new CurrentUserService();
            var user = new User { Username = "alice", Role = "Admin" };

            svc.SetCurrentUser(user);
            Assert.IsTrue(svc.IsAuthenticated);
            Assert.AreEqual("alice", svc.CurrentUser.Username);

            svc.ClearCurrentUser();
            Assert.IsFalse(svc.IsAuthenticated);
            Assert.IsNull(svc.CurrentUser);
        }

        [TestMethod]
        public void HasRole_IsCaseInsensitive()
        {
            var svc = new CurrentUserService();
            svc.SetCurrentUser(new User { Username = "doc", Role = "Doctor" });

            Assert.IsTrue(svc.HasRole("doctor"));
            Assert.IsFalse(svc.HasRole("admin"));
        }

        [TestMethod]
        public void Admin_HasAllPermissions()
        {
            var svc = new CurrentUserService();
            svc.SetCurrentUser(new User { Username = "boss", Role = "Admin" });
            Assert.IsTrue(svc.HasPermission("AnythingAtAll"));
        }
    }
}
