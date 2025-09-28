using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows; // <-- Essential for DataContext and MessageBox
using System.Windows.Controls; // <-- Essential for UserControl base class

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Data Model for a single item (Service/Product) in the bill.
    /// Implements INotifyPropertyChanged to update the DataGrid and TotalAmount when values change.
    /// </summary>
    public class BillItem : INotifyPropertyChanged
    {
        private string _name;
        private int _quantity;
        private decimal _unitPrice;

        public event PropertyChangedEventHandler PropertyChanged;

        // Helper method to raise the PropertyChanged event
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                // Ensure quantity is not negative
                _quantity = Math.Max(0, value);
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(SubTotal)); // Recalculate SubTotal
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                // Ensure price is not negative
                _unitPrice = Math.Max(0, value);
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(SubTotal)); // Recalculate SubTotal
            }
        }

        /// <summary>
        /// Calculated property for the line item subtotal (Quantity * UnitPrice).
        /// This is read-only in the DataGrid.
        /// </summary>
        public decimal SubTotal => Quantity * UnitPrice;
    }

    /// <summary>
    /// Interaction logic for BillingManagementView.xaml (Code-Behind)
    /// Inherits from UserControl to allow embedding in the MainWindow.
    /// </summary>
    public partial class BillingManagementView : UserControl, INotifyPropertyChanged
    {
        // This ObservableCollection is bound to the DataGrid's ItemsSource.
        public ObservableCollection<BillItem> Items { get; set; }

        private decimal _totalAmount;

        // Property to display the running total on the UI, bound to TotalAmount property.
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                _totalAmount = value;
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public BillingManagementView()
        {
            // IMPORTANT: InitializeComponent() is required to load and link the XAML elements.
            InitializeComponent();

            // Setting the DataContext here allows the XAML bindings (like {Binding TotalAmount}) to work.
            this.DataContext = this;

            Items = new ObservableCollection<BillItem>();

            // 1. Listen for items being added/removed from the main collection.
            Items.CollectionChanged += Items_CollectionChanged;

            // Add a default item to start with
            AddItem();
        }

        // Handles additions and removals from the collection (CRUD: Create and Delete)
        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                // 2. For newly added items, subscribe to their individual PropertyChanged event.
                foreach (BillItem item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                // Unsubscribe when items are removed.
                foreach (BillItem item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            // Recalculate total whenever the collection structure changes
            CalculateTotal();
        }

        // Handles property changes within an item (CRUD: Update)
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // We only need to recalculate the total if the SubTotal (which depends on Quantity/UnitPrice) changed.
            if (e.PropertyName == nameof(BillItem.SubTotal))
            {
                CalculateTotal();
            }
        }

        /// <summary>
        /// Calculates the sum of all item subtotals.
        /// </summary>
        private void CalculateTotal()
        {
            TotalAmount = Items.Sum(item => item.SubTotal);
        }

        /// <summary>
        /// Logic to add a new service/item line. (Bound to AddItem_Click button)
        /// </summary>
        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            AddItem();
        }

        private void AddItem()
        {
            Items.Add(new BillItem { Name = "New Service/Procedure", Quantity = 1, UnitPrice = 50.00M });
        }

        /// <summary>
        /// Logic to remove the currently selected service/item line. (Bound to RemoveItem_Click button)
        /// </summary>
        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            // productsDataGrid is automatically available because it was declared with x:Name in XAML
            if (productsDataGrid.SelectedItem is BillItem selectedItem)
            {
                Items.Remove(selectedItem);
            }
            else
            {
                // Instead of alert(), show a message box.
                MessageBox.Show("Please select an item to remove.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Placeholder for final bill generation logic. (Bound to GenerateBill_Click button)
        /// </summary>
        private void GenerateBill_Click(object sender, RoutedEventArgs e)
        {
            // Here you would implement saving the data to a database, printing, or creating a final document.
            MessageBox.Show($"Final bill generated.\nTotal Due: {TotalAmount:C}\nReady for processing.",
                            "Bill Generated",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}