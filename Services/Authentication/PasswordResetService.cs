using System;
using System.Linq;
using System.Threading.Tasks;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Services.Authentication
{
    public interface IPasswordResetService
    {
        Task<bool> ResetPasswordAsync(string username, string newPassword, int adminUserId);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        string GenerateTemporaryPassword();
    }

    public class PasswordResetService : IPasswordResetService
    {
        public async Task<bool> ResetPasswordAsync(string username, string newPassword, int adminUserId)
        {
            try
            {
                using (var context = new HMSDbContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.Username == username);
                    if (user == null) return false;

                    // Hash new password
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    user.Salt = GenerateSalt();

                    // Log password reset
                    var auditLog = new AuditLog
                    {
                        UserID = adminUserId,
                        EventType = "PASSWORD_RESET",
                        Description = $"Password reset for user: {username}",
                        Timestamp = DateTime.Now
                    };
                    context.AuditLogs.Add(auditLog);

                    await context.SaveChangesAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                using (var context = new HMSDbContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.UserID == userId);
                    if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                        return false;

                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    user.Salt = GenerateSalt();

                    await context.SaveChangesAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz023456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateSalt()
        {
            return BCrypt.Net.BCrypt.GenerateSalt();
        }
    }
}