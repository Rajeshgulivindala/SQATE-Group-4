using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class DeactivateUsersView : UserControl
    {
        // Use the real User model from your Models folder.
        public ObservableCollection<User> Users { get; set; }
        private readonly UserService _userService;

        public DeactivateUsersView()
        {
            InitializeComponent();

            // Initialize the UserService with a new database context.
            // This assumes HMSDbContext is accessible here.
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

        private void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersListBox.SelectedItem is User selectedUser)
            {
                // Assuming IsActive is a property on your real User model
                if (selectedUser.IsActive == false)
                {
                    MessageBox.Show("User is already deactivated.", "Status Unchanged", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    // Call the real service to deactivate the user in the database.
                    if (_userService.UpdateUserStatus(selectedUser.UserID, false))
                    {
                        // Update the local UI list only if the database update was successful.
                        selectedUser.IsActive = false;
                        MessageBox.Show($"User '{selectedUser.Username}  has been deactivated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUsers(); // Refresh the list
                    }
                    else
                    {
                        MessageBox.Show("An error occurred while deactivating the user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while deactivating the user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a user to deactivate.", "No User Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersListBox.SelectedItem is User selectedUser)
            {
                if (selectedUser.IsActive)
                {
                    MessageBox.Show("User is already active.", "Status Unchanged", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    if (_userService.UpdateUserStatus(selectedUser.UserID, true))
                    {
                        selectedUser.IsActive = true;
                        MessageBox.Show($"User '{selectedUser.Username}' has been activated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadUsers(); // Refresh the list
                    }
                    else
                    {
                        MessageBox.Show("An error occurred while activating the user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while activating the user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a user to activate.", "No User Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}