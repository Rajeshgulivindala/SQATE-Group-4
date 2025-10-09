using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text; // needed for StringBuilder used in GenerateTemporaryPassword
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

        // >>> ONLY THIS METHOD CHANGED (to satisfy the failing complexity test) <<<
        public string GenerateTemporaryPassword()
        {
            // Keep your non-ambiguous characters
            const string LOWER = "abcdefghijkmnpqrstuvwxyz";   // no l, o
            const string UPPER = "ABCDEFGHJKLMNPQRSTUVWXYZ";   // no I, O
            const string DIGITS = "023456789";                  // no 1
            string ALL = LOWER + UPPER + DIGITS;

            // keep your existing default length behavior
            int length = 8; // original method produced 8 chars
            if (length < 8) length = 8; // enforce minimum

            var rnd = new Random();

            // Ensure at least one of each required category
            var sb = new StringBuilder(length);
            sb.Append(UPPER[rnd.Next(UPPER.Length)]);
            sb.Append(LOWER[rnd.Next(LOWER.Length)]);
            sb.Append(DIGITS[rnd.Next(DIGITS.Length)]);

            // Fill the rest from the combined set
            while (sb.Length < length)
                sb.Append(ALL[rnd.Next(ALL.Length)]);

            // Shuffle so positions aren’t predictable (Fisher–Yates)
            var chars = sb.ToString().ToCharArray();
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = chars[i]; chars[i] = chars[j]; chars[j] = tmp;
            }

            return new string(chars);
        }
        // <<< ONLY THIS METHOD CHANGED

        private string GenerateSalt()
        {
            return BCrypt.Net.BCrypt.GenerateSalt();
        }
    }
}
