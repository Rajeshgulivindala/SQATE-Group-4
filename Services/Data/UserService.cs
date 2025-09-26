using System;
using System.Collections.ObjectModel;
using System.Linq;
using HospitalManagementSystem.Models;
using System.Data.Entity;

namespace HospitalManagementSystem.Services.Data
{
    public class UserService
    {
        private readonly HMSDbContext _context;

        public UserService(HMSDbContext context)
        {
            _context = context;
        }

        public ObservableCollection<User> GetAllUsers()
        {
            return new ObservableCollection<User>(_context.Users.ToList());
        }

        public bool UpdateUserNames(int userId, string newUsername)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    user.Username = newUsername;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error updating user name: {ex.Message}");
                return false;
            }
        }

        public bool UpdateUserRole(int userId, string newRole)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    user.Role = newRole;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error updating user role: {ex.Message}");
                return false;
            }
        }
        public bool UpdateUserStatus(int userId, bool IsActive)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    user.IsActive = IsActive;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error updating user role: {ex.Message}");
                return false;
            }
        }
        public bool DeleteUser(int userId)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error deleting user: {ex.Message}");
                return false;
            }
        }

        public bool RegisterUser(User user)
        {
            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error during registration: {ex.Message}");
                return false;
            }
        }
    
        public bool UpdateUserPassword(int userId, string newPassword)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    // IMPORTANT: In a real application, you must hash and salt the password
                    // before saving it to the database. Never store plain-text passwords.
                    // Example: user.PasswordHash = HashPassword(newPassword);
                    user.PasswordHash = newPassword; // Placeholder for now
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error updating user password: {ex.Message}");
                return false;
            }
        }
    }
}

