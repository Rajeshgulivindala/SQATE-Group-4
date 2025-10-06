using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class DepartmentManagementView : UserControl
    {
        private readonly string _connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        private readonly ObservableCollection<DepartmentRow> _rows = new ObservableCollection<DepartmentRow>();

        // schema cache
        private bool _checkedSchema;
        private bool _hasColName;
        private bool _hasColDepartmentName;

        public DepartmentManagementView()
        {
            InitializeComponent();

            var grid = FindName("DepartmentsDataGrid") as DataGrid;
            if (grid != null) grid.ItemsSource = _rows;

            _ = LoadDepartmentsAsync();
        }

        #region Model
        private sealed class DepartmentRow
        {
            public int DepartmentID { get; set; }
            public string Name { get; set; } // unified display
            public string Description { get; set; }
            public string Location { get; set; }
            public string Phone { get; set; }
            public int? HeadOfDept { get; set; }
            public decimal Budget { get; set; }
            public bool IsActive { get; set; }
        }
        #endregion

        #region UI helpers
        private string GetText(string name)
        {
            var tb = FindName(name) as TextBox;
            return tb == null ? string.Empty : (tb.Text ?? string.Empty).Trim();
        }
        private bool GetBool(string name)
        {
            var cb = FindName(name) as CheckBox;
            return cb?.IsChecked == true;
        }
        private DataGrid GetGrid() => FindName("DepartmentsDataGrid") as DataGrid;
        private static string NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
        #endregion

        #region Schema detection
        private async Task EnsureSchemaAsync(SqlConnection external = null)
        {
            if (_checkedSchema) return;

            bool openedHere = false;
            var con = external ?? new SqlConnection(_connectionString);
            try
            {
                if (con.State != ConnectionState.Open)
                {
                    await con.OpenAsync();
                    openedHere = true;
                }

                using (var cmd = new SqlCommand(@"
SELECT c.name
FROM sys.columns c
JOIN sys.objects o ON o.object_id = c.object_id
WHERE o.type='U' AND o.name='Departments' AND c.name IN ('Name','DepartmentName');", con))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        var n = r["name"].ToString();
                        if (string.Equals(n, "Name", StringComparison.OrdinalIgnoreCase)) _hasColName = true;
                        if (string.Equals(n, "DepartmentName", StringComparison.OrdinalIgnoreCase)) _hasColDepartmentName = true;
                    }
                }

                if (!_hasColName && !_hasColDepartmentName)
                    throw new InvalidOperationException("Departments table must contain a 'Name' or 'DepartmentName' column.");
            }
            finally
            {
                _checkedSchema = true;
                if (openedHere) con.Dispose();
            }
        }
        #endregion

        #region Load
        private async Task LoadDepartmentsAsync()
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();
                    await EnsureSchemaAsync(con);

                    // Build SELECT with COALESCE of whichever columns exist
                    string selNameExpr;
                    if (_hasColName && _hasColDepartmentName)
                        selNameExpr = "COALESCE([DepartmentName],[Name])";
                    else if (_hasColDepartmentName)
                        selNameExpr = "[DepartmentName]";
                    else
                        selNameExpr = "[Name]";

                    using (var cmd = new SqlCommand($@"
SELECT DepartmentID,
       {selNameExpr} AS DeptName,
       Description, Location, Phone, HeadOfDept, Budget, IsActive
FROM dbo.Departments
ORDER BY DeptName;", con))
                    using (var r = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                    {
                        _rows.Clear();
                        while (await r.ReadAsync())
                        {
                            _rows.Add(new DepartmentRow
                            {
                                DepartmentID = r["DepartmentID"] == DBNull.Value ? 0 : Convert.ToInt32(r["DepartmentID"]),
                                Name = r["DeptName"] == DBNull.Value ? string.Empty : r["DeptName"].ToString(),
                                Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
                                Location = r["Location"] == DBNull.Value ? null : r["Location"].ToString(),
                                Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                                HeadOfDept = r["HeadOfDept"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["HeadOfDept"]),
                                Budget = r["Budget"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Budget"]),
                                IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load departments: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Add
        private async void AddDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            var name = GetText("txtDepartmentName");
            if (string.IsNullOrWhiteSpace(name)) name = GetText("txtName"); // support either textbox name

            var description = GetText("txtDescription");
            var location = GetText("txtLocation");
            var phone = GetText("txtPhone");
            var headTxt = GetText("txtHeadOfDept");
            var budgetTxt = GetText("txtBudget");
            var isActive = GetBool("chkIsActive");

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Department name is required.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? head = null;
            if (!string.IsNullOrWhiteSpace(headTxt))
            {
                if (int.TryParse(headTxt, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h)) head = h;
                else { MessageBox.Show("Head of Dept must be an integer id.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            }

            if (!decimal.TryParse(string.IsNullOrWhiteSpace(budgetTxt) ? "0" : budgetTxt,
                                  NumberStyles.Number, CultureInfo.InvariantCulture, out var budget))
            {
                MessageBox.Show("Budget must be numeric.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();
                    await EnsureSchemaAsync(con);

                    // Build dynamic column/values list to satisfy NOT NULLs
                    var nameColumns = new System.Collections.Generic.List<string>();
                    if (_hasColName) nameColumns.Add("[Name]");
                    if (_hasColDepartmentName) nameColumns.Add("[DepartmentName]");

                    var nameValues = string.Join(", ", nameColumns.Select(_ => "@NameValue"));
                    var nameCols = string.Join(", ", nameColumns);

                    var sql = $@"
INSERT INTO dbo.Departments
    ({nameCols}, Description, Location, Phone, HeadOfDept, Budget, IsActive)
VALUES
    ({nameValues}, @Description, @Location, @Phone, @HeadOfDept, @Budget, @IsActive);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.Add("@NameValue", SqlDbType.NVarChar, 150).Value = name; // fill all name-ish columns
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = (object)NullIfEmpty(description) ?? DBNull.Value;
                        cmd.Parameters.Add("@Location", SqlDbType.NVarChar, 200).Value = (object)NullIfEmpty(location) ?? DBNull.Value;
                        cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 50).Value = (object)NullIfEmpty(phone) ?? DBNull.Value;
                        cmd.Parameters.Add("@HeadOfDept", SqlDbType.Int).Value = (object)head ?? DBNull.Value;
                        cmd.Parameters.Add("@Budget", SqlDbType.Money).Value = budget;
                        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;

                        var newId = (int)await cmd.ExecuteScalarAsync();

                        _rows.Add(new DepartmentRow
                        {
                            DepartmentID = newId,
                            Name = name,
                            Description = description,
                            Location = location,
                            Phone = phone,
                            HeadOfDept = head,
                            Budget = budget,
                            IsActive = isActive
                        });

                        MessageBox.Show("Department added.", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding department: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Update
        private async void UpdateDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            var grid = GetGrid();
            var row = grid?.SelectedItem as DepartmentRow;
            if (row == null)
            {
                MessageBox.Show("Select a department to update.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var name = GetText("txtDepartmentName");
            if (string.IsNullOrWhiteSpace(name)) name = GetText("txtName");
            var description = GetText("txtDescription");
            var location = GetText("txtLocation");
            var phone = GetText("txtPhone");
            var headTxt = GetText("txtHeadOfDept");
            var budgetTxt = GetText("txtBudget");
            var isActive = GetBool("chkIsActive");

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Department name is required.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? head = null;
            if (!string.IsNullOrWhiteSpace(headTxt))
            {
                if (int.TryParse(headTxt, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h)) head = h;
                else { MessageBox.Show("Head of Dept must be an integer id.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            }

            if (!decimal.TryParse(string.IsNullOrWhiteSpace(budgetTxt) ? "0" : budgetTxt,
                                  NumberStyles.Number, CultureInfo.InvariantCulture, out var budget))
            {
                MessageBox.Show("Budget must be numeric.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();
                    await EnsureSchemaAsync(con);

                    var setParts = new System.Collections.Generic.List<string>();
                    if (_hasColName) setParts.Add("[Name] = @NameValue");
                    if (_hasColDepartmentName) setParts.Add("[DepartmentName] = @NameValue");
                    setParts.Add("Description = @Description");
                    setParts.Add("Location = @Location");
                    setParts.Add("Phone = @Phone");
                    setParts.Add("HeadOfDept = @HeadOfDept");
                    setParts.Add("Budget = @Budget");
                    setParts.Add("IsActive = @IsActive");

                    var sql = $@"
UPDATE dbo.Departments SET
    {string.Join(", ", setParts)}
WHERE DepartmentID = @DepartmentID;";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = row.DepartmentID;
                        cmd.Parameters.Add("@NameValue", SqlDbType.NVarChar, 150).Value = name;
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = (object)NullIfEmpty(description) ?? DBNull.Value;
                        cmd.Parameters.Add("@Location", SqlDbType.NVarChar, 200).Value = (object)NullIfEmpty(location) ?? DBNull.Value;
                        cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 50).Value = (object)NullIfEmpty(phone) ?? DBNull.Value;
                        cmd.Parameters.Add("@HeadOfDept", SqlDbType.Int).Value = (object)head ?? DBNull.Value;
                        cmd.Parameters.Add("@Budget", SqlDbType.Money).Value = budget;
                        cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;

                        await cmd.ExecuteNonQueryAsync();

                        row.Name = name;
                        row.Description = description;
                        row.Location = location;
                        row.Phone = phone;
                        row.HeadOfDept = head;
                        row.Budget = budget;
                        row.IsActive = isActive;
                        grid.Items.Refresh();

                        MessageBox.Show("Department updated.", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating department: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Delete
        private async void DeleteDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            var grid = GetGrid();
            var row = grid?.SelectedItem as DepartmentRow;
            if (row == null)
            {
                MessageBox.Show("Select a department to delete.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Delete department '{row.Name}'?",
                                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("DELETE FROM dbo.Departments WHERE DepartmentID=@id;", con))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = row.DepartmentID;
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }

                _rows.Remove(row);
                MessageBox.Show("Department deleted.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting department: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
