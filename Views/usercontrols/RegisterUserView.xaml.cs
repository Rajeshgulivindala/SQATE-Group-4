using System;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Interaction logic for RegisterUserView.xaml
    /// </summary>
    public partial class RegisterUserView : UserControl
    {
        private readonly UserService _userService;

        public RegisterUserView()
        {
            InitializeComponent();

            // NOTE: In production use Dependency Injection for HMSDbContext/UserService.
            var dbContext = new HMSDbContext();
            _userService = new UserService(dbContext);
        }

        private void RegisterUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = (UsernameTextBox.Text ?? string.Empty).Trim();
                string password = (PasswordBox.Password ?? string.Empty).Trim();

                string role = null;
                var selectedItem = RoleComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Content != null)
                {
                    role = selectedItem.Content.ToString().Trim();
                }

                // --- Validation ---
                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(role))
                {
                    MessageBox.Show("Please fill out Username, Password, and Role.",
                        "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (password.Length < 6)
                {
                    MessageBox.Show("Password must be at least 6 characters long.",
                        "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // --- Hash the password with BCrypt ---
                // (Requires the 'BCrypt.Net-Next' NuGet package)
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                // --- Create the user entity (uses your Models.User) ---
                var newUser = new User
                {
                    Username = username,
                    PasswordHash = hashedPassword, // store HASH, not raw password
                    Role = role,
                    IsActive = true
                };

                // Keep your existing UI flow/signature:
                if (_userService.RegisterUser(newUser))
                {
                    MessageBox.Show(
                        $"User '{username}' with role '{role}' registered successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    UsernameTextBox.Clear();
                    PasswordBox.Clear();
                    RoleComboBox.SelectedIndex = -1;
                }
                else
                {
                    // If your UserService exposes LastError, show it; otherwise keep the generic message.
                    string message = "Registration failed. Please try again (perhaps the username is taken).";
                    var lastErrorProp = _userService.GetType().GetProperty("LastError");
                    if (lastErrorProp != null)
                    {
                        var lastError = lastErrorProp.GetValue(_userService, null) as string;
                        if (!string.IsNullOrWhiteSpace(lastError)) message = "Registration failed: " + lastError;
                    }

                    MessageBox.Show(message, "Registration Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
