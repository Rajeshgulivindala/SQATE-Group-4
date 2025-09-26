using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Views.UserControls;

namespace HospitalManagementSystem.Views.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the button that was clicked
            Button clickedButton = sender as Button;
            if (clickedButton == null) return;

            // Get the 'Tag' property, which we've set to identify the view
            string viewName = clickedButton.Tag.ToString();

            // Create a new instance of the corresponding UserControl based on the Tag
            UserControl newView = null;
            switch (viewName)
            {
                case "Dashboard":
                    newView = new DashboardView();
                    break;
                case "registeruser":
                    newView = new RegisterUserView();
                    break;
                case "modifyroles":
                    newView = new ModifyRolesView();
                    break;
                case "deactivateusers":
                    newView = new DeactivateUsersView();
                    break;
                case "passwordreset":
                    newView = new PasswordResetView();
                    break;
                case "departmentmanagement":
                    newView = new DepartmentManagementView();
                    break;
                case "roommanagement":
                    newView = new RoomManagementView();
                    break;
                case "hospitalsettings":
                    newView = new HospitalSettingsView();
                    break;
                case "backupmaintenance":
                    newView = new BackupMaintenanceView();
                    break;
                case "SystemUsage":
                    newView = new SystemUsageView();
                    break;
                case "FinancialAnalytics":
                    newView = new FinancialAnalyticsView();
                    break;
                case "StaffPerformance":
                    newView = new StaffPerformanceView();
                    break;
                case "AuditLogs":
                    newView = new AuditLogsView();
                    break;
                case "suppliermanagement":
                    newView = new SupplierManagementView();
                    break;
                case "stockmonitoring":
                    newView = new StockMonitoringView();
                    break;
                case "purchaseorders":
                    newView = new PurchaseOrdersView();
                    break;
                case "purchaseapprovals":
                    newView = new PurchaseApprovalsView();
                    break;
                case "patientmanagement":
                    newView = new PatientManagementView();
                    break;
                case "appointmentmanagement":
                    newView = new AppointmentManagementView();
                    break;
                case "MedicalRecords":
                    newView = new MedicalRecordsView();
                    break;
            }

            // Set the ContentControl's content to the new view
            if (newView != null)
            {
                MainContentArea.Content = newView;
            }
        }
    }
}