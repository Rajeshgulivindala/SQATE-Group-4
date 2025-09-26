using System.Windows;
using System.Threading.Tasks;
using HospitalManagementSystem.Services.Data;
using HospitalManagementSystem.Utilities; // This line is required

namespace HospitalManagementSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Call the base class's OnStartup method.
            base.OnStartup(e);

            // Temporarily add this line to create a test user.
            // You can change "admin" and "adminpassword" to whatever you like.
            await UserCreator.CreateNewUserAsync("admin", "adminpassword","Admin",true);

            // After the database and user are created, you can open the login window.
            var loginWindow = new Views.Windows.LoginWindow();
            loginWindow.Show();
        }
    }
}
