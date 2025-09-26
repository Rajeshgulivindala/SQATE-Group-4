using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HospitalManagementSystem.Views.UserControls
{
    // Simple data model for a purchase order
   
 

    public class PurchaseApprovalsViewModel : INotifyPropertyChanged
    {
        // Connection string for the database
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        // Observable collection to hold the submitted purchase orders.
        // The UI will automatically update when this collection changes.
        public ObservableCollection<PurchaseOrder> SubmittedPurchaseOrders { get; set; }

        // Commands for the approval and rejection buttons
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }

        public PurchaseApprovalsViewModel()
        {
            SubmittedPurchaseOrders = new ObservableCollection<PurchaseOrder>();
            ApproveCommand = new RelayCommand<PurchaseOrder>(ApproveOrder);
            RejectCommand = new RelayCommand<PurchaseOrder>(RejectOrder);
            LoadSubmittedOrders();
        }

        // Method to load orders with "Submitted" status from the database
        public async void LoadSubmittedOrders()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // The SQL query now specifically selects orders where the Status is 'Submitted'.
                    string sqlQuery = "SELECT OrderID, SupplierID, Description, TotalAmount, Status FROM PurchaseOrders WHERE Status = 'Submitted'";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            SubmittedPurchaseOrders.Clear();
                            while (await reader.ReadAsync())
                            {
                                var order = new PurchaseOrder
                                {
                                    OrderId = reader.GetInt32(0),
                                    SupplierId = reader.GetInt32(1),
                                    Description = reader.GetString(2),
                                    TotalAmount = reader.GetDecimal(3),
                                    Status = reader.GetString(4)
                                };
                                SubmittedPurchaseOrders.Add(order);
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
                MessageBox.Show($"Failed to load orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Method to approve a purchase order and update its status in the database
        private async void ApproveOrder(PurchaseOrder order)
        {
            if (order == null) return;
            await UpdateOrderStatus(order.OrderId, "Approved");
            // Remove the order from the list since it's no longer 'Submitted'
            SubmittedPurchaseOrders.Remove(order);
            MessageBox.Show($"Purchase Order {order.OrderId} has been approved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Method to reject a purchase order and update its status in the database
        private async void RejectOrder(PurchaseOrder order)
        {
            if (order == null) return;
            await UpdateOrderStatus(order.OrderId, "Rejected");
            // Remove the order from the list since it's no longer 'Submitted'
            SubmittedPurchaseOrders.Remove(order);
            MessageBox.Show($"Purchase Order {order.OrderId} has been rejected.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Helper method to update the status of an order in the database
        private async Task UpdateOrderStatus(int orderId, string newStatus)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "UPDATE PurchaseOrders SET Status = @Status WHERE OrderID = @OrderID";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Status", newStatus);
                        command.Parameters.AddWithValue("@OrderID", orderId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update order status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Standard INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // A simple RelayCommand to enable command binding in the XAML
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;
        public void Execute(object parameter) => _execute((T)parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
