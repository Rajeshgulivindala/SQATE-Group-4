using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Services.Data;

namespace HospitalManagementSystem.Views.UserControls
{
    public class Department
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; }
    }

    public partial class StaffManagementView : UserControl
    {
        private const string ConnStr =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        private const bool REQUIRE_USER_ACCOUNT = true;

        public StaffManagementView()
        {
            InitializeComponent();
            _ = LoadDepartmentsAsync(); // async load from DB
        }

        private async Task LoadDepartmentsAsync()
        {
            try
            {
                var rows = await StaffRepository.GetDepartmentsAsync();
                DepartmentComboBox.ItemsSource = rows
                    .Select(r => new Department { DepartmentID = r.DepartmentID, DepartmentName = r.DepartmentName })
                    .ToList();
                if (DepartmentComboBox.Items.Count > 0) DepartmentComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load departments: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fullName = NameTextBox.Text.Trim();
                string email = EmailTextBox.Text.Trim();
                string phone = PhoneTextBox.Text.Trim();
                var dept = DepartmentComboBox.SelectedItem as Department;

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (dept == null)
                {
                    MessageBox.Show("Please select a Department.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string first = parts.First();
                string last = (parts.Length > 1) ? string.Join(" ", parts.Skip(1)) : "-";

                int? userId = null;
                if (REQUIRE_USER_ACCOUNT)
                {
                    userId = await TryGetUserIdByEmailAsync(email);
                    if (userId == null)
                    {
                        MessageBox.Show(
                            "No login account found for this email in dbo.Users.\n\n" +
                            "Create the user first (so we have a UserID) or make Staffs.UserID nullable.",
                            "Missing User",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                int newId = await StaffRepository.AddStaffAsync(new StaffRepository.NewStaff
                {
                    FirstName = first,
                    LastName = last,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    DepartmentID = dept.DepartmentID,
                    UserID = userId
                });

                StaffIDTextBox.Text = newId.ToString();
                MessageBox.Show(
                    $"✅ Staff saved.\n\nStaffID: {newId}\nName: {first} {last}\nDepartment: {dept.DepartmentName}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearInputFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to register staff: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearInputFields()
        {
            NameTextBox.Clear();
            EmailTextBox.Clear();
            PhoneTextBox.Clear();
            DOBPicker.SelectedDate = null;
            PasswordBox.Clear();
            StaffIDTextBox.Text = "[Auto-Generated on Save]";
            if (DepartmentComboBox.Items.Count > 0) DepartmentComboBox.SelectedIndex = 0;
        }

        // Look up UserID by Email (and fall back to Username if you want—uncomment the OR)
        private async Task<int?> TryGetUserIdByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            using (var cn = new SqlConnection(ConnStr))
            {
                await cn.OpenAsync();
                await StaffRepository.EnsureUsersEmailColumnAsync(cn);

                using (var cmd = new SqlCommand(
                    "SELECT TOP 1 UserID FROM dbo.Users WHERE Email=@em /* OR Username=@em */;", cn))
                {
                    cmd.Parameters.AddWithValue("@em", email);
                    var o = await cmd.ExecuteScalarAsync();
                    return (o == null || o == DBNull.Value) ? (int?)null : Convert.ToInt32(o);
                }
            }
        }
    }
}
