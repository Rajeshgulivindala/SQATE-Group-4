using System;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class RegisterUserView : UserControl
    {
        private readonly UserService _userService;

        public RegisterUserView()
        {
            InitializeComponent();
            var dbContext = new HMSDbContext();
            _userService = new UserService(dbContext);
        }

        private void RegisterUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameTextBox.Text.Trim();
                string password = PasswordBox.Password.Trim();
                string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
                {
                    MessageBox.Show("Please fill out all fields.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (password.Length < 6)
                {
                    MessageBox.Show("Password must be at least 6 characters long.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newUser = new User
                {
                    Username = username,
                    PasswordHash = password,
                    Role = role
                };

                if (_userService.RegisterUser(newUser))
                {
                    MessageBox.Show(
                        $"User '{username}' with role '{role}' registered successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    UsernameTextBox.Clear();
                    PasswordBox.Clear();
                    RoleComboBox.SelectedIndex = -1;
                }
                else
                {
                    MessageBox.Show(
                        "Registration failed. Please try again.",
                        "Registration Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}