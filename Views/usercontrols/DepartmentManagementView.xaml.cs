using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Interaction logic for DepartmentManagementView.xaml
    /// </summary>
    public partial class DepartmentManagementView : UserControl
    {
        // TODO: **CRITICAL: Replace this with your actual database connection string.**
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True";

        /// <summary>
        /// A data model for a department, matching the columns in the DataGrid.
        /// </summary>
        public class Department
        {
            public int DepartmentId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Location { get; set; }
            public string Phone { get; set; }
            public string HeadOfDept { get; set; }
            public decimal Budget { get; set; }
            public bool IsActive { get; set; }
        }

        // An ObservableCollection for the departments, which automatically updates the UI.
        public ObservableCollection<Department> Departments { get; set; }

        public DepartmentManagementView()
        {
            InitializeComponent();
            Departments = new ObservableCollection<Department>();

            // Set the DataContext to this instance to enable XAML binding.
            this.DataContext = this;

            // Load departments when the view is initialized.
            LoadDepartmentsFromDatabase();
        }

        /// <summary>
        /// Loads department data from the database.
        /// </summary>
        private async void LoadDepartmentsFromDatabase()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // Adjust the SQL query to match your database schema
                    string sqlQuery = "SELECT DepartmentId, Name, Description, Location, Phone, HeadOfDept, Budget, IsActive FROM Departments";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            Departments.Clear();
                            while (await reader.ReadAsync())
                            {
                                var department = new Department
                                {
                                    DepartmentId = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Description = reader.GetString(2),
                                    Location = reader.GetString(3),
                                    Phone = reader.GetString(4),
                                    // CORRECTED: Read the HeadOfDept as an object and convert to string.
                                    // This assumes that the data in the database is an integer that
                                    // needs to be represented as a string.
                                    HeadOfDept = reader.GetValue(5).ToString(),
                                    Budget = reader.GetDecimal(6),
                                    IsActive = reader.GetBoolean(7)
                                };
                                Departments.Add(department);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load departments: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the "Add Department" button.
        /// </summary>
        private async void AddDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Ensure your XAML controls have x:Name attributes to get the values.
            // For example: <TextBox x:Name="txtName"/>

            // Placeholder for getting values from the UI
            // string newName = txtName.Text;
            // ... and so on for other fields.

            // Simulate a new department from UI input
            var newDepartment = new Department
            {
                Name = "New Department",
                Description = "A new department.",
                Location = "Floor 4, Block D",
                Phone = "555-0104",
                HeadOfDept = "Dr. New Guy",
                Budget = 100000.00m,
                IsActive = true
            };

            try
            {
                await AddDepartmentToDatabase(newDepartment);
                Departments.Add(newDepartment); // Add to ObservableCollection for UI update
                MessageBox.Show($"Department '{newDepartment.Name}' added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while adding the department: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the "Update Department" button.
        /// </summary>
        private async void UpdateDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentsDataGrid.SelectedItem is Department selectedDept)
            {
                // TODO: Update the selectedDept object with values from your UI controls.
                // For demonstration, we'll just change the budget.
                selectedDept.Budget += 10000;

                try
                {
                    await UpdateDepartmentInDatabase(selectedDept);
                    MessageBox.Show($"Department '{selectedDept.Name}' has been updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while updating the department: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a department to update.", "No Department Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the click event for the "Delete Department" button.
        /// </summary>
        private async void DeleteDepartmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentsDataGrid.SelectedItem is Department selectedDept)
            {
                try
                {
                    await DeleteDepartmentFromDatabase(selectedDept);
                    Departments.Remove(selectedDept);
                    MessageBox.Show($"Department '{selectedDept.Name}' has been deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while deleting the department: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a department to delete.", "No Department Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Adds a department to the database.
        /// </summary>
        private async Task AddDepartmentToDatabase(Department department)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "INSERT INTO Departments (Name, Description, Location, Phone, HeadOfDept, Budget, IsActive) VALUES (@Name, @Description, @Location, @Phone, @HeadOfDept, @Budget, @IsActive)";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", department.Name);
                    command.Parameters.AddWithValue("@Description", department.Description);
                    command.Parameters.AddWithValue("@Location", department.Location);
                    command.Parameters.AddWithValue("@Phone", department.Phone);
                    command.Parameters.AddWithValue("@HeadOfDept", department.HeadOfDept);
                    command.Parameters.AddWithValue("@Budget", department.Budget);
                    command.Parameters.AddWithValue("@IsActive", department.IsActive);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Updates a department in the database.
        /// </summary>
        private async Task UpdateDepartmentInDatabase(Department department)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Departments SET Name = @Name, Description = @Description, Location = @Location, Phone = @Phone, HeadOfDept = @HeadOfDept, Budget = @Budget, IsActive = @IsActive WHERE DepartmentId = @DepartmentId";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", department.Name);
                    command.Parameters.AddWithValue("@Description", department.Description);
                    command.Parameters.AddWithValue("@Location", department.Location);
                    command.Parameters.AddWithValue("@Phone", department.Phone);
                    command.Parameters.AddWithValue("@HeadOfDept", department.HeadOfDept);
                    command.Parameters.AddWithValue("@Budget", department.Budget);
                    command.Parameters.AddWithValue("@IsActive", department.IsActive);
                    command.Parameters.AddWithValue("@DepartmentId", department.DepartmentId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Deletes a department from the database.
        /// </summary>
        private async Task DeleteDepartmentFromDatabase(Department department)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Departments WHERE DepartmentId = @DepartmentId";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@DepartmentId", department.DepartmentId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
