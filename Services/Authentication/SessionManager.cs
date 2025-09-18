using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace HospitalManagementSystem.Services.Authentication
{
    public class SessionManager
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly DispatcherTimer _sessionTimer;
        private DateTime _lastActivity;
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _warningTime = TimeSpan.FromMinutes(2);

        public SessionManager(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
            _sessionTimer = new DispatcherTimer();
            _sessionTimer.Interval = TimeSpan.FromMinutes(1); // Check every minute
            _sessionTimer.Tick += SessionTimer_Tick;
            _lastActivity = DateTime.Now;
        }

        public void StartSession()
        {
            _lastActivity = DateTime.Now;
            _sessionTimer.Start();
        }

        public void UpdateActivity()
        {
            _lastActivity = DateTime.Now;
        }

        public void EndSession()
        {
            _sessionTimer.Stop();
            _currentUserService.ClearCurrentUser();
        }

        private void SessionTimer_Tick(object sender, EventArgs e)
        {
            var timeSinceLastActivity = DateTime.Now - _lastActivity;

            if (timeSinceLastActivity >= _sessionTimeout)
            {
                // Session expired - force logout
                EndSession();
                MessageBox.Show("Your session has expired. Please log in again.", "Session Expired",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                // Redirect to login
                Application.Current.MainWindow = new Views.Windows.LoginWindow();
                Application.Current.MainWindow.Show();
            }
            else if (timeSinceLastActivity >= (_sessionTimeout - _warningTime))
            {
                // Show warning
                var result = MessageBox.Show(
                    $"Your session will expire in {(_sessionTimeout - timeSinceLastActivity).Minutes} minutes. Continue?",
                    "Session Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    UpdateActivity(); // Extend session
                }
            }
        }
    }
}