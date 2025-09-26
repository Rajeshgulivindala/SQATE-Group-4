using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace HospitalManagementSystem.Services.Authentication
{
    public interface ITokenService
    {
        void SaveRememberMeToken(string username);
        string GetRememberedUser();
        void ClearRememberMeToken();
    }

    public class TokenService : ITokenService
    {
        private const string RegistryKey = @"SOFTWARE\HospitalManagementSystem\Auth";
        private const string TokenValueName = "RememberToken";

        public void SaveRememberMeToken(string username)
        {
            try
            {
                // Simple encoding for now (can be enhanced later)
                var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(username));

                using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
                {
                    key?.SetValue(TokenValueName, encodedToken);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save remember token: {ex.Message}");
            }
        }

        public string GetRememberedUser()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey))
                {
                    var encodedToken = key?.GetValue(TokenValueName)?.ToString();
                    if (!string.IsNullOrEmpty(encodedToken))
                    {
                        return Encoding.UTF8.GetString(Convert.FromBase64String(encodedToken));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get remember token: {ex.Message}");
            }

            return null;
        }

        public void ClearRememberMeToken()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true))
                {
                    key?.DeleteValue(TokenValueName, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear remember token: {ex.Message}");
            }
        }
    }
}