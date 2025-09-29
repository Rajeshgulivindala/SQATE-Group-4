using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HospitalManagementSystem.Services.Authentication;
using HospitalManagementSystem.Services.Data;
using HospitalManagementSystem.Models;
using BCrypt.Net;

namespace HospitalManagementSystem.Views.Windows
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        // Fields to track failed login attempts for a simple lockout mechanism.
        private int _failedLoginAttempts = 0;
        private DateTime? _lastFailedLogin;

        public LoginWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // This will catch any errors that prevent the UI from loading.
                MessageBox.Show($"Initialization failed: {ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Re-throw the exception to ensure it's logged.
            }
        }

        /// <summary>
        /// Handles the Click event for the Login button.
        /// </summary>
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Debugging message to confirm the method has been entered
            Console.WriteLine("LoginButton_Click method entered.");

            // Get the user input from the text boxes.
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Immediately update the UI to provide user feedback.
            LoadingProgressBar.Visibility = Visibility.Visible;
            LoginButton.IsEnabled = false;
            LoginButton.Content = "Signing in...";
            ErrorMessageText.Text = "";

            // --- Basic Client-Side Validation ---
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessageText.Text = "Please enter a username and password.";
                ResetLoginButton();
                Console.WriteLine("Validation failed: Username or password was empty.");
                return;
            }

            // --- Simple Account Lockout Check ---
            if (_failedLoginAttempts >= 5 && _lastFailedLogin.HasValue &&
                (DateTime.Now - _lastFailedLogin.Value).TotalMinutes < 15)
            {
                ErrorMessageText.Text = "Account locked due to too many failed attempts. Please try again in 15 minutes.";
                ResetLoginButton();
                Console.WriteLine("Login attempt blocked: Account is locked.");
                return;
            }

            // --- Server-Side Authentication ---
            try
            {
                // Use a 'using' statement to ensure the database context is properly disposed.
                using (var context = new HMSDbContext())
                {
                    // Find the user by username and ensure they are active.
                    var user = context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

                    // Verify the password using the BCrypt hashing library.
                    if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    {
                        Console.WriteLine("Login successful.");
                        // --- Successful Login ---
                        AuthenticationService.CurrentUser = user;
                        _failedLoginAttempts = 0;

                        // ------------------------------------------------------------------
                        // UPDATED: ROLE-BASED REDIRECTION LOGIC
                        // Doctor role now redirects to MainWindow.
                        // ------------------------------------------------------------------
                        Window targetWindow = null;

                        // Check the role of the authenticated user
                        switch (user.Role)
                        {
                            case "Admin":
                                targetWindow = new MainWindow();
                                break;
                            case "Doctor":
                                // Admin and Doctor roles use the full administrative MainWindow
                                targetWindow = new MainWindow();
                                break;
                            case "Nurse":
                                // Nurse role opens the dedicated Nurse dashboard
                                targetWindow = new MainWindow();
                                break;
                            case "Clerk":
                                // Nurse role opens the dedicated Nurse dashboard
                                targetWindow = new MainWindow();
                                break;
                            default:
                                // Handle unknown roles gracefully
                                ErrorMessageText.Text = $"Unknown role assigned: {user.Role}. Access denied.";
                                ResetLoginButton();
                                return;
                        }

                        // Open the target window and close the login window
                        if (targetWindow != null)
                        {
                            targetWindow.Show();
                            this.Close();
                        }
                        // ------------------------------------------------------------------

                        // Update the last login time in the background to not block the UI.
                        await Task.Run(() =>
                        {
                            try
                            {
                                using (var bgContext = new HMSDbContext())
                                {
                                    // It's safer to fetch the user again in the new context
                                    var bgUser = bgContext.Users.Find(user.UserID);
                                    if (bgUser != null)
                                    {
                                        bgUser.LastLogin = DateTime.Now;
                                        bgContext.SaveChanges();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Background task failed to update last login: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        // --- Failed Login ---
                        Console.WriteLine("Login failed: Invalid username or password.");
                        _failedLoginAttempts++;
                        _lastFailedLogin = DateTime.Now;
                        ErrorMessageText.Text = "Invalid username or password.";
                        ResetLoginButton();
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch any exceptions that occur during the login process (e.g., database connection issues).
                ErrorMessageText.Text = "Login failed. Please check your connection and try again.";
                Console.WriteLine($"Login Exception: {ex.Message}");
                ResetLoginButton();
            }
        }

        /// <summary>
        /// Resets the UI state of the login button and progress bar.
        /// </summary>
        private void ResetLoginButton()
        {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LoginButton.IsEnabled = true;
            LoginButton.Content = "LOGIN";
        }
    }
}
