using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using HospitalManagementSystem.Services.Authentication;
using HospitalManagementSystem.Services.Data;   // HMSDbContext

// If you're on EF6 (most likely for .NET Framework WPF):
using System.Data.Entity;
// If you're on EF Core instead, comment the line above and uncomment this:
// using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Views.Windows
{
    // Requires NuGet: BCrypt.Net-Next
    public partial class LoginWindow : Window
    {
        private bool _suppressPasswordSync = false;

        public LoginWindow()
        {
            InitializeComponent();
        }

        // =============================
        // Login Button
        // =============================
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

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessageText.Text = "Please enter a username and password.";
                ResetLoginUi();
                return;
            }

            try
            {
                using (var context = new HMSDbContext())
                {
                    // Async DB query
                    var user = await context.Users
                        .Where(u => u.Username == username && u.IsActive)
                        .FirstOrDefaultAsync();

                    bool ok = false;

                    if (user != null && !string.IsNullOrWhiteSpace(user.PasswordHash))
                    {
                        string stored = user.PasswordHash.Trim();

                        // Does it look like a BCrypt hash already?
                        bool looksLikeBCrypt =
                            stored.StartsWith("$2a$") ||
                            stored.StartsWith("$2b$") ||
                            stored.StartsWith("$2y$");

                        try
                        {
                            if (looksLikeBCrypt)
                            {
                                // Normal path
                                ok = BCrypt.Net.BCrypt.Verify(password, stored);
                            }
                            else
                            {
                                // LEGACY PATH: treat stored value as plain text.
                                // If it matches, silently upgrade to BCrypt for future logins.
                                if (string.Equals(stored, password, StringComparison.Ordinal))
                                {
                                    ok = true;
                                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                                    await context.SaveChangesAsync();
                                }
                                else
                                {
                                    ok = false;
                                }
                            }
                        }
                        catch (BCrypt.Net.SaltParseException)
                        {
                            // Stored value is not a valid BCrypt hash (legacy/other).
                            if (string.Equals(stored, password, StringComparison.Ordinal))
                            {
                                ok = true;
                                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                                await context.SaveChangesAsync();
                            }
                            else
                            {
                                ok = false;
                            }
                        }
                    }

                    if (ok)
                    {
                        AuthenticationService.CurrentUser = user;

                        // Optional toast
                        MessageBox.Show("Login successful.", "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);

                        // Fire-and-forget: update last login
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using (var bg = new HMSDbContext())
                                {
                                    var u = await bg.Users.FindAsync(user.UserID);
                                    if (u != null)
                                    {
                                        u.LastLogin = DateTime.Now;
                                        await bg.SaveChangesAsync();
                                    }
                                }
                            }
                            catch { /* swallow background errors */ }
                        });

                        var main = new MainWindow();
                        main.Show();
                        Close();
                        return;
                    }

                    ErrorMessageText.Text = "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessageText.Text = "Login failed. Please try again.";
#if DEBUG
                MessageBox.Show(ex.ToString(), "Login Error");
#endif
            }

            ResetLoginUi();
        }

        private void ResetLoginUi()
        {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LoginButton.IsEnabled = true;
            LoginButton.Content = "LOGIN";
        }

        // =============================
        // Show / Hide Password Logic
        // =============================
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
