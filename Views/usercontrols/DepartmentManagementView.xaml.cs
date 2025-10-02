using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Code-behind for DepartmentManagementView.xaml
    /// </summary>
    public partial class DepartmentManagementView : UserControl
    {
        private readonly string _connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True";

        // ---- Validation limits / patterns ----
        private const int NameMinLen = 2;
        private const int NameMaxLen = 100;
        private const int LocationMinLen = 2;
        private const int LocationMaxLen = 100;
        private const int DescriptionMaxLen = 1000;
        private static readonly Regex PhoneRegex = new Regex(@"^\+?[0-9()\-\s]{7,20}$", RegexOptions.Compiled);

        public class Department
        {
            public int DepartmentId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Location { get; set; }
            public string Phone { get; set; }
            public int HeadOfDept { get; set; }   // staff/employee ID
            public decimal Budget { get; set; }
            public bool IsActive { get; set; }
        }

        public ObservableCollection<Department> Departments { get; } = new ObservableCollection<Department>();

        public DepartmentManagementView()
        {
            InitializeComponent();
            DataContext = this;

            DepartmentsDataGrid.SelectionChanged += DepartmentsDataGrid_SelectionChanged;
            // Avoid newer discard lambda syntax for max compatibility
            Loaded += async (s, e) => await LoadDepartmentsFromDatabase();
        }

        // --- Populate inputs when row selected ---
        private void DepartmentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var d = DepartmentsDataGrid.SelectedItem as Department;
            if (d != null)
            {
                txtName.Text = d.Name ?? "";
                txtDescription.Text = d.Description ?? "";
                txtLocation.Text = d.Location ?? "";
                txtPhone.Text = d.Phone ?? "";
                txtHeadOfDept.Text = d.HeadOfDept.ToString(CultureInfo.InvariantCulture);
                txtBudget.Text = d.Budget.ToString(CultureInfo.InvariantCulture);
                chkIsActive.IsChecked = d.IsActive;

                ClearValidation();
            }
        }

        // --- Load data ---
        private async Task LoadDepartmentsFromDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    const string sql = @"
SELECT DepartmentId, Name, Description, Location, Phone, HeadOfDept, Budget, IsActive
FROM Departments
ORDER BY Name;";

                    using (var cmd = new SqlCommand(sql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        Departments.Clear();
                        while (await reader.ReadAsync())
                        {
                            var dept = new Department
                            {
                                DepartmentId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Location = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Phone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                HeadOfDept = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                Budget = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6),
                                IsActive = !reader.IsDBNull(7) && reader.GetBoolean(7)
                            };
                            Departments.Add(dept);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load departments: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === VALIDATION HELPERS =======================================================

        private void ClearValidation()
        {
            MarkValid(txtName);
            MarkValid(txtDescription);
            MarkValid(txtLocation);
            MarkValid(txtPhone);
            MarkValid(txtHeadOfDept);
            MarkValid(txtBudget);
        }

        private static void MarkInvalid(Control c, string msg)
        {
            if (c == null) return;
            c.BorderBrush = Brushes.Red;
            c.BorderThickness = new Thickness(1.5);
            c.ToolTip = msg;
        }

        private static void MarkValid(Control c)
        {
            if (c == null) return;
            c.ClearValue(Border.BorderBrushProperty);
            c.ClearValue(Border.BorderThicknessProperty);
            c.ToolTip = null;
        }

        /// <summary>
        /// Read inputs and validate every required field. If valid, outputs a Department.
        /// </summary>
        private bool TryReadInputs(out Department dept, bool requireExisting)
        {
            ClearValidation();
            dept = null;
            int deptId = 0;

            if (requireExisting)
            {
                var sel = DepartmentsDataGrid.SelectedItem as Department;
                if (sel == null)
                {
                    MessageBox.Show("Select a department in the table first.", "No Selection",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                deptId = sel.DepartmentId;
            }

            var errors = new StringBuilder();
            Control firstInvalid = null;

            string name = (txtName.Text ?? "").Trim();
            string description = (txtDescription.Text ?? "").Trim();
            string location = (txtLocation.Text ?? "").Trim();
            string phone = (txtPhone.Text ?? "").Trim();
            string headText = (txtHeadOfDept.Text ?? "").Trim();
            string budgetText = (txtBudget.Text ?? "").Trim();
            bool isActive = chkIsActive.IsChecked ?? true;

            // ---- Name (required, length) ----
            if (string.IsNullOrWhiteSpace(name) || name.Length < NameMinLen || name.Length > NameMaxLen)
            {
                var msg = string.Format("Name is required (length {0}-{1}).", NameMinLen, NameMaxLen);
                errors.AppendLine("• " + msg);
                MarkInvalid(txtName, msg);
                if (firstInvalid == null) firstInvalid = txtName;
            }
            else
            {
                // naive in-memory uniqueness check (case-insensitive)
                foreach (var d in Departments)
                {
                    if (!requireExisting || d.DepartmentId != deptId)
                    {
                        if (string.Equals((d.Name ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase))
                        {
                            var msg = "A department with this name already exists.";
                            errors.AppendLine("• " + msg);
                            MarkInvalid(txtName, msg);
                            if (firstInvalid == null) firstInvalid = txtName;
                            break;
                        }
                    }
                }
            }

            // ---- Description (optional, max len) ----
            if (description.Length > DescriptionMaxLen)
            {
                var msg = string.Format("Description is too long (max {0} characters).", DescriptionMaxLen);
                errors.AppendLine("• " + msg);
                MarkInvalid(txtDescription, msg);
                if (firstInvalid == null) firstInvalid = txtDescription;
            }

            // ---- Location (required, length) ----
            if (string.IsNullOrWhiteSpace(location) || location.Length < LocationMinLen || location.Length > LocationMaxLen)
            {
                var msg = string.Format("Location is required (length {0}-{1}).", LocationMinLen, LocationMaxLen);
                errors.AppendLine("• " + msg);
                MarkInvalid(txtLocation, msg);
                if (firstInvalid == null) firstInvalid = txtLocation;
            }

            // ---- Phone (required, basic pattern) ----
            if (string.IsNullOrWhiteSpace(phone) || !PhoneRegex.IsMatch(phone))
            {
                var msg = "Phone is required (digits, spaces, (), -, optional +).";
                errors.AppendLine("• " + msg);
                MarkInvalid(txtPhone, msg);
                if (firstInvalid == null) firstInvalid = txtPhone;
            }

            // ---- HeadOfDept (required, positive int) ----
            int headId;
            if (!int.TryParse(headText, NumberStyles.Integer, CultureInfo.InvariantCulture, out headId) || headId <= 0)
            {
                var msg = "Head of Dept must be a positive integer (staff/employee ID).";
                errors.AppendLine("• " + msg);
                MarkInvalid(txtHeadOfDept, msg);
                if (firstInvalid == null) firstInvalid = txtHeadOfDept;
            }

            // ---- Budget (required, non-negative decimal; clamp to 2 dp) ----
            decimal budget;
            if (!decimal.TryParse(budgetText, NumberStyles.Number, CultureInfo.InvariantCulture, out budget) || budget < 0m)
            {
                var msg = "Budget must be a non-negative number (e.g., 1000.00).";
                errors.AppendLine("• " + msg);
                MarkInvalid(txtBudget, msg);
                if (firstInvalid == null) firstInvalid = txtBudget;
            }
            else
            {
                budget = decimal.Round(budget, 2, MidpointRounding.AwayFromZero);
            }

            if (errors.Length > 0)
            {
                MessageBox.Show("Please fix the following:\n\n" + errors, "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                if (firstInvalid != null) firstInvalid.Focus();
                return false;
            }

            // All good
            dept = new Department
            {
                DepartmentId = deptId,
                Name = name,
                Description = description,
                Location = location,
                Phone = phone,
                HeadOfDept = headId,
                Budget = budget,
                IsActive = isActive
            };
            return true;
        }

        // --- Add ---
        private async void AddDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            Department newDept;
            if (!TryReadInputs(out newDept, false)) return;

            try
            {
                await AddDepartmentToDatabase(newDept);
                await LoadDepartmentsFromDatabase();
                MessageBox.Show(string.Format("Department '{0}' added.", newDept.Name), "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding department: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Update ---
        private async void UpdateDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            Department updated;
            if (!TryReadInputs(out updated, true)) return;

            try
            {
                await UpdateDepartmentInDatabase(updated);
                await LoadDepartmentsFromDatabase();
                MessageBox.Show(string.Format("Department '{0}' updated.", updated.Name), "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating department: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Delete ---
        private async void DeleteDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = DepartmentsDataGrid.SelectedItem as Department;
            if (selected == null)
            {
                MessageBox.Show("Select a department to delete.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                string.Format("Delete department '{0}' (ID {1})?", selected.Name, selected.DepartmentId),
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await DeleteDepartmentFromDatabase(selected.DepartmentId);
                await LoadDepartmentsFromDatabase();
                MessageBox.Show(string.Format("Department '{0}' deleted.", selected.Name), "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting department: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Clear inputs ---
        private void ClearInputs()
        {
            txtName.Clear();
            txtDescription.Clear();
            txtLocation.Clear();
            txtPhone.Clear();
            txtHeadOfDept.Clear();
            txtBudget.Clear();
            chkIsActive.IsChecked = true;
            DepartmentsDataGrid.SelectedItem = null;
            ClearValidation();
        }

        // --- SQL ops ---
        private async Task AddDepartmentToDatabase(Department department)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = @"
INSERT INTO Departments (Name, Description, Location, Phone, HeadOfDept, Budget, IsActive)
VALUES (@Name, @Description, @Location, @Phone, @HeadOfDept, @Budget, @IsActive);";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Name", department.Name);
                    cmd.Parameters.AddWithValue("@Description", department.Description);
                    cmd.Parameters.AddWithValue("@Location", department.Location);
                    cmd.Parameters.AddWithValue("@Phone", department.Phone);
                    cmd.Parameters.AddWithValue("@HeadOfDept", department.HeadOfDept);
                    cmd.Parameters.AddWithValue("@Budget", department.Budget);
                    cmd.Parameters.AddWithValue("@IsActive", department.IsActive);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task UpdateDepartmentInDatabase(Department department)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = @"
UPDATE Departments
SET Name=@Name, Description=@Description, Location=@Location, Phone=@Phone,
    HeadOfDept=@HeadOfDept, Budget=@Budget, IsActive=@IsActive
WHERE DepartmentId=@DepartmentId;";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Name", department.Name);
                    cmd.Parameters.AddWithValue("@Description", department.Description);
                    cmd.Parameters.AddWithValue("@Location", department.Location);
                    cmd.Parameters.AddWithValue("@Phone", department.Phone);
                    cmd.Parameters.AddWithValue("@HeadOfDept", department.HeadOfDept);
                    cmd.Parameters.AddWithValue("@Budget", department.Budget);
                    cmd.Parameters.AddWithValue("@IsActive", department.IsActive);
                    cmd.Parameters.AddWithValue("@DepartmentId", department.DepartmentId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task DeleteDepartmentFromDatabase(int departmentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = "DELETE FROM Departments WHERE DepartmentId=@DepartmentId;";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@DepartmentId", departmentId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
