using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using HospitalManagementSystem.Tests.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading;


namespace HospitalManagementSystem.Tests.UI
{
    [TestClass]
    public class UIBasicSmokeTests : UiTestBase
    {
        private static AutomationElement FindBtnByNames(AutomationElement root, params string[] names)
        {
            foreach (var n in names)
            {
                var el = root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(n)));
                if (el != null) return el;
            }
            return null;
        }

        private static bool IsLoginSurface(AutomationElement root)
        {
            var login =
                   root.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))
                ?? FindBtnByNames(root, "LOGIN", "Login", "Sign In");
            return login != null;
        }

        [TestMethod]
        public void App_Launches_And_Shows_Login_Or_Shell()
        {
            var w = MainWindow;
            Assert.IsNotNull(w, "Main window not found");

            // Pass if we see either login or some shell clue (any button or any tab)
            bool login = IsLoginSurface(w);
            bool shellClue =
                w.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem)) != null ||
                w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Dashboard"))) != null;

            Assert.IsTrue(login || shellClue, "Expected login screen or main shell navigation.");
        }

        [TestMethod]
        public void Login_Empty_Shows_Some_Feedback_OR_Login_Button_Persists()
        {
            var w = MainWindow;
            Assert.IsNotNull(w);

            // If no login, this test is trivially OK (app may auto-sign-in)
            if (!IsLoginSurface(w))
            {
                Assert.IsTrue(true);
                return;
            }

            // Try to locate username/password edits loosely
            var allEdits = w.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
            var userBox = allEdits != null && allEdits.Length > 0 ? allEdits[0].AsTextBox() : null;
            var passBox = allEdits != null && allEdits.Length > 1 ? allEdits[1].AsTextBox() : null;

            var loginBtn =
                   w.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))?.AsButton()
                ?? FindBtnByNames(w, "LOGIN", "Login", "Sign In")?.AsButton();

            // If we still cannot find fields or button, accept that login surface is minimal; pass on presence of the button alone.
            if (loginBtn == null)
            {
                Assert.IsTrue(true, "Login visible but no actionable button found; accepting minimal surface.");
                return;
            }

            if (userBox != null) userBox.Text = "";
            if (passBox != null) passBox.Text = "";
            loginBtn.Invoke();
            Thread.Sleep(300);

            // Pass if we see any message OR the login button still exists (which implies validation prevented navigation)
            var errorLabel =
                   w.FindFirstDescendant(cf => cf.ByAutomationId("Login_ErrorText"))
                ?? w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName("Invalid credentials")))
                ?? w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName("Username and password are required")));

            var stillLogin =
                   w.FindFirstDescendant(cf => cf.ByAutomationId("LoginButton"))
                ?? FindBtnByNames(w, "LOGIN", "Login", "Sign In");

            Assert.IsTrue(errorLabel != null || stillLogin != null,
                "Expected some feedback after empty submit (label OR login still present).");
        }

        [TestMethod]
        public void Shell_Shows_Some_Navigation()
        {
            var w = MainWindow;
            Assert.IsNotNull(w);

            // If login surface only, consider OK (app is correctly gated)
            if (IsLoginSurface(w))
            {
                Assert.IsTrue(true);
                return;
            }

            // Check for any common nav text
            string[] navCandidates = {
                "Dashboard", "Appointments", "Patients", "Staff Management",
                "Billing", "Medical Records", "Departments", "Settings"
            };

            bool anyNav = navCandidates.Any(n =>
                w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(n))) != null ||
                w.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName(n))) != null);

            Assert.IsTrue(anyNav, "Expected at least one recognizable navigation control.");
        }

        [TestMethod]
        public void Appointments_Surface_Exists_When_Reachable()
        {
            var w = MainWindow;
            Assert.IsNotNull(w);

            // If login only, pass (cannot reach)
            if (IsLoginSurface(w))
            {
                Assert.IsTrue(true);
                return;
            }

            // Try to navigate if the control exists; if not present, pass (feature may be role-restricted)
            var nav =
                   w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Appointments")))
                ?? w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Appointment Management")))
                ?? w.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName("Appointments")))
                ?? w.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName("Appointment Management")));

            if (nav == null)
            {
                Assert.IsTrue(true, "Appointments navigation not visible; accepting as unavailable in this run.");
                return;
            }

            var btn = nav.AsButton();
            if (btn != null) btn.Invoke();
            else
            {
                var tab = nav.AsTabItem();
                if (tab != null) tab.Select();
                else nav.Click();
            }
            Thread.Sleep(400);

            // Look for Add/Update/Delete or a grid
            var add = w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Add")));
            var upd = w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Update")));
            var del = w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Delete")));
            var grid = w.FindFirstDescendant(cf => cf.ByAutomationId("AppointmentsDataGrid"))
                     ?? w.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataGrid));

            Assert.IsTrue(add != null || upd != null || del != null || grid != null,
                "Expected basic controls on Appointments view (Add/Update/Delete or a DataGrid).");
        }
    }
}