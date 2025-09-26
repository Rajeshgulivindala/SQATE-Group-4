using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Represents a purchase order data model.
    /// This is the "Model" in the MVVM pattern.
    /// </summary>
    
    /// <summary>
    /// The ViewModel that handles all data and logic for the PurchaseOrdersView.
    /// This class is the core of the solution, separating UI from business logic.
    /// </summary>
    public class PurchaseOrdersViewModel : INotifyPropertyChanged
    {
        // The connection string is now private to the ViewModel, so the View cannot access it directly.
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        private ObservableCollection<PurchaseOrder> _purchaseOrders;
        public ObservableCollection<PurchaseOrder> PurchaseOrders
        {
            get => _purchaseOrders;
            set
            {
                _purchaseOrders = value;
                OnPropertyChanged();
            }
        }

        private PurchaseOrder _selectedPurchaseOrder;
        public PurchaseOrder SelectedPurchaseOrder
        {
            get => _selectedPurchaseOrder;
            set
            {
                _selectedPurchaseOrder = value;
                OnPropertyChanged();
                PopulateForm();
            }
        }

        // Properties that the UI will bind to.
        public string OrderIdText { get; set; }
        public string SupplierIdText { get; set; }
        public DateTime? OrderDate { get; set; }
        public string TotalAmountText { get; set; }
        public string DescriptionText { get; set; }
        public string Status { get; set; }

        public PurchaseOrdersViewModel()
        {
            PurchaseOrders = new ObservableCollection<PurchaseOrder>();
            ClearForm();
        }

        /// <summary>
        /// Loads all purchase orders from the database.
        /// </summary>
        public async Task LoadPurchaseOrdersAsync()
        {
            PurchaseOrders.Clear();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sqlQuery = "SELECT OrderId, SupplierId, OrderDate, TotalAmount, Description, Status, CreatedDate, UpdatedDate FROM PurchaseOrders";
                    using (var command = new SqlCommand(sqlQuery, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                PurchaseOrders.Add(new PurchaseOrder
                                {
                                    OrderId = reader.GetInt32(0),
                                    SupplierId = reader.GetInt32(1),
                                    OrderDate = reader.GetDateTime(2),
                                    TotalAmount = reader.GetDecimal(3),
                                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Status = reader.GetString(5),
                                    CreatedDate = reader.GetDateTime(6),
                                    UpdatedDate = reader.GetDateTime(7)
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
                MessageBox.Show($"Failed to load purchase orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles adding a new purchase order.
        /// </summary>
        public async Task AddPurchaseOrderAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"INSERT INTO PurchaseOrders (SupplierId, OrderDate, TotalAmount, Description, Status, CreatedDate, UpdatedDate)
                                   VALUES (@SupplierId, @OrderDate, @TotalAmount, @Description, @Status, GETDATE(), GETDATE());";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Purchase order added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadPurchaseOrdersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add purchase order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles updating an existing purchase order.
        /// </summary>
        public async Task UpdatePurchaseOrderAsync()
        {
            if (SelectedPurchaseOrder == null) return;
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"UPDATE PurchaseOrders SET SupplierId = @SupplierId, OrderDate = @OrderDate, TotalAmount = @TotalAmount, Description = @Description, Status = @Status, UpdatedDate = GETDATE()
                                   WHERE OrderId = @OrderId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        command.Parameters.AddWithValue("@OrderId", SelectedPurchaseOrder.OrderId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Purchase order updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadPurchaseOrdersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update purchase order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles deleting a purchase order.
        /// </summary>
        public async Task DeletePurchaseOrderAsync()
        {
            if (SelectedPurchaseOrder == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete this purchase order?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "DELETE FROM PurchaseOrders WHERE OrderId = @OrderId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@OrderId", SelectedPurchaseOrder.OrderId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Purchase order deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadPurchaseOrdersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete purchase order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Submit for Approval" action.
        /// </summary>
        public async Task SubmitForApprovalAsync()
        {
            if (SelectedPurchaseOrder == null)
            {
                MessageBox.Show("Please select a purchase order to submit for approval.", "No Order Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedPurchaseOrder.Status == "Submitted" || SelectedPurchaseOrder.Status == "Approved")
            {
                MessageBox.Show("This purchase order has already been submitted or approved.", "Status Unchanged", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await ChangeStatusAsync("Submitted");
        }

        /// <summary>
        /// Handles the "Approve" action.
        /// </summary>
        public async Task ApproveAsync()
        {
            if (SelectedPurchaseOrder == null) return;
            await ChangeStatusAsync("Approved");
        }

        /// <summary>
        /// Handles the "Reject" action.
        /// </summary>
        public async Task RejectAsync()
        {
            if (SelectedPurchaseOrder == null) return;
            await ChangeStatusAsync("Rejected");
        }

        /// <summary>
        /// Helper method to change the status of the selected order.
        /// </summary>
        private async Task ChangeStatusAsync(string newStatus)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"UPDATE PurchaseOrders SET Status = @Status, UpdatedDate = GETDATE() WHERE OrderId = @OrderId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Status", newStatus);
                        command.Parameters.AddWithValue("@OrderId", SelectedPurchaseOrder.OrderId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show($"Purchase order status changed to '{newStatus}' successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadPurchaseOrdersAsync();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to change status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Populates the form fields based on the selected item.
        /// This is called automatically when the `SelectedPurchaseOrder` changes.
        /// </summary>
        private void PopulateForm()
        {
            if (SelectedPurchaseOrder != null)
            {
                OrderIdText = SelectedPurchaseOrder.OrderId.ToString();
                SupplierIdText = SelectedPurchaseOrder.SupplierId.ToString();
                OrderDate = SelectedPurchaseOrder.OrderDate;
                TotalAmountText = SelectedPurchaseOrder.TotalAmount.ToString();
                DescriptionText = SelectedPurchaseOrder.Description;
                Status = SelectedPurchaseOrder.Status;
            }
            else
            {
                ClearForm();
            }
            OnPropertyChanged("OrderIdText");
            OnPropertyChanged("SupplierIdText");
            OnPropertyChanged("OrderDate");
            OnPropertyChanged("TotalAmountText");
            OnPropertyChanged("DescriptionText");
            OnPropertyChanged("Status");
        }

        /// <summary>
        /// Resets all input fields.
        /// </summary>
        public void ClearForm()
        {
            OrderIdText = string.Empty;
            SupplierIdText = string.Empty;
            OrderDate = null;
            TotalAmountText = string.Empty;
            DescriptionText = string.Empty;
            Status = "Draft"; // Set initial status
            OnPropertyChanged(string.Empty); // Notify all properties changed
        }

        /// <summary>
        /// Helper method to add parameters to the SQL command.
        /// </summary>
        private void AddParameters(SqlCommand command)
        {
            if (int.TryParse(SupplierIdText, out int supplierId))
            {
                command.Parameters.AddWithValue("@SupplierId", supplierId);
            }
            else
            {
                command.Parameters.AddWithValue("@SupplierId", DBNull.Value);
            }

            if (OrderDate.HasValue)
            {
                command.Parameters.AddWithValue("@OrderDate", OrderDate.Value);
            }
            else
            {
                command.Parameters.AddWithValue("@OrderDate", DBNull.Value);
            }

            if (decimal.TryParse(TotalAmountText, out decimal totalAmount))
            {
                command.Parameters.AddWithValue("@TotalAmount", totalAmount);
            }
            else
            {
                command.Parameters.AddWithValue("@TotalAmount", 0);
            }

            command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(DescriptionText) ? (object)DBNull.Value : DescriptionText);
            command.Parameters.AddWithValue("@Status", Status);
        }

        // Boilerplate code for INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Interaction logic for PurchaseOrdersView.xaml
    /// This is the "View" in the MVVM pattern.
    /// It now only interacts with the ViewModel.
    /// </summary>
    public partial class PurchaseOrdersView : UserControl
    {
        private PurchaseOrdersViewModel _viewModel;

        public PurchaseOrdersView()
        {
            InitializeComponent();
            _viewModel = new PurchaseOrdersViewModel();
            this.DataContext = _viewModel; // Set the DataContext to the ViewModel
            this.Loaded += PurchaseOrdersView_Loaded;
        }

        /// <summary>
        /// Loads data when the view is loaded.
        /// The 'await' keyword is now correctly used here.
        /// </summary>
        private async void PurchaseOrdersView_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadPurchaseOrdersAsync();
        }

        /// <summary>
        /// Handles the "Add" button click event.
        /// </summary>
        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.AddPurchaseOrderAsync();
        }

        /// <summary>
        /// Handles the "Update" button click event.
        /// </summary>
        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.UpdatePurchaseOrderAsync();
        }

        /// <summary>
        /// Handles the "Delete" button click event.
        /// </summary>
        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.DeletePurchaseOrderAsync();
        }

        /// <summary>
        /// Handles the "Submit for Approval" button click event.
        /// </summary>
        private async void btnSubmitForApproval_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.SubmitForApprovalAsync();
        }

        /// <summary>
        /// Handles the "Approve" button click event.
        /// </summary>
        private async void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ApproveAsync();
        }

        /// <summary>
        /// Handles the "Reject" button click event.
        /// </summary>
        private async void btnReject_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.RejectAsync();
        }

        /// <summary>
        /// Handles the selection change in the DataGrid.
        /// </summary>
        private void PurchaseOrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.SelectedPurchaseOrder = PurchaseOrdersDataGrid.SelectedItem as PurchaseOrder;

            // The rest of the UI state is managed by the ViewModel.
            btnAdd.IsEnabled = _viewModel.SelectedPurchaseOrder == null;
            btnUpdate.IsEnabled = _viewModel.SelectedPurchaseOrder != null;
            btnDelete.IsEnabled = _viewModel.SelectedPurchaseOrder != null;

            if (_viewModel.SelectedPurchaseOrder != null)
            {
                btnApprove.IsEnabled = _viewModel.SelectedPurchaseOrder.Status == "Submitted";
                btnReject.IsEnabled = _viewModel.SelectedPurchaseOrder.Status == "Submitted";
            }
            else
            {
                btnApprove.IsEnabled = false;
                btnReject.IsEnabled = false;
            }
        }

        /// <summary>
        /// Handles the "Clear" button click and resets the form.
        /// </summary>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearForm();
            PurchaseOrdersDataGrid.UnselectAll();
        }
    }
}
