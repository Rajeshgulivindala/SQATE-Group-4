using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using HospitalManagementSystem.Services.Data;
using HospitalManagementSystem.Services.Navigation;
namespace HospitalManagementSystem
{
    public partial class MainWindow : Window
    {
        private readonly HospitalManagementSystem.Services.Navigation.HMSNavigationService _navigationService;

        public MainWindow()
        {
            InitializeComponent();
            _navigationService = new HospitalManagementSystem.Services.Navigation.HMSNavigationService(MainContentArea);
            TestDatabaseConnection();
        }

        private void TestDatabaseConnection()
        {
            try
            {
                using (var context = new HMSDbContext())
                {
                    var userCount = context.Users.Count();
                    var deptCount = context.Departments.Count();

                    StatusText.Text = $"Database: Connected | Users: {userCount} | Departments: {deptCount}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Database: Error - {ex.Message}";
            }
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var module = button?.Tag?.ToString();
            if (!string.IsNullOrEmpty(module))
            {
                _navigationService.NavigateTo(module);
            }
        }
    }
}