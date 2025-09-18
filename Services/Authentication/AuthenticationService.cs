using System;
using System.Linq;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Services.Authentication
{
    public class AuthenticationService
    {
        public static User CurrentUser { get; set; } // Add this line

        public static bool Login(string username, string password)
        {
            try
            {
                using (var context = new HMSDbContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

                    if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    {
                        CurrentUser = user; // Set current user

                        // Update last login
                        user.LastLogin = DateTime.Now;
                        context.SaveChanges();

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            }

            return false;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static bool IsAuthenticated()
        {
            return CurrentUser != null;
        }

        public static bool HasRole(string role)
        {
            return CurrentUser?.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}