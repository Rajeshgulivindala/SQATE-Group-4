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
        /// Hide/show sections based on role.
        /// </summary>
        private void InitializeUserInterface()
        {
            string role = AuthenticationService.CurrentUser?.Role ?? "";

            // Default: show admin sections
            UserExpander.Visibility = Visibility.Visible;
            SystemExpander.Visibility = Visibility.Visible;
            ReportsExpander.Visibility = Visibility.Visible;
            DoctorExpander.Visibility = Visibility.Collapsed;
            NurseExpander.Visibility = Visibility.Collapsed;
            ClerkExpander.Visibility = Visibility.Collapsed;
            PatientExpander.Visibility = Visibility.Collapsed;

            switch (role)
            {
                case "Admin":
                    // admin sees admin sections only (already set)
                    break;

                case "Doctor":
                    UserExpander.Visibility = Visibility.Collapsed;
                    SystemExpander.Visibility = Visibility.Collapsed;
                    ReportsExpander.Visibility = Visibility.Collapsed;
                    DoctorExpander.Visibility = Visibility.Visible;
                    break;

                case "Nurse":
                    UserExpander.Visibility = Visibility.Collapsed;
                    SystemExpander.Visibility = Visibility.Collapsed;
                    ReportsExpander.Visibility = Visibility.Collapsed;
                    NurseExpander.Visibility = Visibility.Visible;
                    break;

                case "Clerk":
                    UserExpander.Visibility = Visibility.Collapsed;
                    SystemExpander.Visibility = Visibility.Collapsed;
                    ReportsExpander.Visibility = Visibility.Collapsed;
                    ClerkExpander.Visibility = Visibility.Visible;
                    break;

                case "Patient":
                    // Only the patient menu is visible for patients
                    UserExpander.Visibility = Visibility.Collapsed;
                    SystemExpander.Visibility = Visibility.Collapsed;
                    ReportsExpander.Visibility = Visibility.Collapsed;
                    DoctorExpander.Visibility = Visibility.Collapsed;
                    NurseExpander.Visibility = Visibility.Collapsed;
                    ClerkExpander.Visibility = Visibility.Collapsed;
                    PatientExpander.Visibility = Visibility.Visible;
                    break;

                default:
                    // unknown role -> minimalist view
                    UserExpander.Visibility = Visibility.Collapsed;
                    SystemExpander.Visibility = Visibility.Collapsed;
                    ReportsExpander.Visibility = Visibility.Collapsed;
                    DoctorExpander.Visibility = Visibility.Collapsed;
                    NurseExpander.Visibility = Visibility.Collapsed;
                    ClerkExpander.Visibility = Visibility.Collapsed;
                    PatientExpander.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// Load a sensible default view for each role.
        /// Patients go straight to "My Profile".
        /// </summary>
        private void ShowDefaultForRole()
        {
            string role = AuthenticationService.CurrentUser?.Role ?? "";

            if (role == "Patient")
            {
                MainContentArea.Content = new PatientSelfView();
            }
            else
            {
                // Keep your original dashboard as the default elsewhere
                MainContentArea.Content = new DashboardView();
            }
        }


        /// <summary>
        /// Handles all sidebar navigation.
        /// </summary>
        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                string tag = (btn.Tag ?? "").ToString().ToLowerInvariant();

                switch (tag)
                {
                    case "dashboard":
                        MainContentArea.Content = new DashboardView();
                        break;

                    // Admin & Staff (kept from your previous routes)
                    case "registeruser": MainContentArea.Content = new RegisterUserView(); break;
                    case "modifyroles": MainContentArea.Content = new ModifyRolesView(); break;
                    case "deactivateusers": MainContentArea.Content = new DeactivateUsersView(); break;
                    case "passwordreset": MainContentArea.Content = new PasswordResetView(); break;
                    case "staffmanagement": MainContentArea.Content = new StaffManagementView(); break;

                    case "departmentmanagement": MainContentArea.Content = new DepartmentManagementView(); break;
                    case "roommanagement": MainContentArea.Content = new RoomManagementView(); break;
                    case "hospitalsettings": MainContentArea.Content = new HospitalSettingsView(); break;
                    case "backupmaintenance": MainContentArea.Content = new BackupMaintenanceView(); break;
                    case "communication": MainContentArea.Content = new CommunicationManagementView(); break;

                    case "financialanalytics": MainContentArea.Content = new FinancialAnalyticsView(); break;
                    case "staffperformance": MainContentArea.Content = new StaffPerformanceView(); break;
                    case "auditlogs": MainContentArea.Content = new AuditLogsView(); break;
                    case "systemusage": MainContentArea.Content = new SystemUsageView(); break;

                    case "patientmanagement": MainContentArea.Content = new PatientManagementView(); break;
                    case "appointmentmanagement": MainContentArea.Content = new AppointmentManagementView(); break;
                    case "medicalrecords": MainContentArea.Content = new MedicalRecordsView(); break;
                    case "shiftmanagement": MainContentArea.Content = new ShiftManagementView(); break;
                    case "suppliermanagement": MainContentArea.Content = new SupplierManagementView(); break;
                    case "stockmonitoring": MainContentArea.Content = new StockMonitoringView(); break;
                    case "purchaseorders": MainContentArea.Content = new PurchaseOrdersView(); break;
                    case "purchaseapprovals": MainContentArea.Content = new PurchaseApprovalsView(); break;
                    case "billing": MainContentArea.Content = new BillingManagementView(); break;

                    // Patient only
                    case "patientprofile":
                        MainContentArea.Content = new PatientSelfView();
                        break;
                    

                    default:
                        // no-op
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
