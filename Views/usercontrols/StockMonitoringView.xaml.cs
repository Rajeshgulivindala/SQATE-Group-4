using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

    public partial class StockMonitoringView : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        private readonly ObservableCollection<StockItem> stockItems = new ObservableCollection<StockItem>();

        // ---- Validation rules / limits ----
        private static readonly Regex ItemCodeRegex = new Regex(@"^[A-Za-z0-9._-]{3,50}$", RegexOptions.Compiled);
        private const int MaxNameLen = 100;
        private const int MaxCategoryLen = 60;
        private const int MaxDescriptionLen = 1000;
        private const int MaxLocationLen = 100;
        private const int MaxStock = 100000000; // 100M
        private const int MaxLevel = 100000000;
        private const decimal MaxUnitPrice = 1000000000m; // 1B

        public StockMonitoringView()
        {
            InitializeComponent();
            StockDataGrid.ItemsSource = stockItems;
            Loaded += StockMonitoringView_Loaded;
            if (cmbStatus != null && cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = 0;
        }

        private async void StockMonitoringView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStockItemsAsync();
        }

        private async Task LoadStockItemsAsync()
        {
            stockItems.Clear();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sqlQuery = @"
SELECT ItemId, SupplierId, ItemCode, Name, Category, Description, CurrentStock, MinimumLevel,
       MaximumLevel, UnitPrice, ExpiryDate, Location, Status, CreatedDate, UpdatedDate
FROM Inventories
ORDER BY ItemId DESC;";
                    using (var command = new SqlCommand(sqlQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            stockItems.Add(new StockItem
                            {
                                ItemId = reader.GetInt32(0),
                                SupplierId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                ItemCode = reader.GetString(2),
                                Name = reader.GetString(3),
                                Category = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                                CurrentStock = reader.GetInt32(6),
                                MinimumLevel = reader.GetInt32(7),
                                MaximumLevel = reader.GetInt32(8),
                                UnitPrice = reader.GetDecimal(9),
                                ExpiryDate = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                                Location = reader.IsDBNull(11) ? null : reader.GetString(11),
                                Status = reader.IsDBNull(12) ? null : reader.GetString(12),
                                CreatedDate = reader.GetDateTime(13),
                                UpdatedDate = reader.GetDateTime(14)
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load stock items: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ClearValidationHighlights();
            NormalizeInputs();

            string errors;
            if (!ValidateInputs(out errors))
            {
                MessageBox.Show("Please fix the following:\n\n" + errors, "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Supplier must exist if provided
            int supplierIdParsed;
            if (!string.IsNullOrWhiteSpace(txtSupplierId.Text) &&
                int.TryParse(txtSupplierId.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out supplierIdParsed))
            {
                var exists = await SupplierExistsAsync(supplierIdParsed);
                if (exists == false)
                {
                    MarkInvalid(txtSupplierId, "Supplier ID not found.");
                    MessageBox.Show("Supplier ID does not exist.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Unique Item Code guard
            if (await ItemCodeExistsAsync(txtItemCode.Text.Trim(), null))
            {
                MarkInvalid(txtItemCode, "Item Code already exists.");
                MessageBox.Show("Item Code already exists. Please choose another.", "Duplicate",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sql = @"
INSERT INTO Inventories
 (SupplierId, ItemCode, Name, Category, Description, CurrentStock, MinimumLevel, MaximumLevel,
  UnitPrice, ExpiryDate, Location, Status, CreatedDate, UpdatedDate)
VALUES
 (@SupplierId, @ItemCode, @Name, @Category, @Description, @CurrentStock, @MinimumLevel, @MaximumLevel,
  @UnitPrice, @ExpiryDate, @Location, @Status, GETDATE(), GETDATE());";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Stock item added successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadStockItemsAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to add stock item: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = StockDataGrid.SelectedItem as StockItem;
            if (selectedItem == null) return;

            ClearValidationHighlights();
            NormalizeInputs();

            string errors;
            if (!ValidateInputs(out errors))
            {
                MessageBox.Show("Please fix the following:\n\n" + errors, "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Supplier must exist if provided
            int supplierIdParsed;
            if (!string.IsNullOrWhiteSpace(txtSupplierId.Text) &&
                int.TryParse(txtSupplierId.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out supplierIdParsed))
            {
                var exists = await SupplierExistsAsync(supplierIdParsed);
                if (exists == false)
                {
                    MarkInvalid(txtSupplierId, "Supplier ID not found.");
                    MessageBox.Show("Supplier ID does not exist.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Unique Item Code guard (excluding current record)
            if (await ItemCodeExistsAsync(txtItemCode.Text.Trim(), selectedItem.ItemId))
            {
                MarkInvalid(txtItemCode, "Item Code already exists.");
                MessageBox.Show("Item Code already exists. Please choose another.", "Duplicate",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sql = @"
UPDATE Inventories
   SET SupplierId=@SupplierId, ItemCode=@ItemCode, Name=@Name, Category=@Category, Description=@Description,
       CurrentStock=@CurrentStock, MinimumLevel=@MinimumLevel, MaximumLevel=@MaximumLevel,
       UnitPrice=@UnitPrice, ExpiryDate=@ExpiryDate, Location=@Location, Status=@Status,
       UpdatedDate=GETDATE()
 WHERE ItemId=@ItemId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        command.Parameters.AddWithValue("@ItemId", selectedItem.ItemId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Stock item updated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadStockItemsAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update stock item: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = StockDataGrid.SelectedItem as StockItem;
            if (selectedItem == null) return;

            var result = MessageBox.Show(
                "Are you sure you want to delete " + selectedItem.Name + "?",
                "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sql = "DELETE FROM Inventories WHERE ItemId = @ItemId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ItemId", selectedItem.ItemId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Stock item deleted successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadStockItemsAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete stock item: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StockDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearValidationHighlights();
            var selectedItem = StockDataGrid.SelectedItem as StockItem;
            if (selectedItem != null)
            {
                txtItemId.Text = selectedItem.ItemId.ToString(CultureInfo.InvariantCulture);
                txtSupplierId.Text = selectedItem.SupplierId.HasValue
                    ? selectedItem.SupplierId.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                txtItemCode.Text = selectedItem.ItemCode;
                txtName.Text = selectedItem.Name;
                txtCategory.Text = selectedItem.Category;
                txtDescription.Text = selectedItem.Description;
                txtCurrentStock.Text = selectedItem.CurrentStock.ToString(CultureInfo.InvariantCulture);
                txtMinimumLevel.Text = selectedItem.MinimumLevel.ToString(CultureInfo.InvariantCulture);
                txtMaximumLevel.Text = selectedItem.MaximumLevel.ToString(CultureInfo.InvariantCulture);
                txtUnitPrice.Text = selectedItem.UnitPrice.ToString(CultureInfo.InvariantCulture);
                dpExpiryDate.SelectedDate = selectedItem.ExpiryDate;
                txtLocation.Text = selectedItem.Location;

                if (cmbStatus != null)
                {
                    cmbStatus.SelectedItem = cmbStatus.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(item => string.Equals(
                            Convert.ToString(item.Content), selectedItem.Status, StringComparison.OrdinalIgnoreCase));
                }

                btnAdd.IsEnabled = false;
                btnUpdate.IsEnabled = true;
                btnDelete.IsEnabled = true;
            }
            else
            {
                ClearForm();
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            ClearValidationHighlights();

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
            if (cmbStatus != null && cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = 0;

            StockDataGrid.UnselectAll();
            btnAdd.IsEnabled = true;
            btnUpdate.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        private void NormalizeInputs()
        {
            if (txtItemCode != null) txtItemCode.Text = (txtItemCode.Text ?? string.Empty).Trim();
            if (txtName != null) txtName.Text = (txtName.Text ?? string.Empty).Trim();
            if (txtCategory != null) txtCategory.Text = (txtCategory.Text ?? string.Empty).Trim();
            if (txtDescription != null) txtDescription.Text = (txtDescription.Text ?? string.Empty).Trim();
            if (txtLocation != null) txtLocation.Text = (txtLocation.Text ?? string.Empty).Trim();
            if (txtSupplierId != null) txtSupplierId.Text = (txtSupplierId.Text ?? string.Empty).Trim();
        }

        private void AddParameters(SqlCommand command)
        {
            int supplierIdParsed;
            if (int.TryParse(txtSupplierId.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out supplierIdParsed) && supplierIdParsed > 0)
                command.Parameters.AddWithValue("@SupplierId", supplierIdParsed);
            else
                command.Parameters.AddWithValue("@SupplierId", DBNull.Value);

            command.Parameters.AddWithValue("@ItemCode", txtItemCode.Text.Trim());
            command.Parameters.AddWithValue("@Name", txtName.Text.Trim());
            command.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(txtCategory.Text) ? (object)DBNull.Value : txtCategory.Text.Trim());
            command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(txtDescription.Text) ? (object)DBNull.Value : txtDescription.Text.Trim());

            int iVal;
            if (int.TryParse(txtCurrentStock.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out iVal))
                command.Parameters.AddWithValue("@CurrentStock", iVal);
            else
                command.Parameters.AddWithValue("@CurrentStock", 0);

            if (int.TryParse(txtMinimumLevel.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out iVal))
                command.Parameters.AddWithValue("@MinimumLevel", iVal);
            else
                command.Parameters.AddWithValue("@MinimumLevel", 0);

            if (int.TryParse(txtMaximumLevel.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out iVal))
                command.Parameters.AddWithValue("@MaximumLevel", iVal);
            else
                command.Parameters.AddWithValue("@MaximumLevel", 0);

            decimal dVal;
            if (decimal.TryParse(txtUnitPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out dVal))
                command.Parameters.AddWithValue("@UnitPrice", dVal);
            else
                command.Parameters.AddWithValue("@UnitPrice", 0m);

            command.Parameters.AddWithValue("@ExpiryDate", dpExpiryDate.SelectedDate.HasValue ? (object)dpExpiryDate.SelectedDate.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Location", string.IsNullOrWhiteSpace(txtLocation.Text) ? (object)DBNull.Value : txtLocation.Text.Trim());
            command.Parameters.AddWithValue("@Status",
                cmbStatus != null && cmbStatus.SelectedItem is ComboBoxItem
                    ? ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString()
                    : (object)DBNull.Value);
        }

        // ---------------- Validation helpers ----------------
        private bool ValidateInputs(out string summary)
        {
            var sb = new StringBuilder();
            Control firstInvalid = null;

            ClearValidationHighlights();

            int supplierIdParsed;
            if (!string.IsNullOrWhiteSpace(txtSupplierId.Text) &&
                (!int.TryParse(txtSupplierId.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out supplierIdParsed) || supplierIdParsed <= 0))
            {
                Append(sb, "Supplier ID must be a positive integer or left blank.");
                MarkInvalid(txtSupplierId, "Enter a positive integer or leave empty.");
                if (firstInvalid == null) firstInvalid = txtSupplierId;
            }

            var code = (txtItemCode.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                Append(sb, "Item Code is required.");
                MarkInvalid(txtItemCode, "Item Code is required.");
                if (firstInvalid == null) firstInvalid = txtItemCode;
            }
            else if (!ItemCodeRegex.IsMatch(code))
            {
                Append(sb, "Item Code must be 3–50 characters, letters/digits/._- only.");
                MarkInvalid(txtItemCode, "Use letters/digits/._- (3–50).");
                if (firstInvalid == null) firstInvalid = txtItemCode;
            }

            var name = (txtName.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Append(sb, "Name is required.");
                MarkInvalid(txtName, "Name is required.");
                if (firstInvalid == null) firstInvalid = txtName;
            }
            else if (name.Length > MaxNameLen)
            {
                Append(sb, "Name is too long (max " + MaxNameLen + ").");
                MarkInvalid(txtName, "Max " + MaxNameLen + " characters.");
                if (firstInvalid == null) firstInvalid = txtName;
            }

            var category = (txtCategory.Text ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(category) && category.Length > MaxCategoryLen)
            {
                Append(sb, "Category is too long (max " + MaxCategoryLen + ").");
                MarkInvalid(txtCategory, "Max " + MaxCategoryLen + " characters.");
                if (firstInvalid == null) firstInvalid = txtCategory;
            }

            var desc = (txtDescription.Text ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(desc) && desc.Length > MaxDescriptionLen)
            {
                Append(sb, "Description is too long (max " + MaxDescriptionLen + ").");
                MarkInvalid(txtDescription, "Max " + MaxDescriptionLen + " characters.");
                if (firstInvalid == null) firstInvalid = txtDescription;
            }

            int currentStock;
            if (!int.TryParse(txtCurrentStock.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentStock) ||
                currentStock < 0 || currentStock > MaxStock)
            {
                Append(sb, "Current Stock must be 0–" + MaxStock + ".");
                MarkInvalid(txtCurrentStock, "Enter a whole number 0–" + MaxStock + ".");
                if (firstInvalid == null) firstInvalid = txtCurrentStock;
            }

            int minLevel;
            if (!int.TryParse(txtMinimumLevel.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out minLevel) ||
                minLevel < 0 || minLevel > MaxLevel)
            {
                Append(sb, "Minimum Level must be 0–" + MaxLevel + ".");
                MarkInvalid(txtMinimumLevel, "Enter a whole number 0–" + MaxLevel + ".");
                if (firstInvalid == null) firstInvalid = txtMinimumLevel;
            }

            int maxLevel;
            if (!int.TryParse(txtMaximumLevel.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxLevel) ||
                maxLevel < 0 || maxLevel > MaxLevel)
            {
                Append(sb, "Maximum Level must be 0–" + MaxLevel + ".");
                MarkInvalid(txtMaximumLevel, "Enter a whole number 0–" + MaxLevel + ".");
                if (firstInvalid == null) firstInvalid = txtMaximumLevel;
            }

            // Cross-field constraints
            if (sb.Length == 0 && maxLevel < minLevel)
            {
                Append(sb, "Maximum Level cannot be less than Minimum Level.");
                MarkInvalid(txtMaximumLevel, "Must be ≥ Minimum Level.");
                if (firstInvalid == null) firstInvalid = txtMaximumLevel;
            }
            if (sb.Length == 0 && currentStock > maxLevel)
            {
                Append(sb, "Current Stock cannot exceed Maximum Level.");
                MarkInvalid(txtCurrentStock, "Must be ≤ Maximum Level.");
                if (firstInvalid == null) firstInvalid = txtCurrentStock;
            }

            decimal price;
            if (!decimal.TryParse(txtUnitPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out price) ||
                price < 0m || price > MaxUnitPrice)
            {
                Append(sb, "Unit Price must be 0–" + MaxUnitPrice.ToString("0.##", CultureInfo.InvariantCulture) + ".");
                MarkInvalid(txtUnitPrice, "Enter a number 0–" + MaxUnitPrice.ToString("0.##", CultureInfo.InvariantCulture) + ".");
                if (firstInvalid == null) firstInvalid = txtUnitPrice;
            }

            if (dpExpiryDate.SelectedDate.HasValue)
            {
                var exp = dpExpiryDate.SelectedDate.Value.Date;
                if (exp < DateTime.Today)
                {
                    Append(sb, "Expiry Date cannot be in the past.");
                    MarkInvalid(dpExpiryDate, "Pick today or a future date.");
                    if (firstInvalid == null) firstInvalid = dpExpiryDate;
                }
            }

            var loc = (txtLocation.Text ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(loc) && loc.Length > MaxLocationLen)
            {
                Append(sb, "Location is too long (max " + MaxLocationLen + ").");
                MarkInvalid(txtLocation, "Max " + MaxLocationLen + " characters.");
                if (firstInvalid == null) firstInvalid = txtLocation;
            }

            string status = null;
            if (cmbStatus != null && cmbStatus.SelectedItem is ComboBoxItem)
                status = ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString();

            if (string.IsNullOrWhiteSpace(status))
            {
                Append(sb, "Status is required.");
                MarkInvalid(cmbStatus, "Select a status.");
                if (firstInvalid == null) firstInvalid = cmbStatus;
            }

            if (sb.Length > 0)
            {
                if (firstInvalid != null) firstInvalid.Focus();
                summary = sb.ToString();
                return false;
            }

            summary = string.Empty;
            return true;
        }

        private async Task<bool> ItemCodeExistsAsync(string code, int? excludingItemId)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();
                    string sql = "SELECT COUNT(*) FROM Inventories WHERE ItemCode=@c";
                    if (excludingItemId.HasValue) sql += " AND ItemId <> @id";
                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@c", code);
                        if (excludingItemId.HasValue) cmd.Parameters.AddWithValue("@id", excludingItemId.Value);
                        var o = await cmd.ExecuteScalarAsync();
                        int count = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
                        return count > 0;
                    }
                }
            }
            catch
            {
                // On lookup failure we don't block, but you could choose to block if preferred.
                return false;
            }
        }

        /// <summary>
        /// Verifies the Supplier exists (tries 'Suppliers' then 'Supplier' table names).
        /// Returns false if definitely not found; true if found; and true on query errors (fail-open).
        /// </summary>
        private async Task<bool> SupplierExistsAsync(int supplierId)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();
                    // Try Suppliers
                    var ok = await SupplierExistsInternalAsync(cn, "Suppliers", supplierId);
                    if (ok.HasValue) return ok.Value;
                    // Try Supplier
                    ok = await SupplierExistsInternalAsync(cn, "Supplier", supplierId);
                    if (ok.HasValue) return ok.Value;
                }
            }
            catch
            {
                // ignore, treat as unknown/true to not block workflow on metadata issues
            }
            return true; // fail-open
        }

        private static async Task<bool?> SupplierExistsInternalAsync(SqlConnection cn, string table, int id)
        {
            try
            {
                string sql = $"SELECT COUNT(*) FROM {table} WHERE SupplierID = @id";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    var o = await cmd.ExecuteScalarAsync();
                    int count = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
                    return count > 0;
                }
            }
            catch
            {
                return null; // table might not exist
            }
        }

        // ---------------- Tiny UI helpers ----------------
        private void ClearValidationHighlights()
        {
            Control[] ctrls = new Control[]
            {
                txtSupplierId, txtItemCode, txtName, txtCategory, txtDescription,
                txtCurrentStock, txtMinimumLevel, txtMaximumLevel, txtUnitPrice,
                txtLocation, dpExpiryDate, cmbStatus
            };

            foreach (var c in ctrls)
            {
                if (c == null) continue;
                c.ClearValue(Border.BorderBrushProperty);
                c.ClearValue(Border.BorderThicknessProperty);
                c.ToolTip = null;
            }
        }

        private static void MarkInvalid(Control c, string tooltip)
        {
            if (c == null) return;
            c.BorderBrush = Brushes.Red;
            c.BorderThickness = new Thickness(1.5);
            c.ToolTip = tooltip;
        }

        private static void Append(StringBuilder sb, string msg) => sb.AppendLine("• " + msg);
    }
}
