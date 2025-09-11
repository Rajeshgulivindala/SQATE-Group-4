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
namespace HospitalManagementSystem
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
                    var roomCount = context.Rooms.Count();

                    MessageBox.Show($"Database Connected Successfully!\n\n" +
                                  $"Users: {userCount}\n" +
                                  $"Departments: {deptCount}\n" +
                                  $"Rooms: {roomCount}\n\n" +
                                  $"Database: {context.Database.Connection.Database}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection failed:\n{ex.Message}");
            }
        }
    }
}