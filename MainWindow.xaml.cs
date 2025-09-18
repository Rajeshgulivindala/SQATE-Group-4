using HospitalManagementSystem.Services.Authentication;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem
{
    public partial class MainWindow : Window
    {
        private readonly HospitalManagementSystem.Services.Navigation.HMSNavigationService _navigationService;

        public MainWindow()
        {
            InitializeComponent();

            // Fast initialization - no database calls
            _navigationService = new HospitalManagementSystem.Services.Navigation.HMSNavigationService(MainContentArea);

            // Set welcome message immediately
            SetWelcomeMessage();

            // Load dashboard immediately
            _navigationService.NavigateTo("Dashboard");
        }

        private void SetWelcomeMessage()
        {
            if (AuthenticationService.CurrentUser != null)
            {
                var user = AuthenticationService.CurrentUser;
                StatusText.Text = $"Welcome, {user.Username} ({user.Role}) | Ready";
                Title = $"HMS - {user.Username}";
            }
            else
            {
                StatusText.Text = "Ready";
                Title = "Hospital Management System";
            }
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var module = button?.Tag?.ToString();

            if (!string.IsNullOrEmpty(module))
            {
                _navigationService.NavigateTo(module);
                StatusText.Text = $"Current: {module} | {AuthenticationService.CurrentUser?.Username}";
            }
        }
    }
}