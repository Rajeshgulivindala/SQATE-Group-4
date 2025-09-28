
using System.Windows;
using HospitalManagementSystem.Services.Authentication;
using HospitalManagementSystem.Views.UserControls;
// This using statement is now assumed to contain all UserControl views (Dashboard, Patient, Appointment, Admin, etc.)

namespace HospitalManagementSystem.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeUserInterface();
        }

        /// <summary>
        /// Restricts UI elements based on the current user's role.
        /// If the user is a Doctor or Nurse, administrative Expander controls are hidden.
        /// </summary>
        private void InitializeUserInterface()
        {
            string userRole = AuthenticationService.CurrentUser?.Role;

            // Administrative features that only the Admin should see
            if (userRole == "Doctor" || userRole == "Nurse")
            {
                // Hide User Management
                UserExpander.Visibility = Visibility.Collapsed;

                // Hide System Configuration
                SystemExpander.Visibility = Visibility.Collapsed;

                // Optionally hide other non-clinical/non-relevant sections
                // Example: Hide Inventory Management for a Doctor
                if (userRole == "Doctor")
                {
                    NurseExpander.Visibility = Visibility.Collapsed;
                    InventoryExpander.Visibility = Visibility.Collapsed;
                    ReportsExpander.Visibility = Visibility.Collapsed;
                }
            }

            // Specific UI visibility for Doctor
            if (userRole == "Doctor")
            {
                // Ensure Doctor Dashboard is visible (it's the only one left after Admin parts are hidden)
                DoctorExpander.Visibility = Visibility.Visible;
            }
            else
            {
                // If the user is Admin or Nurse, hide the Doctor-specific dashboard
                if (userRole == "Nurse")
                    ReportsExpander.Visibility = Visibility.Collapsed;
                DoctorExpander.Visibility = Visibility.Collapsed;
                if (userRole == "Admin")
                    NurseExpander.Visibility = Visibility.Collapsed;
                DoctorExpander.Visibility = Visibility.Collapsed;
            }


        }

        /// <summary>
        /// Handles the click events for all navigation buttons to load corresponding views 
        /// into the MainContentArea based on the button's Tag property.
        /// </summary>
        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                // Safely retrieve the Tag and convert to lowercase for case-insensitive comparison
                string tag = button.Tag?.ToString().ToLowerInvariant();

                // Use a switch statement for clean handling of multiple view navigations
                switch (tag)
                {
                    // ------------------------------------
                    // --- Clinical / Doctor / Nurse Views ---
                    // ------------------------------------

                    case "patientmanagement":
                        MainContentArea.Content = new PatientManagementView();
                        break;
                    case "appointmentmanagement":
                        MainContentArea.Content = new AppointmentManagementView();
                        break;
                    case "medicalrecords":
                        MainContentArea.Content = new MedicalRecordsView();
                        break;

                    case "shiftmanagement":
                        MainContentArea.Content = new ShiftManagementView();
                        break;


                    // ------------------------------------
                    // --- Admin: User Management Views ---
                    // ------------------------------------
                    case "registeruser":
                        MainContentArea.Content = new RegisterUserView();
                        break;
                    case "modifyroles":
                        MainContentArea.Content = new ModifyRolesView();
                        break;
                    case "deactivateusers":
                        MainContentArea.Content = new DeactivateUsersView();
                        break;
                    case "passwordreset":
                        MainContentArea.Content = new PasswordResetView();
                        break;


                    // ------------------------------------
                    // --- Admin: System Configuration Views ---
                    // ------------------------------------
                    case "departmentmanagement":
                        MainContentArea.Content = new DepartmentManagementView();
                        break;
                    case "roommanagement":
                        MainContentArea.Content = new RoomManagementView();
                        break;
                    case "hospitalsettings":
                        MainContentArea.Content = new HospitalSettingsView();
                        break;
                    case "backupmaintenance":
                        MainContentArea.Content = new BackupMaintenanceView();
                        break;


                    // ------------------------------------
                    // --- Reporting & Analytics Views ---
                    // ------------------------------------

                    case "financialanalytics":
                        MainContentArea.Content = new FinancialAnalyticsView();
                        break;
                    case "staffperformance":
                        MainContentArea.Content = new StaffPerformanceView();
                        break;
                    case "auditlogs":
                        MainContentArea.Content = new AuditLogsView();
                        break;


                    // ------------------------------------
                    // --- Inventory Management Views ---
                    // ------------------------------------
                    case "suppliermanagement":
                        MainContentArea.Content = new SupplierManagementView();
                        break;
                    case "stockmonitoring":
                        MainContentArea.Content = new StockMonitoringView();
                        break;
                    case "purchaseorders":
                        MainContentArea.Content = new PurchaseOrdersView();
                        break;
                    case "purchaseapprovals":
                        // Grouping purchase order/approval into one view for simplicity
                        MainContentArea.Content = new PurchaseApprovalsView();
                        break;

                    default:
                        // Log or handle unhandled navigation tags
                        System.Diagnostics.Debug.WriteLine($"Unhandled navigation tag: {tag}");
                        break;
                }
            }
        }

        // You would typically include a Logout button handler here as well
        // private void LogoutButton_Click(object sender, RoutedEventArgs e) { ... }
    }
}
