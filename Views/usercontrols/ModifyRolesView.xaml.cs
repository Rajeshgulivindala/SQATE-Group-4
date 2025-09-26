using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Interaction logic for ModifyRolesView.xaml
    /// This file contains the UI logic for displaying and managing user roles.
    /// </summary>
    public partial class ModifyRolesView : UserControl
    {
        private readonly UserService _userService;
        private User _selectedUser;

        public ModifyRolesView()
        {
            try
            {
                InitializeComponent();
                // Instantiate the database context and user service.
                var dbContext = new HMSDbContext();
                // This ensures the in-memory database is created for the demo.
                
                _userService = new UserService(dbContext);

                LoadUsers();

                // Hook up the SelectionChanged event to update the UI when a user is selected.
                UserListBox.SelectionChanged += UserListBox_SelectionChanged;
            }
            catch (Exception ex)
            {
                // This will catch any errors that prevent the UI from loading.
                StatusTextBlock.Text = $"Initialization failed: {ex.Message}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        /// <summary>
        /// Populates the ListBox with all users from the database.
        /// </summary>
        private void LoadUsers()
        {
            try
            {
                var users = _userService.GetAllUsers();
                UserListBox.ItemsSource = users;
                StatusTextBlock.Text = "";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error loading users: {ex.Message}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        /// <summary>
        /// Handles the selection change in the user list box.
        /// </summary>
        private void UserListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserListBox.SelectedItem is User user)
            {
                _selectedUser = user;
                UsernameTextBlock.Text = _selectedUser.Username;
                
                CurrentRoleTextBlock.Text = _selectedUser.Role;
                // Set the ComboBox to the user's current role.
                NewRoleComboBox.SelectedItem = NewRoleComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == _selectedUser.Role);
                StatusTextBlock.Text = "";
            }
            else
            {
                // Clear the UI if no user is selected.
                _selectedUser = null;
                UsernameTextBlock.Text = "";
                FullNameTextBlock.Text = "";
                CurrentRoleTextBlock.Text = "";
                NewRoleComboBox.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Handles the Click event for the Update Role button.
        /// This method updates the selected user's role in the database.
        /// </summary>
        private void UpdateRoleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                StatusTextBlock.Text = "Please select a user to modify.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            var selectedRole = (NewRoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (string.IsNullOrEmpty(selectedRole))
            {
                StatusTextBlock.Text = "Please select a new role.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (_userService.UpdateUserRole(_selectedUser.UserID, selectedRole))
            {
                StatusTextBlock.Text = "$Successfully updated '{_selectedUser.Username}' to role: {selectedRole}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                // Re-load users to reflect the change in the UI.
                LoadUsers();
            }
            else
            {
                StatusTextBlock.Text = "Failed to update role. Please try again.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        /// <summary>
        /// Handles the Click event for the Delete User button.
        /// This method removes the selected user from the database.
        /// </summary>
        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                StatusTextBlock.Text = "Please select a user to delete.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (_userService.DeleteUser(_selectedUser.UserID))
            {
                StatusTextBlock.Text = $"Successfully deleted user '{_selectedUser.Username}'.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                // Re-load users to reflect the change in the UI.
                LoadUsers();
            }
            else
            {
                StatusTextBlock.Text = "Failed to delete user. Please try again.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
    }
}
