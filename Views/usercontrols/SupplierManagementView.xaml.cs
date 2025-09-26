using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Represents a supplier data model.
    /// The 'Id' property is automatically numbered by the database upon insertion.
    /// </summary>
    public class Supplier
    {
        public int SupplierId { get; set; }
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PaymentTerms { get; set; }
        public int? Rating { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Interaction logic for SupplierManagementView.xaml
    /// </summary>
    public partial class SupplierManagementView : UserControl
    {
        // TODO: **CRITICAL: Replace this with your actual database connection string.**
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        // This is a collection to hold supplier data and automatically update the UI.
        private ObservableCollection<Supplier> suppliers = new ObservableCollection<Supplier>();

        public SupplierManagementView()
        {
            InitializeComponent();
            SuppliersDataGrid.ItemsSource = suppliers;
            this.Loaded += SupplierManagementView_Loaded;
        }

        /// <summary>
        /// Loads data when the view is loaded.
        /// </summary>
        private async void SupplierManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSuppliersAsync();
        }

        /// <summary>
        /// Loads all suppliers from the database and populates the data grid.
        /// </summary>
        private async Task LoadSuppliersAsync()
        {
            suppliers.Clear();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sqlQuery = "SELECT SupplierId, CompanyName, ContactPerson, Phone, Email, Address, City, Country, PaymentTerms, Rating, IsActive, CreatedDate, Notes FROM Suppliers";
                    using (var command = new SqlCommand(sqlQuery, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                suppliers.Add(new Supplier
                                {
                                    SupplierId = reader.GetInt32(0),
                                    CompanyName = reader.GetString(1),
                                    ContactPerson = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Address = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    City = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    Country = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    PaymentTerms = reader.IsDBNull(8) ? null : reader.GetString(8),
                                    Rating = reader.IsDBNull(9) ? null : (int?)reader.GetInt32(9),
                                    IsActive = reader.GetBoolean(10),
                                    CreatedDate = reader.GetDateTime(11),
                                    Notes = reader.IsDBNull(12) ? null : reader.GetString(12)
                                });
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
                MessageBox.Show($"Failed to load suppliers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Add" button click event.
        /// The database will automatically assign an ID for the new record.
        /// </summary>
        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"INSERT INTO Suppliers (CompanyName, ContactPerson, Phone, Email, Address, City, Country, PaymentTerms, Rating, IsActive, CreatedDate, Notes)
                                   VALUES (@CompanyName, @ContactPerson, @Phone, @Email, @Address, @City, @Country, @PaymentTerms, @Rating, @IsActive, GETDATE(), @Notes);";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Supplier added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSuppliersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add supplier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Update" button click event.
        /// </summary>
        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            var selectedSupplier = SuppliersDataGrid.SelectedItem as Supplier;
            if (selectedSupplier == null) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"UPDATE Suppliers SET CompanyName = @CompanyName, ContactPerson = @ContactPerson, Phone = @Phone, Email = @Email, Address = @Address, City = @City, Country = @Country, PaymentTerms = @PaymentTerms, Rating = @Rating, IsActive = @IsActive, Notes = @Notes
                                   WHERE SupplierId = @SupplierId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        command.Parameters.AddWithValue("@SupplierId", selectedSupplier.SupplierId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Supplier updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSuppliersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update supplier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Delete" button click event.
        /// </summary>
        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedSupplier = SuppliersDataGrid.SelectedItem as Supplier;
            if (selectedSupplier == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {selectedSupplier.CompanyName}?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "DELETE FROM Suppliers WHERE SupplierId = @SupplierId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@SupplierId", selectedSupplier.SupplierId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Supplier deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSuppliersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete supplier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the selection change in the DataGrid to populate the form for editing.
        /// </summary>
        private void SuppliersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSupplier = SuppliersDataGrid.SelectedItem as Supplier;
            if (selectedSupplier != null)
            {
                txtSupplierId.Text = selectedSupplier.SupplierId.ToString();
                txtCompanyName.Text = selectedSupplier.CompanyName;
                txtContactPerson.Text = selectedSupplier.ContactPerson;
                txtPhone.Text = selectedSupplier.Phone;
                txtEmail.Text = selectedSupplier.Email;
                txtAddress.Text = selectedSupplier.Address;
                txtCity.Text = selectedSupplier.City;
                txtCountry.Text = selectedSupplier.Country;
                txtPaymentTerms.Text = selectedSupplier.PaymentTerms;
                txtRating.Text = selectedSupplier.Rating?.ToString();
                chkIsActive.IsChecked = selectedSupplier.IsActive;
                txtNotes.Text = selectedSupplier.Notes;

                // Enable update and delete buttons, disable add button.
                btnAdd.IsEnabled = false;
                btnUpdate.IsEnabled = true;
                btnDelete.IsEnabled = true;
            }
            else
            {
                ClearForm();
            }
        }

        /// <summary>
        /// Handles the "Clear" button click and resets the form.
        /// </summary>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        /// <summary>
        /// Resets all input fields and button states.
        /// </summary>
        private void ClearForm()
        {
            txtSupplierId.Clear();
            txtCompanyName.Clear();
            txtContactPerson.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
            txtCity.Clear();
            txtCountry.Clear();
            txtPaymentTerms.Clear();
            txtRating.Clear();
            chkIsActive.IsChecked = false;
            txtNotes.Clear();

            SuppliersDataGrid.UnselectAll();

            btnAdd.IsEnabled = true;
            btnUpdate.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        /// <summary>
        /// Helper method to add parameters to the SQL command to prevent SQL injection.
        /// </summary>
        private void AddParameters(SqlCommand command)
        {
            command.Parameters.AddWithValue("@CompanyName", txtCompanyName.Text);
            command.Parameters.AddWithValue("@ContactPerson", string.IsNullOrEmpty(txtContactPerson.Text) ? (object)DBNull.Value : txtContactPerson.Text);
            command.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(txtPhone.Text) ? (object)DBNull.Value : txtPhone.Text);
            command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text);
            command.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(txtAddress.Text) ? (object)DBNull.Value : txtAddress.Text);
            command.Parameters.AddWithValue("@City", string.IsNullOrEmpty(txtCity.Text) ? (object)DBNull.Value : txtCity.Text);
            command.Parameters.AddWithValue("@Country", string.IsNullOrEmpty(txtCountry.Text) ? (object)DBNull.Value : txtCountry.Text);
            command.Parameters.AddWithValue("@PaymentTerms", string.IsNullOrEmpty(txtPaymentTerms.Text) ? (object)DBNull.Value : txtPaymentTerms.Text);

            if (int.TryParse(txtRating.Text, out int rating))
            {
                command.Parameters.AddWithValue("@Rating", rating);
            }
            else
            {
                command.Parameters.AddWithValue("@Rating", DBNull.Value);
            }

            command.Parameters.AddWithValue("@IsActive", chkIsActive.IsChecked ?? false);
            command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(txtNotes.Text) ? (object)DBNull.Value : txtNotes.Text);
        }
    }
}
