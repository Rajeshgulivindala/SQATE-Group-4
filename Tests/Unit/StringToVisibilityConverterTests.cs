using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Helpers;
using System.Windows;

namespace HospitalManagementSystem.Tests
{
    [TestClass]
    public class StringToVisibilityConverterTests
    {
        [TestMethod]
        public void Convert_NullOrEmpty_ReturnsCollapsed()
        {
            var c = new StringToVisibilityConverter();
            Assert.AreEqual(Visibility.Collapsed, c.Convert(null, typeof(Visibility), null, null));
            Assert.AreEqual(Visibility.Collapsed, c.Convert("", typeof(Visibility), null, null));
        }

        [TestMethod]
        public void Convert_NonEmpty_ReturnsVisible()
        {
            var c = new StringToVisibilityConverter();
            Assert.AreEqual(Visibility.Visible, c.Convert("hi", typeof(Visibility), null, null));
        }
    }
}
