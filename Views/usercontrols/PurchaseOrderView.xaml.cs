using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Models; // reuse your PurchaseOrder model

namespace HospitalManagementSystem.Views.UserControls
{
    public sealed class SupplierOption
    {
        public int SupplierId { get; set; }
        public string Display { get; set; }
    }

    public class PurchaseOrdersViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public ObservableCollection<PurchaseOrder> PurchaseOrders { get; } = new ObservableCollection<PurchaseOrder>();
        public ObservableCollection<SupplierOption> SupplierOptions { get; } = new ObservableCollection<SupplierOption>();

        private PurchaseOrder _selectedPurchaseOrder;
        public PurchaseOrder SelectedPurchaseOrder
        {
            get => _selectedPurchaseOrder;
            set { _selectedPurchaseOrder = value; PopulateForm(); OnPropertyChanged(); }
        }

        private int? _selectedSupplierId;
        public int? SelectedSupplierId
        {
            get => _selectedSupplierId;
            set { _selectedSupplierId = value; OnPropertyChanged(); }
        }

        public string OrderIdText { get; set; }
        public DateTime? OrderDate { get; set; }
        public string TotalAmountText { get; set; }
        public string DescriptionText { get; set; }
        public string Status { get; set; } = "Draft";
        public int NextOrderId { get; private set; }

        public async Task InitAsync()
        {
            await LoadSuppliersAsync();          // fill the dropdown
            await LoadPurchaseOrdersAsync();     // fill the grid
            await ComputeNextOrderIdAsync();
        }

        // ---------- DB helpers ----------
        private static async Task<bool> TableExistsAsync(SqlConnection con, string tableName)
        {
            using (var cmd = new SqlCommand("SELECT 1 FROM sys.objects WHERE type='U' AND name=@t", con))
            {
                cmd.Parameters.AddWithValue("@t", tableName);
                var res = await cmd.ExecuteScalarAsync();
                return res != null;
            }
        }

        private static async Task<string> FirstExistingColumnAsync(SqlConnection con, string table, params string[] candidates)
        {
            using (var cmd = new SqlCommand(@"
SELECT TOP(1) c.name
FROM sys.columns c
JOIN sys.objects o ON o.object_id = c.object_id
WHERE o.type='U' AND o.name=@t AND (" +
"c.name=@c0 OR c.name=@c1 OR c.name=@c2 OR c.name=@c3)", con))
            {
                cmd.Parameters.AddWithValue("@t", table);
                // pad to 4 params to keep the above SQL simple
                for (int i = 0; i < 4; i++)
                {
                    var val = i < candidates.Length ? (object)candidates[i] : DBNull.Value;
                    cmd.Parameters.AddWithValue("@c" + i, val);
                }
                var v = await cmd.ExecuteScalarAsync();
                return v == null || v == DBNull.Value ? null : v.ToString();
            }
        }
        // --------------------------------

        // ---------- Suppliers (robust + fallbacks) ----------
        private async Task LoadSuppliersAsync()
        {
            SupplierOptions.Clear();
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    // try plural then singular
                    string suppliersTable = null;
                    if (await TableExistsAsync(con, "Suppliers")) suppliersTable = "Suppliers";
                    else if (await TableExistsAsync(con, "Supplier")) suppliersTable = "Supplier";

                    if (suppliersTable != null)
                    {
                        // Detect ID and display columns
                        var idCol = await FirstExistingColumnAsync(con, suppliersTable, "SupplierID", "SupplierId");
                        if (string.IsNullOrEmpty(idCol)) idCol = "SupplierID"; // best guess

                        var nameCol = await FirstExistingColumnAsync(con, suppliersTable, "SupplierName", "Name", "CompanyName");
                        string sql;
                        if (!string.IsNullOrEmpty(nameCol))
                        {
                            sql = $"SELECT [{idCol}] AS Id, CAST([{nameCol}] AS nvarchar(4000)) AS Display FROM dbo.[{suppliersTable}] ORDER BY Display";
                        }
                        else
                        {
                            sql = $"SELECT [{idCol}] AS Id, 'Supplier ' + CAST([{idCol}] AS nvarchar(32)) AS Display FROM dbo.[{suppliersTable}] ORDER BY Id";
                        }

                        using (var cmd = new SqlCommand(sql, con))
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                SupplierOptions.Add(new SupplierOption
                                {
                                    SupplierId = Convert.ToInt32(r["Id"]),
                                    Display = r["Display"]?.ToString()
                                });
                            }
                        }
                    }

                    // Fallback: derive from PurchaseOrders
                    if (SupplierOptions.Count == 0)
                    {
                        // detect SupplierId column name in PurchaseOrders
                        var poIdCol = await FirstExistingColumnAsync(con, "PurchaseOrders", "SupplierId", "SupplierID") ?? "SupplierId";
                        var sql = $"SELECT DISTINCT [{poIdCol}] AS Id FROM dbo.PurchaseOrders WHERE [{poIdCol}] IS NOT NULL ORDER BY Id";

                        using (var cmd = new SqlCommand(sql, con))
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                var id = Convert.ToInt32(r["Id"]);
                                SupplierOptions.Add(new SupplierOption { SupplierId = id, Display = "Supplier " + id });
                            }
                        }
                    }
                }

                // optional: select first supplier by default if none selected
                if (SupplierOptions.Count > 0 && !SelectedSupplierId.HasValue)
                {
                    SelectedSupplierId = SupplierOptions[0].SupplierId;
                    OnPropertyChanged(nameof(SelectedSupplierId));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load suppliers.\n\n" + ex.Message, "Suppliers",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        // -----------------------------------------------------

        public async Task LoadPurchaseOrdersAsync()
        {
            PurchaseOrders.Clear();
            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(
                    "SELECT OrderId, SupplierId, OrderDate, TotalAmount, Description, Status, CreatedDate, UpdatedDate FROM dbo.PurchaseOrders ORDER BY OrderId", con))
                {
                    await con.OpenAsync();
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            PurchaseOrders.Add(new PurchaseOrder
                            {
                                OrderId = r.GetInt32(0),
                                SupplierId = r.GetInt32(1),
                                OrderDate = r.GetDateTime(2),
                                TotalAmount = r.GetDecimal(3),
                                Description = r.IsDBNull(4) ? null : r.GetString(4),
                                Status = r.GetString(5),
                                CreatedDate = r.GetDateTime(6),
                                UpdatedDate = r.GetDateTime(7)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load purchase orders: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ComputeNextOrderIdAsync()
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT ISNULL(MAX(OrderId), 0) + 1 FROM dbo.PurchaseOrders", con))
                {
                    await con.OpenAsync();
                    NextOrderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    OnPropertyChanged(nameof(NextOrderId));
                }
            }
            catch { /* non-fatal */ }
        }

        public async Task AddPurchaseOrderAsync()
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
INSERT INTO dbo.PurchaseOrders
    (SupplierId, OrderDate, TotalAmount, Description, Status, CreatedDate, UpdatedDate)
VALUES
    (@SupplierId, @OrderDate, @TotalAmount, @Description, @Status, GETDATE(), GETDATE());", con))
                {
                    if (!TryAddParameters(cmd, out string err))
                    {
                        MessageBox.Show(err, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }

                MessageBox.Show("Purchase order added successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadPurchaseOrdersAsync();
                await ComputeNextOrderIdAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add purchase order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task UpdatePurchaseOrderAsync()
        {
            if (SelectedPurchaseOrder == null) return;

            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(@"
UPDATE dbo.PurchaseOrders SET
    SupplierId = @SupplierId,
    OrderDate  = @OrderDate,
    TotalAmount= @TotalAmount,
    Description= @Description,
    Status     = @Status,
    UpdatedDate= GETDATE()
WHERE OrderId = @OrderId;", con))
                {
                    if (!TryAddParameters(cmd, out string err))
                    {
                        MessageBox.Show(err, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    cmd.Parameters.AddWithValue("@OrderId", SelectedPurchaseOrder.OrderId);

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }

                MessageBox.Show("Purchase order updated successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadPurchaseOrdersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update purchase order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeletePurchaseOrderAsync()
        {
            if (SelectedPurchaseOrder == null) return;

            if (MessageBox.Show("Delete this purchase order?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("DELETE FROM dbo.PurchaseOrders WHERE OrderId=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", SelectedPurchaseOrder.OrderId);
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }

                await LoadPurchaseOrdersAsync();
                await ComputeNextOrderIdAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete purchase order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task SubmitForApprovalAsync()
        {
            if (SelectedPurchaseOrder == null)
            {
                MessageBox.Show("Select an order first.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (SelectedPurchaseOrder.Status == "Submitted" || SelectedPurchaseOrder.Status == "Approved")
            {
                MessageBox.Show("This order is already submitted/approved.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await ChangeStatusAsync("Submitted");
        }

        public Task ApproveAsync() => SelectedPurchaseOrder == null ? Task.CompletedTask : ChangeStatusAsync("Approved");
        public Task RejectAsync() => SelectedPurchaseOrder == null ? Task.CompletedTask : ChangeStatusAsync("Rejected");

        private async Task ChangeStatusAsync(string newStatus)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(
                    "UPDATE dbo.PurchaseOrders SET Status=@s, UpdatedDate=GETDATE() WHERE OrderId=@id", con))
                {
                    cmd.Parameters.AddWithValue("@s", newStatus);
                    cmd.Parameters.AddWithValue("@id", SelectedPurchaseOrder.OrderId);
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }

                await LoadPurchaseOrdersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to change status: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateForm()
        {
            if (SelectedPurchaseOrder != null)
            {
                OrderIdText = SelectedPurchaseOrder.OrderId.ToString();
                SelectedSupplierId = SelectedPurchaseOrder.SupplierId;
                OrderDate = SelectedPurchaseOrder.OrderDate;
                TotalAmountText = SelectedPurchaseOrder.TotalAmount.ToString(CultureInfo.InvariantCulture);
                DescriptionText = SelectedPurchaseOrder.Description;
                Status = SelectedPurchaseOrder.Status;
            }
            else
            {
                ClearForm();
            }

            OnPropertyChanged(nameof(OrderIdText));
            OnPropertyChanged(nameof(SelectedSupplierId));
            OnPropertyChanged(nameof(OrderDate));
            OnPropertyChanged(nameof(TotalAmountText));
            OnPropertyChanged(nameof(DescriptionText));
            OnPropertyChanged(nameof(Status));
        }

        public void ClearForm()
        {
            OrderIdText = string.Empty;
            SelectedSupplierId = null;
            OrderDate = null;
            TotalAmountText = string.Empty;
            DescriptionText = string.Empty;
            Status = "Draft";
            OnPropertyChanged(string.Empty);
        }

        private bool TryAddParameters(SqlCommand command, out string error)
        {
            error = null;

            if (!SelectedSupplierId.HasValue)
            {
                error = "Please select a Supplier from the dropdown.";
                return false;
            }
            command.Parameters.Add("@SupplierId", SqlDbType.Int).Value = SelectedSupplierId.Value;

            if (!OrderDate.HasValue)
            {
                error = "Order Date is required.";
                return false;
            }
            command.Parameters.Add("@OrderDate", SqlDbType.Date).Value = OrderDate.Value.Date;

            if (!decimal.TryParse(TotalAmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount < 0)
            {
                error = "Enter a valid positive Total Amount (e.g., 1499.99).";
                return false;
            }
            command.Parameters.Add("@TotalAmount", SqlDbType.Money).Value = amount;

            command.Parameters.Add("@Description", SqlDbType.NVarChar, 500)
                   .Value = string.IsNullOrWhiteSpace(DescriptionText) ? (object)DBNull.Value : DescriptionText.Trim();

            command.Parameters.Add("@Status", SqlDbType.NVarChar, 30)
                   .Value = string.IsNullOrWhiteSpace(Status) ? "Draft" : Status;

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class PurchaseOrdersView : UserControl
    {
        private readonly PurchaseOrdersViewModel _vm;

        public PurchaseOrdersView()
        {
            InitializeComponent();
            _vm = new PurchaseOrdersViewModel();
            DataContext = _vm;
            Loaded += PurchaseOrdersView_Loaded;
        }

        private async void PurchaseOrdersView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.InitAsync();
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e) => await _vm.AddPurchaseOrderAsync();
        private async void btnUpdate_Click(object sender, RoutedEventArgs e) => await _vm.UpdatePurchaseOrderAsync();
        private async void btnDelete_Click(object sender, RoutedEventArgs e) => await _vm.DeletePurchaseOrderAsync();
        private async void btnSubmitForApproval_Click(object sender, RoutedEventArgs e) => await _vm.SubmitForApprovalAsync();
        private async void btnApprove_Click(object sender, RoutedEventArgs e) => await _vm.ApproveAsync();
        private async void btnReject_Click(object sender, RoutedEventArgs e) => await _vm.RejectAsync();

        private void PurchaseOrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _vm.SelectedPurchaseOrder = PurchaseOrdersDataGrid.SelectedItem as PurchaseOrder;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            _vm.ClearForm();
            PurchaseOrdersDataGrid.UnselectAll();
        }
    }
}
