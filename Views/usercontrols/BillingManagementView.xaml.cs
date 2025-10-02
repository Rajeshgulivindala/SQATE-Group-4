using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HospitalManagementSystem.Views.UserControls
{
    public class BillItem : INotifyPropertyChanged, IDataErrorInfo
    {
        public const int MaxNameLength = 100;
        public const int MinQuantity = 1;
        public const int MaxQuantity = 1000;
        public const decimal MinUnitPrice = 0m;
        public const decimal MaxUnitPrice = 1_000_000m;

        private string _name;
        private int _quantity;
        private decimal _unitPrice;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void RefreshValidation()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(UnitPrice));
            OnPropertyChanged(nameof(SubTotal));
        }

        public string Name
        {
            get => _name;
            set { _name = value?.Trim(); OnPropertyChanged(nameof(Name)); OnPropertyChanged(nameof(SubTotal)); }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value < MinQuantity) value = MinQuantity;
                if (value > MaxQuantity) value = MaxQuantity;
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(SubTotal));
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (value < MinUnitPrice) value = MinUnitPrice;
                if (value > MaxUnitPrice) value = MaxUnitPrice;
                _unitPrice = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(SubTotal));
            }
        }

        public decimal SubTotal => decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

        public bool IsValid =>
            string.IsNullOrEmpty(this[nameof(Name)]) &&
            string.IsNullOrEmpty(this[nameof(Quantity)]) &&
            string.IsNullOrEmpty(this[nameof(UnitPrice)]);

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Name):
                        if (string.IsNullOrWhiteSpace(Name)) return "Name is required.";
                        if (Name.Length > MaxNameLength) return $"Name must be ≤ {MaxNameLength} characters.";
                        return null;
                    case nameof(Quantity):
                        if (Quantity < MinQuantity || Quantity > MaxQuantity) return $"Quantity must be between {MinQuantity} and {MaxQuantity}.";
                        return null;
                    case nameof(UnitPrice):
                        if (UnitPrice < MinUnitPrice || UnitPrice > MaxUnitPrice) return $"Unit price must be between {MinUnitPrice:0.##} and {MaxUnitPrice:0,0.##}.";
                        return null;
                    default:
                        return null;
                }
            }
        }
    }

    public partial class BillingManagementView : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<BillItem> Items { get; set; }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(nameof(TotalAmount)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public BillingManagementView()
        {
            InitializeComponent();
            DataContext = this;

            Items = new ObservableCollection<BillItem>();
            Items.CollectionChanged += Items_CollectionChanged;

            // Start with one default line
            AddItemInternal();
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (BillItem item in e.NewItems) item.PropertyChanged += Item_PropertyChanged;

            if (e.OldItems != null)
                foreach (BillItem item in e.OldItems) item.PropertyChanged -= Item_PropertyChanged;

            CalculateTotal();
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BillItem.SubTotal) ||
                e.PropertyName == nameof(BillItem.Quantity) ||
                e.PropertyName == nameof(BillItem.UnitPrice) ||
                e.PropertyName == nameof(BillItem.Name))
            {
                CalculateTotal();
            }
        }

        private void CalculateTotal() => TotalAmount = Items.Where(i => i.IsValid).Sum(i => i.SubTotal);

        // ----------------- VALIDATION: Patient details -----------------
        private bool ValidatePatientDetails(bool focusOnFirstError = true)
        {
            // txtPatient, cmbDepartment, dpVisitDate are named in XAML
            if (string.IsNullOrWhiteSpace(txtPatient?.Text))
            {
                MessageBox.Show("Please enter Patient Name/ID.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (focusOnFirstError) txtPatient?.Focus();
                return false;
            }

            if (cmbDepartment?.SelectedItem == null)
            {
                MessageBox.Show("Please select a Department.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (focusOnFirstError) cmbDepartment?.Focus();
                return false;
            }

            if (dpVisitDate?.SelectedDate == null)
            {
                MessageBox.Show("Please pick an Admission/Visit Date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (focusOnFirstError) dpVisitDate?.Focus();
                return false;
            }

            return true;
        }

        // ----------------- Buttons -----------------

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            // NEW: block adding services until required patient details are provided
            if (!ValidatePatientDetails()) return;

            AddItemInternal();
        }

        private void AddItemInternal()
        {
            Items.Add(new BillItem
            {
                Name = "New Service/Procedure",
                Quantity = BillItem.MinQuantity,
                UnitPrice = 50.00m
            });
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (productsDataGrid?.SelectedItem is BillItem selectedItem)
            {
                Items.Remove(selectedItem);
            }
            else
            {
                MessageBox.Show("Please select an item to remove.",
                                "Selection Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GenerateBill_Click(object sender, RoutedEventArgs e)
        {
            CommitPendingEdit(productsDataGrid);

            // Patient details must be filled
            if (!ValidatePatientDetails()) return;

            // Items required
            if (!Items.Any())
            {
                MessageBox.Show("Add at least one item before generating the bill.",
                                "No Items", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var invalid = Items.Where(i => !i.IsValid).ToList();
            if (invalid.Any())
            {
                var summary = BuildValidationSummary(invalid);
                MessageBox.Show("Fix the following before generating the bill:\n\n" + summary,
                                "Validation Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TotalAmount > 50_000_000m)
            {
                MessageBox.Show("Total amount seems unusually high. Please review the items.",
                                "Sanity Check", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var deptText = (cmbDepartment.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            MessageBox.Show(
                $"Patient: {txtPatient.Text}\nDepartment: {deptText}\nDate: {dpVisitDate.SelectedDate:yyyy-MM-dd}\n\nTotal Due: {TotalAmount:C}\n\nFinal bill generated.",
                "Bill Generated",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ----------------- DataGrid hooks & helpers -----------------

        private void productsDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && e.Row?.Item is BillItem item)
            {
                item.RefreshValidation();
            }
        }

        private static void CommitPendingEdit(DataGrid grid)
        {
            if (grid == null) return;
            if (grid.CommitEdit(DataGridEditingUnit.Cell, true))
                grid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        private static string BuildValidationSummary(System.Collections.Generic.IEnumerable<BillItem> items)
        {
            var sb = new StringBuilder();
            int idx = 1;
            foreach (var it in items)
            {
                var nameErr = it[nameof(BillItem.Name)];
                var qtyErr = it[nameof(BillItem.Quantity)];
                var priceErr = it[nameof(BillItem.UnitPrice)];

                sb.AppendLine($"Item {idx}: {(string.IsNullOrWhiteSpace(it?.Name) ? "(no name)" : it.Name)}");
                if (!string.IsNullOrEmpty(nameErr)) sb.AppendLine($"  • {nameErr}");
                if (!string.IsNullOrEmpty(qtyErr)) sb.AppendLine($"  • {qtyErr}");
                if (!string.IsNullOrEmpty(priceErr)) sb.AppendLine($"  • {priceErr}");
                sb.AppendLine();
                idx++;
            }
            return sb.ToString().Trim();
        }

        // Optional input filters (only used if wired in XAML)
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            bool isDigit = e.Text.All(char.IsDigit);
            bool isDot = e.Text == ".";
            if (isDigit) { e.Handled = false; return; }
            if (isDot && tb != null && !tb.Text.Contains(".")) { e.Handled = false; return; }
            e.Handled = true;
        }
    }
}
