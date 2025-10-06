using System.Windows;
using HospitalManagementSystem.Services.Authentication;
using HospitalManagementSystem.Views.UserControls;

namespace HospitalManagementSystem.Views.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeUserInterface();
            ShowDefaultForRole();
        }

        /// <summary>
        /// Show/hide sidebar sections based on role.
        /// </summary>
        private void InitializeUserInterface()
        {
            string role = AuthenticationService.CurrentUser?.Role ?? string.Empty;

            // Hide all by default
            UserExpander.Visibility = Visibility.Collapsed;
            SystemExpander.Visibility = Visibility.Collapsed;
            ReportsExpander.Visibility = Visibility.Collapsed;
            DoctorExpander.Visibility = Visibility.Collapsed;
            NurseExpander.Visibility = Visibility.Collapsed;
            ClerkExpander.Visibility = Visibility.Collapsed;
            InventoryExpander.Visibility = Visibility.Collapsed;
            PatientExpander.Visibility = Visibility.Collapsed;

            switch (role)
            {
                case "Admin":
                    UserExpander.Visibility = Visibility.Visible;
                    SystemExpander.Visibility = Visibility.Visible;
                    ReportsExpander.Visibility = Visibility.Visible;
                    InventoryExpander.Visibility = Visibility.Visible; // optional
                    break;

                case "Doctor":
                    DoctorExpander.Visibility = Visibility.Visible;
                    break;

                case "Nurse":
                    NurseExpander.Visibility = Visibility.Visible;
                    break;

                case "Clerk":
                    ClerkExpander.Visibility = Visibility.Visible;
                    InventoryExpander.Visibility = Visibility.Visible; // if clerks manage inventory
                    break;

                case "Patient":
                    PatientExpander.Visibility = Visibility.Visible;
                    break;

                default:
                    // Unknown role: keep minimal UI (dashboard only)
                    break;
            }
        }

        /// <summary>
        /// Route to a sensible default per role.
        /// Patients go to My Profile; others to Dashboard.
        /// </summary>
        private void ShowDefaultForRole()
        {
            string role = AuthenticationService.CurrentUser?.Role ?? string.Empty;

            if (role == "Patient")
                MainContentArea.Content = new PatientSelfView();
            else
                MainContentArea.Content = new DashboardView();
        }

        /// <summary>
        /// Centralized navigation by Tag.
        /// </summary>
        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                string tag = (button.Tag ?? string.Empty).ToString().ToLowerInvariant();

                switch (tag)
                {
                    // Common
                    case "dashboard": MainContentArea.Content = new DashboardView(); break;

                    // Patient
                    case "patientprofile": MainContentArea.Content = new PatientSelfView(); break;

                    // Clinical
                    case "patientmanagement": MainContentArea.Content = new PatientManagementView(); break;
                    case "appointmentmanagement": MainContentArea.Content = new AppointmentManagementView(); break;
                    case "medicalrecords": MainContentArea.Content = new MedicalRecordsView(); break;
                    case "shiftmanagement": MainContentArea.Content = new ShiftManagementView(); break;

                    // Admin - users
                    case "registeruser": MainContentArea.Content = new RegisterUserView(); break;
                    case "modifyroles": MainContentArea.Content = new ModifyRolesView(); break;
                    case "deactivateusers": MainContentArea.Content = new DeactivateUsersView(); break;
                    case "passwordreset": MainContentArea.Content = new PasswordResetView(); break;
                    case "staffmanagement": MainContentArea.Content = new StaffManagementView(); break;

                    // Admin - system
                    case "departmentmanagement": MainContentArea.Content = new DepartmentManagementView(); break;
                    case "roommanagement": MainContentArea.Content = new RoomManagementView(); break;
                    case "hospitalsettings": MainContentArea.Content = new HospitalSettingsView(); break;
                    case "backupmaintenance": MainContentArea.Content = new BackupMaintenanceView(); break;
                    case "communication": MainContentArea.Content = new CommunicationManagementView(); break;

                    // Reports
                    case "systemusage": MainContentArea.Content = new SystemUsageView(); break;
                    case "financialanalytics": MainContentArea.Content = new FinancialAnalyticsView(); break;
                    case "staffperformance": MainContentArea.Content = new StaffPerformanceView(); break;
                    case "auditlogs": MainContentArea.Content = new AuditLogsView(); break;

                    // Inventory
                    case "suppliermanagement": MainContentArea.Content = new SupplierManagementView(); break;
                    case "stockmonitoring": MainContentArea.Content = new StockMonitoringView(); break;
                    case "purchaseorders": MainContentArea.Content = new PurchaseOrdersView(); break;
                    case "purchaseapprovals": MainContentArea.Content = new PurchaseApprovalsView(); break;

                    // Billing
                    case "billing": MainContentArea.Content = new BillingManagementView(); break;

                    default:
                        System.Diagnostics.Debug.WriteLine("Unhandled navigation tag: " + tag);
                        break;
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AuthenticationService.CurrentUser = null;
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}
