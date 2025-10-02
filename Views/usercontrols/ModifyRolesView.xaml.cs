using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Interaction logic for ModifyRolesView.xaml
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

                var dbContext = new HMSDbContext();
                _userService = new UserService(dbContext);

                LoadUsers();

                UserListBox.SelectionChanged += UserListBox_SelectionChanged;
            }
            catch (Exception ex)
            {
                ShowStatus($"Initialization failed: {ex.Message}", isError: true);
            }
        }

        // -------------------- UI helpers --------------------

        private void ShowStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message ?? string.Empty;
            StatusTextBlock.Foreground = isError ? Brushes.Red : Brushes.Green;
        }

        private void MarkInvalid(Control control, string tooltip)
        {
            if (control == null) return;
            control.BorderBrush = Brushes.Red;
            control.BorderThickness = new Thickness(1.5);
            control.ToolTip = tooltip;
        }

        private void ClearInvalid(Control control)
        {
            if (control == null) return;
            control.ClearValue(Border.BorderBrushProperty);
            control.ClearValue(Border.BorderThicknessProperty);
            control.ToolTip = null;
        }

        private string GetSelectedRoleFromCombo()
        {
            var item = NewRoleComboBox.SelectedItem as ComboBoxItem;
            return item != null ? item.Content?.ToString()?.Trim() : null;
        }

        private List<string> GetRolesFromCombo()
        {
            return NewRoleComboBox.Items
                .OfType<ComboBoxItem>()
                .Select(i => i.Content != null ? i.Content.ToString().Trim() : null)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private bool RoleExistsInCombo(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            var roles = GetRolesFromCombo();
            return roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        private int CountAdmins()
        {
            var users = _userService.GetAllUsers() ?? Enumerable.Empty<User>();
            return users.Count(u => string.Equals(u.Role, "Admin", StringComparison.OrdinalIgnoreCase));
        }

        private void ReselectUser(int userId)
        {
            var users = UserListBox.ItemsSource as IEnumerable<User>;
            var match = users != null ? users.FirstOrDefault(u => u.UserID == userId) : null;
            if (match != null) UserListBox.SelectedItem = match;
        }

        private void UpdateRoleLabelsFromSelection()
        {
            if (UserListBox.SelectedItem is User u)
            {
                UsernameTextBlock.Text = u.Username ?? string.Empty;
                CurrentRoleTextBlock.Text = u.Role ?? string.Empty;

                var match = NewRoleComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(i => string.Equals(i.Content != null ? i.Content.ToString() : null, u.Role, StringComparison.OrdinalIgnoreCase));
                NewRoleComboBox.SelectedItem = match;
            }
        }

        // -------------------- Data loading --------------------

        private void LoadUsers()
        {
            try
            {
                var users = _userService.GetAllUsers();
                UserListBox.ItemsSource = users;

                if (users == null || !users.Any())
                {
                    ShowStatus("No users found.", isError: true);
                }
                else
                {
                    ShowStatus(string.Empty, isError: false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error loading users: {ex.Message}", isError: true);
            }
        }

        // -------------------- Events --------------------

        private void UserListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearInvalid(NewRoleComboBox);
            ClearInvalid(UserListBox);

            if (UserListBox.SelectedItem is User user)
            {
                _selectedUser = user;
                UsernameTextBlock.Text = _selectedUser.Username ?? string.Empty;
                CurrentRoleTextBlock.Text = _selectedUser.Role ?? string.Empty;

                // Select current role in ComboBox (case-insensitive)
                var match = NewRoleComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => string.Equals(i.Content != null ? i.Content.ToString() : null, _selectedUser.Role, StringComparison.OrdinalIgnoreCase));
                NewRoleComboBox.SelectedItem = match;

                ShowStatus(string.Empty, isError: false);
            }
            else
            {
                _selectedUser = null;
                UsernameTextBlock.Text = string.Empty;
                // Only clear FullNameTextBlock if it exists in XAML
                try { FullNameTextBlock.Text = string.Empty; } catch { /* ignore if not present */ }
                CurrentRoleTextBlock.Text = string.Empty;
                NewRoleComboBox.SelectedIndex = -1;
            }
        }

        private void UpdateRoleButton_Click(object sender, RoutedEventArgs e)
        {
            ClearInvalid(NewRoleComboBox);
            ClearInvalid(UserListBox);

            // Validate selection
            if (_selectedUser == null)
            {
                MarkInvalid(UserListBox, "Select a user to modify.");
                ShowStatus("Please select a user to modify.", isError: true);
                return;
            }

            // Validate role selection
            var selectedRole = GetSelectedRoleFromCombo();
            if (string.IsNullOrWhiteSpace(selectedRole))
            {
                MarkInvalid(NewRoleComboBox, "Select a new role.");
                ShowStatus("Please select a new role.", isError: true);
                return;
            }

            // Whitelist: role must exist in combo
            if (!RoleExistsInCombo(selectedRole))
            {
                MarkInvalid(NewRoleComboBox, "Invalid role.");
                ShowStatus("Selected role is not valid.", isError: true);
                return;
            }

            // No-op change
            if (string.Equals(_selectedUser.Role, selectedRole, StringComparison.OrdinalIgnoreCase))
            {
                ShowStatus("The selected role is the same as the current role. No changes made.", isError: true);
                return;
            }

            // Prevent demoting the last Admin
            bool selectedUserIsAdmin = string.Equals(_selectedUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool demotingAdmin = selectedUserIsAdmin && !string.Equals(selectedRole, "Admin", StringComparison.OrdinalIgnoreCase);
            if (demotingAdmin && CountAdmins() <= 1)
            {
                MarkInvalid(NewRoleComboBox, "Cannot demote the last Admin.");
                ShowStatus("Operation blocked: You cannot demote the last Admin.", isError: true);
                return;
            }

            try
            {
                var ok = _userService.UpdateUserRole(_selectedUser.UserID, selectedRole);
                if (!ok)
                {
                    ShowStatus("Failed to update role. Please try again.", isError: true);
                    return;
                }

                // Success popup asking to refresh
                var result = MessageBox.Show(
                    $"Successfully updated '{_selectedUser.Username}' to role: {selectedRole}.\n\nRefresh the roles list now?",
                    "Role Updated",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    LoadUsers();
                    ReselectUser(_selectedUser.UserID);   // keeps selection on the same user
                    UpdateRoleLabelsFromSelection();      // sync labels + combobox
                    ShowStatus("Roles list refreshed.", isError: false);
                }
                else
                {
                    // Update the currently selected user's role locally so labels reflect change
                    _selectedUser.Role = selectedRole;
                    UpdateRoleLabelsFromSelection();
                    ShowStatus($"Role updated to: {selectedRole}. (Refresh skipped)", isError: false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error updating role: {ex.Message}", isError: true);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            ClearInvalid(UserListBox);

            if (_selectedUser == null)
            {
                MarkInvalid(UserListBox, "Select a user to delete.");
                ShowStatus("Please select a user to delete.", isError: true);
                return;
            }

            // Prevent deleting the last Admin
            bool isAdmin = string.Equals(_selectedUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            if (isAdmin && CountAdmins() <= 1)
            {
                ShowStatus("Operation blocked: You cannot delete the last Admin.", isError: true);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete user '{_selectedUser.Username}'?\nThis action cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var ok = _userService.DeleteUser(_selectedUser.UserID);
                if (ok)
                {
                    ShowStatus($"Successfully deleted user '{_selectedUser.Username}'.", isError: false);
                    LoadUsers();

                    // Clear selection and UI
                    UserListBox.SelectedIndex = -1;
                    _selectedUser = null;
                    UsernameTextBlock.Text = string.Empty;
                    // Only clear FullNameTextBlock if it exists in XAML
                    try { FullNameTextBlock.Text = string.Empty; } catch { /* ignore if not present */ }
                    CurrentRoleTextBlock.Text = string.Empty;
                    NewRoleComboBox.SelectedIndex = -1;
                }
                else
                {
                    ShowStatus("Failed to delete user. Please try again.", isError: true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error deleting user: {ex.Message}", isError: true);
            }
        }
    }
}
