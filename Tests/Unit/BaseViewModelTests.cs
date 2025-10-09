using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.ViewModels;
using System.ComponentModel;

namespace HospitalManagementSystem.Tests
{
    [TestClass]
    public class BaseViewModelTests
    {
        private class DummyVm : BaseViewModel
        {
            private int _age;
            public int Age
            {
                get => _age;
                set => SetProperty(ref _age, value);
            }
        }

        [TestMethod]
        public void Implements_INotifyPropertyChanged()
        {
            var vm = new DummyVm();
            Assert.IsTrue(vm is INotifyPropertyChanged);
        }

        [TestMethod]
        public void SetProperty_RaisesPropertyChanged()
        {
            var vm = new DummyVm();
            string last = null;
            vm.PropertyChanged += (s, e) => last = e.PropertyName;

            vm.Age = 42;

            Assert.AreEqual("Age", last, "Expected PropertyChanged for Age");
        }
    }
}
