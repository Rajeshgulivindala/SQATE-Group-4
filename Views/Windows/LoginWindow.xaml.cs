using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Services.Authentication;
using HospitalManagementSystem.Services.Data;
using HospitalManagementSystem.Models;

// NOTE: requires BCrypt.Net-Next package
namespace HospitalManagementSystem.Views.Windows
{
    public partial class LoginWindow : Window
    {
        private int _failedLoginAttempts = 0;
        private DateTime? _lastFailedLogin;
        private bool _suppressPasswordSync = false;

        public LoginWindow()
        {
            InitializeComponent();
        }

        // ===== Login =====
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = (UsernameTextBox.Text ?? string.Empty).Trim();
            string password = (ShowPasswordCheckBox.IsChecked == true)
                                ? (PasswordRevealTextBox.Text ?? string.Empty)
                                : (PasswordBox.Password ?? string.Empty);

            LoadingProgressBar.Visibility = Visibility.Visible;
            LoginButton.IsEnabled = false;
            LoginButton.Content = "Signing in…";
            ErrorMessageText.Text = string.Empty;

            // Required fields
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessageText.Text = "Please enter a username and password.";
                ResetLoginUi();
                return;
            }

            // ONLY rule: min length 6
            if (password.Length < 6)
            {
                ErrorMessageText.Text = "Password must be at least 6 characters.";
                ResetLoginUi();
                return;
            }

            // Simple lockout
            if (_failedLoginAttempts >= 5 && _lastFailedLogin.HasValue &&
                (DateTime.Now - _lastFailedLogin.Value).TotalMinutes < 15)
            {
                ErrorMessageText.Text = "Account locked due to too many failed attempts. Try again in 15 minutes.";
                ResetLoginUi();
                return;
            }

            try
            {
                using (var context = new HMSDbContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

                    bool ok = false;
                    if (user != null)
                    {
                        // verify with BCrypt
                        ok = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    }

                    if (ok)
                    {
                        AuthenticationService.CurrentUser = user;
                        _failedLoginAttempts = 0;

                        // success popup
                        MessageBox.Show("Login successful.", "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);

                        // open main window
                        var target = new MainWindow();
                        target.Show();
                        Close();

                        // update last login (background)
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                using (var bg = new HMSDbContext())
                                {
                                    var u = bg.Users.Find(user.UserID);
                                    if (u != null)
                                    {
                                        u.LastLogin = DateTime.Now;
                                        bg.SaveChanges();
                                    }
                                }
                            }
                            catch { /* ignore background failures */ }
                        });

                        return;
                    }

                    // failed
                    _failedLoginAttempts++;
                    _lastFailedLogin = DateTime.Now;
                    ErrorMessageText.Text = "Invalid username or password.";
                    ResetLoginUi();
                }
            }
            catch
            {
                ErrorMessageText.Text = "Login failed. Please check your connection and try again.";
                ResetLoginUi();
            }
        }

        private void ResetLoginUi()
        {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LoginButton.IsEnabled = true;
            LoginButton.Content = "LOGIN";
        }

        // ===== Show / Hide password wiring =====
        private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _suppressPasswordSync = true;
            PasswordRevealTextBox.Text = PasswordBox.Password;
            _suppressPasswordSync = false;

            PasswordRevealTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordRevealTextBox.Focus();
            PasswordRevealTextBox.CaretIndex = PasswordRevealTextBox.Text.Length;
        }

        private void ShowPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _suppressPasswordSync = true;
            PasswordBox.Password = PasswordRevealTextBox.Text;
            _suppressPasswordSync = false;

            PasswordRevealTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Focus();
            PasswordBox.SelectAll();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressPasswordSync || ShowPasswordCheckBox.IsChecked != true) return;
            _suppressPasswordSync = true;
            PasswordRevealTextBox.Text = PasswordBox.Password;
            _suppressPasswordSync = false;
        }

        private void PasswordRevealTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressPasswordSync || ShowPasswordCheckBox.IsChecked != true) return;
            _suppressPasswordSync = true;
            PasswordBox.Password = PasswordRevealTextBox.Text;
            _suppressPasswordSync = false;
        }
    }
}
