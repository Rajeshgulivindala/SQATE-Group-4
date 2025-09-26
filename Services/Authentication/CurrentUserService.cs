using System;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Services.Authentication
{
    public interface ICurrentUserService
    {
        User CurrentUser { get; }
        void SetCurrentUser(User user);
        void ClearCurrentUser();
        bool IsAuthenticated { get; }
        bool HasRole(string role);
        bool HasPermission(string permission);
    }

    public class CurrentUserService : ICurrentUserService
    {
        private User _currentUser;
        private DateTime _sessionStartTime;
        private DateTime _lastActivity;

        public User CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
            _sessionStartTime = DateTime.Now;
            _lastActivity = DateTime.Now;
        }

        public void ClearCurrentUser()
        {
            _currentUser = null;
        }

        public bool HasRole(string role)
        {
            return _currentUser?.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
        }

        public bool HasPermission(string permission)
        {
            if (!IsAuthenticated) return false;

            // Define role-based permissions
            switch (_currentUser.Role.ToLower())
            {
                case "admin":
                    return true; // Admin has all permissions
                case "doctor":
                    return permission.In("ViewPatients", "EditPatients", "ViewMedicalRecords",
                                       "EditMedicalRecords", "ViewAppointments", "CreateAppointments");
                case "nurse":
                    return permission.In("ViewPatients", "EditPatients", "ViewAppointments",
                                       "ViewInventory", "UpdateInventory");
                case "clerk":
                    return permission.In("ViewPatients", "CreatePatients", "ViewAppointments",
                                       "CreateAppointments", "ViewReports");
                default:
                    return false;
            }
        }

        public bool IsSessionExpired()
        {
            if (!IsAuthenticated) return true;

            var sessionTimeout = TimeSpan.FromMinutes(30);
            return DateTime.Now.Subtract(_lastActivity) > sessionTimeout;
        }

        public void UpdateActivity()
        {
            _lastActivity = DateTime.Now;
        }
    }
}

// Extension method for permission checking
public static class StringExtensions
{
    public static bool In(this string value, params string[] values)
    {
        return Array.IndexOf(values, value) >= 0;
    }
}