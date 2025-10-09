
using System;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalManagementSystem.UiTests
{
    [TestClass]
    public class FlaUITests
    {
        private static string ExePath => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HospitalManagementSystem.exe");

        [TestMethod]
        public void MainWindow_Opens()
        {
            using (var app = FlaUI.Core.Application.Launch(ExePath))
            using (var automation = new UIA3Automation())
            {
                var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
                Assert.IsNotNull(main);
            }
        }
    }
}
