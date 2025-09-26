using HospitalManagementSystem.Models;
using HospitalManagementSystem.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.Windows
{
    /// <summary>
    /// Interaction logic for UserOnboardingWindow.xaml
    /// </summary>
    public partial class UserOnboardingWindow : Window
    {
        public UserOnboardingWindow()
        {
            InitializeComponent();
            PopulateRolesComboBox();
        }

        private void PopulateRolesComboBox()
        {
            // Populate the ComboBox with user roles. This list can be managed centrally later.
            RoleComboBox.Items.Add("Admin");
            RoleComboBox.Items.Add("Doctor");
            RoleComboBox.Items.Add("Nurse");
            RoleComboBox.Items.Add("Clerk");
            RoleComboBox.SelectedIndex = 0; // Default to the first role
        }

        private async void OnboardButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the button to prevent multiple clicks during the process
            OnboardButton.IsEnabled = false;
            StatusMessageText.Text = "Onboarding user...";
            StatusMessageText.Foreground = System.Windows.Media.Brushes.Orange;

            string newUsername = UsernameTextBox.Text.Trim();
            string newPassword = PasswordBox.Password;
            string selectedRole = RoleComboBox.SelectedItem?.ToString();

            // Simple validation to ensure fields are not empty
            if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(selectedRole))
            {
                StatusMessageText.Text = "Please fill in all fields.";
                StatusMessageText.Foreground = System.Windows.Media.Brushes.Red;
                OnboardButton.IsEnabled = true;
                return;
            }

            try
            {
                // Call the utility method to create the user with the selected role
                // The UserCreator handles the password hashing and database saving securely
                await UserCreator.CreateNewUserAsync(newUsername, newPassword, selectedRole, true);

                // Provide success feedback to the user
                StatusMessageText.Text = $"User '{newUsername}' onboarded as '{selectedRole}' successfully!";
                StatusMessageText.Foreground = System.Windows.Media.Brushes.Green;

                // Clear the input fields for the next user
                UsernameTextBox.Text = string.Empty;
                PasswordBox.Password = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessageText.Text = "An error occurred while creating the user.";
                StatusMessageText.Foreground = System.Windows.Media.Brushes.Red;
                // Log the full exception details for debugging purposes
                Console.WriteLine($"Error creating user: {ex.Message}");
            }
            finally
            {
                // Re-enable the button regardless of success or failure
                OnboardButton.IsEnabled = true;
            }
        }
    }
}
