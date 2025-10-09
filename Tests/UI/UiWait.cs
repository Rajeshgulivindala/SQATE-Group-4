using FlaUI.Core.AutomationElements;
using HospitalManagementSystem.Tests.UI;
using System.Threading;
using System;


namespace HospitalManagementSystem.Tests.UI
{
    public static class UiWait
    {
        public static AutomationElement WaitId(AutomationElement root, string automationId, int ms = 5000)
        {
            var end = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < end)
            {
                var el = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                if (el != null) return el;
                Thread.Sleep(100);
            }
            return null;
        }
    }
}