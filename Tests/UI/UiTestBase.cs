using System;
using System.IO;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalManagementSystem.Tests.UI
{
    public abstract class UiTestBase
    {
        protected Application App;
        protected UIA3Automation Automation;
        protected Window MainWindow;

        protected virtual string ResolveExePath()
        {
            // Try current + typical bin folders
            string cwd = Directory.GetCurrentDirectory();
            string[] tries =
            {
                Path.Combine(cwd, "HospitalManagementSystem.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HospitalManagementSystem.exe"),
                Path.Combine(cwd, "bin", "Debug", "HospitalManagementSystem.exe"),
                Path.Combine(cwd, "bin", "Release", "HospitalManagementSystem.exe"),
                Path.Combine(Directory.GetParent(cwd)?.FullName ?? cwd, "bin", "Debug", "HospitalManagementSystem.exe"),
            };
            foreach (var p in tries)
            {
                if (File.Exists(p)) return p;
            }
            return tries[0];
        }

        [TestInitialize]
        public void BaseStart()
        {
            var exePath = ResolveExePath();
            App = Application.Launch(exePath);
            Automation = new UIA3Automation();
            MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(10));
            Assert.IsNotNull(MainWindow, "Failed to obtain main window");
        }

        [TestCleanup]
        public void BaseStop()
        {
            try { if (Automation != null) Automation.Dispose(); } catch { }
            try { if (App != null && !App.HasExited) App.Close(); } catch { }
        }
    }
}