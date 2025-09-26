using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Utilities
{
    /// <summary>
    /// Utility class for creating and managing user records.
    /// </summary>
    public static class UserCreator
    {
        /// <summary>
        /// Creates a new user with a hashed password and saves it to the database.
        /// </summary>
        /// <param name="username">The username for the new user.</param>
        /// <param name="password">The plaintext password for the new user.</param>
        /// <param name="role">The role of the new user (e.g., "Admin", "Doctor", "Nurse").</param>
        /// <param name="isActive">A flag indicating whether the user account is active.</param>
        public static async Task CreateNewUserAsync(string username, string password, string role, bool isActive)
        {
            try
            {
                using (var context = new HMSDbContext())
                {
                    // Check if a user with the same username already exists.
                    if (context.Users.Any(u => u.Username == username))
                    {
                        Console.WriteLine($"User with username '{username}' already exists. Skipping creation.");
                        return;
                    }

                    // --- CRITICAL STEP: Hash the password before saving ---
                    // BCrypt.HashPassword creates a secure, hashed string that is safe to store in your database.
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                    // Create the new User model.
                    var newUser = new User
                    {
                        Username = username,
                        PasswordHash = hashedPassword,
                        IsActive = isActive,
                        Role = role
                    };

                    // Add the new user to the database context.
                    context.Users.Add(newUser);

                    // Save the changes to the database.
                    await context.SaveChangesAsync();

                    Console.WriteLine($"Successfully created new user: {username}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the user: {ex.Message}");
            }
        }
    }
}
