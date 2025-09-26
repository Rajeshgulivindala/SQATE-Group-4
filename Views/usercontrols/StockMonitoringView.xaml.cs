using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Represents a stock item data model.
    /// The 'ItemId' property is automatically numbered by the database upon insertion.
    /// </summary>
    public class StockItem
    {
        public int ItemId { get; set; }
        public int? SupplierId { get; set; }
        public string ItemCode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumLevel { get; set; }
        public int MaximumLevel { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    /// <summary>
    /// Interaction logic for StockMonitoringView.xaml
    /// </summary>
    public partial class StockMonitoringView : UserControl
    {
        // TODO: **CRITICAL: Replace this with your actual database connection string.**
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        // This is a collection to hold stock data and automatically update the UI.
        private ObservableCollection<StockItem> stockItems = new ObservableCollection<StockItem>();

        public StockMonitoringView()
        {
            InitializeComponent();
            StockDataGrid.ItemsSource = stockItems;
            this.Loaded += StockMonitoringView_Loaded;
            // Populate ComboBox with initial values
            cmbStatus.SelectedIndex = 0;
        }

        /// <summary>
        /// Loads data when the view is loaded.
        /// </summary>
        private async void StockMonitoringView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStockItemsAsync();
        }

        /// <summary>
        /// Loads all stock items from the database and populates the data grid.
        /// </summary>
        private async Task LoadStockItemsAsync()
        {
            stockItems.Clear();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sqlQuery = "SELECT ItemId, SupplierId, ItemCode, Name, Category, Description, CurrentStock, MinimumLevel, MaximumLevel, UnitPrice, ExpiryDate, Location, Status, CreatedDate, UpdatedDate FROM Inventories";
                    using (var command = new SqlCommand(sqlQuery, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stockItems.Add(new StockItem
                                {
                                    ItemId = reader.GetInt32(0),
                                    SupplierId = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1),
                                    ItemCode = reader.GetString(2),
                                    Name = reader.GetString(3),
                                    Category = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    CurrentStock = reader.GetInt32(6),
                                    MinimumLevel = reader.GetInt32(7),
                                    MaximumLevel = reader.GetInt32(8),
                                    UnitPrice = reader.GetDecimal(9),
                                    ExpiryDate = reader.IsDBNull(10) ? null : (DateTime?)reader.GetDateTime(10),
                                    Location = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    Status = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    CreatedDate = reader.GetDateTime(13),
                                    UpdatedDate = reader.GetDateTime(14)
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
                MessageBox.Show($"Failed to load stock items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Add" button click event.
        /// The ItemId is automatically generated by the database, so it is not included in the insert statement.
        /// </summary>
        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"INSERT INTO Inventories (SupplierId, ItemCode, Name, Category, Description, CurrentStock, MinimumLevel, MaximumLevel, UnitPrice, ExpiryDate, Location, Status, CreatedDate, UpdatedDate)
                                   VALUES (@SupplierId, @ItemCode, @Name, @Category, @Description, @CurrentStock, @MinimumLevel, @MaximumLevel, @UnitPrice, @ExpiryDate, @Location, @Status, GETDATE(), GETDATE());";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Stock item added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadStockItemsAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add stock item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Update" button click event.
        /// </summary>
        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = StockDataGrid.SelectedItem as StockItem;
            if (selectedItem == null) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"UPDATE Inventories SET SupplierId = @SupplierId, ItemCode = @ItemCode, Name = @Name, Category = @Category, Description = @Description, CurrentStock = @CurrentStock, MinimumLevel = @MinimumLevel, MaximumLevel = @MaximumLevel, UnitPrice = @UnitPrice, ExpiryDate = @ExpiryDate, Location = @Location, Status = @Status, UpdatedDate = GETDATE()
                                   WHERE ItemId = @ItemId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        command.Parameters.AddWithValue("@ItemId", selectedItem.ItemId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Stock item updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadStockItemsAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update stock item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Delete" button click event.
        /// </summary>
        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = StockDataGrid.SelectedItem as StockItem;
            if (selectedItem == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {selectedItem.Name}?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "DELETE FROM Inventories WHERE ItemId = @ItemId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ItemId", selectedItem.ItemId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Stock item deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadStockItemsAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete stock item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the selection change in the DataGrid to populate the form for editing.
        /// </summary>
        private void StockDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = StockDataGrid.SelectedItem as StockItem;
            if (selectedItem != null)
            {
                txtItemId.Text = selectedItem.ItemId.ToString();
                txtSupplierId.Text = selectedItem.SupplierId?.ToString();
                txtItemCode.Text = selectedItem.ItemCode;
                txtName.Text = selectedItem.Name;
                txtCategory.Text = selectedItem.Category;
                txtDescription.Text = selectedItem.Description;
                txtCurrentStock.Text = selectedItem.CurrentStock.ToString();
                txtMinimumLevel.Text = selectedItem.MinimumLevel.ToString();
                txtMaximumLevel.Text = selectedItem.MaximumLevel.ToString();
                txtUnitPrice.Text = selectedItem.UnitPrice.ToString();
                dpExpiryDate.SelectedDate = selectedItem.ExpiryDate;
                txtLocation.Text = selectedItem.Location;
                cmbStatus.SelectedItem = cmbStatus.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedItem.Status);

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
            txtItemId.Clear();
            txtSupplierId.Clear();
            txtItemCode.Clear();
            txtName.Clear();
            txtCategory.Clear();
            txtDescription.Clear();
            txtCurrentStock.Clear();
            txtMinimumLevel.Clear();
            txtMaximumLevel.Clear();
            txtUnitPrice.Clear();
            dpExpiryDate.SelectedDate = null;
            txtLocation.Clear();
            cmbStatus.SelectedIndex = 0;

            StockDataGrid.UnselectAll();

            btnAdd.IsEnabled = true;
            btnUpdate.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        /// <summary>
        /// Helper method to add parameters to the SQL command to prevent SQL injection.
        /// </summary>
        private void AddParameters(SqlCommand command)
        {
            if (int.TryParse(txtSupplierId.Text, out int supplierId))
            {
                command.Parameters.AddWithValue("@SupplierId", supplierId);
            }
            else
            {
                command.Parameters.AddWithValue("@SupplierId", DBNull.Value);
            }

            command.Parameters.AddWithValue("@ItemCode", txtItemCode.Text);
            command.Parameters.AddWithValue("@Name", txtName.Text);
            command.Parameters.AddWithValue("@Category", string.IsNullOrEmpty(txtCategory.Text) ? (object)DBNull.Value : txtCategory.Text);
            command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(txtDescription.Text) ? (object)DBNull.Value : txtDescription.Text);

            if (int.TryParse(txtCurrentStock.Text, out int currentStock))
            {
                command.Parameters.AddWithValue("@CurrentStock", currentStock);
            }
            else
            {
                command.Parameters.AddWithValue("@CurrentStock", 0);
            }

            if (int.TryParse(txtMinimumLevel.Text, out int minLevel))
            {
                command.Parameters.AddWithValue("@MinimumLevel", minLevel);
            }
            else
            {
                command.Parameters.AddWithValue("@MinimumLevel", 0);
            }

            if (int.TryParse(txtMaximumLevel.Text, out int maxLevel))
            {
                command.Parameters.AddWithValue("@MaximumLevel", maxLevel);
            }
            else
            {
                command.Parameters.AddWithValue("@MaximumLevel", 0);
            }

            if (decimal.TryParse(txtUnitPrice.Text, out decimal unitPrice))
            {
                command.Parameters.AddWithValue("@UnitPrice", unitPrice);
            }
            else
            {
                command.Parameters.AddWithValue("@UnitPrice", 0);
            }

            command.Parameters.AddWithValue("@ExpiryDate", dpExpiryDate.SelectedDate.HasValue ? (object)dpExpiryDate.SelectedDate.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Location", string.IsNullOrEmpty(txtLocation.Text) ? (object)DBNull.Value : txtLocation.Text);
            command.Parameters.AddWithValue("@Status", cmbStatus.SelectedItem != null ? ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString() : (object)DBNull.Value);
        }
    }
}
