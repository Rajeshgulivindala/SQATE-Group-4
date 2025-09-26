using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class PasswordResetView : UserControl
    {
        public ObservableCollection<User> Users { get; set; }
        private readonly UserService _userService;

        public PasswordResetView()
        {
            InitializeComponent();
            _userService = new UserService(new HMSDbContext());
            Users = new ObservableCollection<User>();

            this.DataContext = this;
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                var usersFromDb = _userService.GetAllUsers().ToList();
                Users.Clear();
                foreach (var user in usersFromDb)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersListBox.SelectedItem is User selectedUser)
            {
                string newPassword = NewPasswordBox.Password;
                string confirmPassword = ConfirmPasswordBox.Password;

                if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
                {
                    MessageBox.Show("Passwords do not match or are empty. Please try again.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Assuming you have an UpdateUserPassword method in your UserService
                    if (_userService.UpdateUserPassword(selectedUser.UserID, newPassword))
                    {
                        MessageBox.Show($"Password for user '{selectedUser.Username}' has been successfully reset.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        NewPasswordBox.Clear();
                        ConfirmPasswordBox.Clear();
                    }
                    else
                    {
                        MessageBox.Show("An error occurred while resetting the password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while resetting the password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a user from the list first.", "No User Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}