using System;
using System.Linq;
using System.Windows;
using HospitalManagementSystem.Services.Authentication;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Views.Windows
{
    public partial class LoginWindow : Window
    {
        private int _failedLoginAttempts = 0;
        private DateTime? _lastFailedLogin;

        public LoginWindow()
        {
            InitializeComponent();
            PasswordBox.Password = "admin123"; // Pre-fill for testing
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Immediate UI feedback
            LoadingProgressBar.Visibility = Visibility.Visible;
            LoginButton.IsEnabled = false;
            LoginButton.Content = "Signing in...";
            ErrorMessageText.Text = "";

            // Quick validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessageText.Text = "Please enter username and password";
                ResetLoginButton();
                return;
            }

            // Quick lockout check
            if (_failedLoginAttempts >= 5 && _lastFailedLogin.HasValue &&
                DateTime.Now.Subtract(_lastFailedLogin.Value).TotalMinutes < 15)
            {
                ErrorMessageText.Text = "Account locked. Try again later.";
                ResetLoginButton();
                return;
            }

            // Fast login check
            try
            {
                using (var context = new HMSDbContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

                    if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    {
                        // Set current user immediately
                        AuthenticationService.CurrentUser = user;
                        _failedLoginAttempts = 0;

                        // Open main window instantly
                        var mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();

                        // Update last login in background (don't wait)
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                using (var bgContext = new HMSDbContext())
                                {
                                    var bgUser = bgContext.Users.Find(user.UserID);
                                    if (bgUser != null)
                                    {
                                        bgUser.LastLogin = DateTime.Now;
                                        bgContext.SaveChanges();
                                    }
                                }
                            }
                            catch { }
                        });
                    }
                    else
                    {
                        _failedLoginAttempts++;
                        _lastFailedLogin = DateTime.Now;
                        ErrorMessageText.Text = "Invalid username or password";
                        ResetLoginButton();
                    }
                }
            }
            catch
            {
                ErrorMessageText.Text = "Login failed. Please try again.";
                ResetLoginButton();
            }
        }

        private void ResetLoginButton()
        {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LoginButton.IsEnabled = true;
            LoginButton.Content = "LOGIN";
        }
    }
}