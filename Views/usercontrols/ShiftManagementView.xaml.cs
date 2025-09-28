using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    // --- 1. Data Model Class (INotifyPropertyChanged for Data Binding) ---
    /// <summary>
    /// Represents a single nurse shift assignment. This class implements INotifyPropertyChanged
    /// to ensure the UI updates automatically when properties are changed in the code.
    /// </summary>
    public class ShiftAssignment : INotifyPropertyChanged
    {
        // Property backing fields
        private string _nurseID;
        private DateTime? _assignmentDate;
        private string _shiftType;
        private string _ward;
        private string _notes;
        private string _status;

        // Primary Key / Document ID (Used for unique identification)
        public string ShiftID { get; set; }

        public string NurseID { get => _nurseID; set { _nurseID = value; OnPropertyChanged(nameof(NurseID)); } }
        public DateTime? AssignmentDate { get => _assignmentDate; set { _assignmentDate = value; OnPropertyChanged(nameof(AssignmentDate)); } }
        public string ShiftType { get => _shiftType; set { _shiftType = value; OnPropertyChanged(nameof(ShiftType)); } }
        public string Ward { get => _ward; set { _ward = value; OnPropertyChanged(nameof(Ward)); } }
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(nameof(Notes)); } }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }

        // Audit Fields
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Creates a shallow copy of the object, useful when editing a record 
        /// selected from the DataGrid before committing the changes.
        /// </summary>
        public ShiftAssignment Clone()
        {
            return (ShiftAssignment)this.MemberwiseClone();
        }
    }


    // --- 2. User Control Code-Behind ---
    /// <summary>
    /// Interaction logic for NurseShiftView.xaml
    /// </summary>
    // NOTE: The class name MUST match the x:Class attribute in the XAML file.
    // The 'partial' keyword is essential for InitializeComponent() to be generated.
    public partial class ShiftManagementView : UserControl
    {
        // Placeholder for user and app IDs (in a real app, these would come from authentication)
        private string _userId;
        private string _appId;
        private const string CollectionName = "nurse_shifts";

        // ObservableCollection binds to the DataGrid for real-time list updates
        public ObservableCollection<ShiftAssignment> ShiftsList { get; set; }

        // CurrentShift binds to the detail form fields (TextBoxes, DatePicker, ComboBoxes)
        public ShiftAssignment CurrentShift { get; set; }

        public ShiftManagementView()
        {
            // InitializeComponent() is defined by the compiler because this is a partial class
            InitializeComponent();
            ShiftsList = new ObservableCollection<ShiftAssignment>();

            // Set initial state for the detail form (defaults)
            CurrentShift = new ShiftAssignment { Status = "Confirmed", AssignmentDate = DateTime.Today.AddDays(1) };

            // Set the DataContext to this UserControl instance so bindings work
            this.DataContext = this;

            // Load data when the control is rendered
            Loaded += async (sender, e) => await InitializeAndLoadData();
        }

        private async Task InitializeAndLoadData()
        {
            try
            {
                // Initialize global variables (simulated environment access)
                _appId = Environment.GetEnvironmentVariable("__app_id") ?? "default-app-id";
                _userId = Environment.GetEnvironmentVariable("USER_ID") ?? "anonymous_user";

                // Load initial data (simulated read)
                LoadData();

                // Set initial UI state
                // This control reference is now valid because it is defined in the XAML with x:Name
                DeleteShiftButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization Error: {ex.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Simulates fetching data from the database (READ operation) and populates ShiftsList.
        /// </summary>
        private void LoadData()
        {
            ShiftsList.Clear();

            // --- SIMULATION START: Add simulated data records ---
            ShiftsList.Add(new ShiftAssignment
            {
                ShiftID = "SH001",
                NurseID = "N105",
                AssignmentDate = DateTime.Today.AddDays(1),
                ShiftType = "Day (07:00 - 15:00)",
                Ward = "General Med",
                Status = "Confirmed",
                CreatedDate = DateTime.Now.AddDays(-5),
                UpdatedBy = "admin"
            });
            ShiftsList.Add(new ShiftAssignment
            {
                ShiftID = "SH002",
                NurseID = "N212",
                AssignmentDate = DateTime.Today.AddDays(1),
                ShiftType = "Night (23:00 - 07:00)",
                Ward = "ICU-West",
                Status = "Confirmed",
                CreatedDate = DateTime.Now.AddDays(-3),
                UpdatedBy = "scheduler_A"
            });
            ShiftsList.Add(new ShiftAssignment
            {
                ShiftID = "SH003",
                NurseID = "N300",
                AssignmentDate = DateTime.Today.AddDays(2),
                ShiftType = "Evening (15:00 - 23:00)",
                Ward = "Pediatrics",
                Status = "Tentative",
                CreatedDate = DateTime.Now.AddDays(-1),
                UpdatedBy = "admin"
            });
            // --- SIMULATION END ---

            // Ensure initial ComboBoxes reflect the default CurrentShift
            // These control references are now valid
            if (StatusComboBox.Items.Cast<ComboBoxItem>().Any(i => i.Content.ToString() == CurrentShift.Status))
            {
                StatusComboBox.SelectedValue = CurrentShift.Status;
            }
            if (ShiftTypeComboBox.Items.Cast<ComboBoxItem>().Any(i => i.Content.ToString() == CurrentShift.ShiftType))
            {
                ShiftTypeComboBox.SelectedValue = CurrentShift.ShiftType;
            }
        }

        // --- 3. CRUD Event Handlers (These methods resolve the missing definition errors) ---

        /// <summary>
        /// Handles the DataGrid selection change event, loading the selected assignment 
        /// into the detail form for inspection/editing.
        /// </summary>
        public void ShiftsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShiftsDataGrid.SelectedItem is ShiftAssignment selectedShift)
            {
                // Clone the assignment to populate the form.
                CurrentShift = selectedShift.Clone();

                // Update the DataContext to refresh the detail form fields
                this.DataContext = null;
                this.DataContext = this;

                // Enable delete button for existing record
                DeleteShiftButton.IsEnabled = true;
            }
            else
            {
                // If selection is cleared (e.g., after deletion), reset the form
                ClearForm();
            }
        }

        /// <summary>
        /// Clears the form and prepares a new assignment for entry (CREATE mode).
        /// </summary>
        public void NewShiftButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            // Reset CurrentShift to a fresh instance with defaults
            CurrentShift = new ShiftAssignment
            {
                Status = "Confirmed",
                ShiftType = "Day (07:00 - 15:00)",
                AssignmentDate = DateTime.Today.AddDays(1)
            };

            // Force DataContext refresh to clear all form fields
            this.DataContext = null;
            this.DataContext = this;

            // Ensure the UI state is correct for a new record (no deletion possible)
            DeleteShiftButton.IsEnabled = false;
        }

        /// <summary>
        /// Handles saving a record (CREATE or UPDATE).
        /// </summary>
        public void SaveShiftButton_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (string.IsNullOrEmpty(CurrentShift.NurseID) || CurrentShift.AssignmentDate == null || string.IsNullOrEmpty(CurrentShift.Ward))
            {
                MessageBox.Show("Nurse ID, Assignment Date, and Ward are required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ensure status and shift type are captured from the ComboBoxes (even though they are bound, this adds robustness)
            CurrentShift.Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            CurrentShift.ShiftType = (ShiftTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            try
            {
                DateTime now = DateTime.Now;

                bool isNewRecord = string.IsNullOrEmpty(CurrentShift.ShiftID);

                if (isNewRecord)
                {
                    // CREATE: Generate a unique ID and set audit fields
                    CurrentShift.ShiftID = "SH" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(); // Unique ID for the new record
                    CurrentShift.CreatedDate = now;
                    CurrentShift.UpdatedDate = now;
                    CurrentShift.UpdatedBy = _userId;

                    // Add the new record to the list
                    ShiftsList.Add(CurrentShift);
                }
                else
                {
                    // UPDATE: Update the 'Updated' audit fields
                    CurrentShift.UpdatedDate = now;
                    CurrentShift.UpdatedBy = _userId;

                    // Find and update the existing record in the observable collection
                    var existingShift = ShiftsList.FirstOrDefault(r => r.ShiftID == CurrentShift.ShiftID);
                    if (existingShift != null)
                    {
                        int index = ShiftsList.IndexOf(existingShift);
                        // Replace the object in the list to trigger the DataGrid update
                        ShiftsList[index] = CurrentShift.Clone();
                    }
                }

                MessageBox.Show(isNewRecord ? "New shift assignment created successfully!" : $"Shift {CurrentShift.ShiftID} updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Select the newly saved/updated record in the grid to show audit info
                ShiftsDataGrid.SelectedItem = CurrentShift;
                DeleteShiftButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving assignment: {ex.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles deleting the currently selected assignment (DELETE operation).
        /// </summary>
        public void DeleteShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentShift?.ShiftID))
            {
                MessageBox.Show("Please select an assignment to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete Shift ID: {CurrentShift.ShiftID}?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // --- SIMULATION START: Remove from the list ---
                    var shiftToRemove = ShiftsList.FirstOrDefault(r => r.ShiftID == CurrentShift.ShiftID);
                    if (shiftToRemove != null)
                    {
                        ShiftsList.Remove(shiftToRemove);
                    }
                    // --- SIMULATION END ---

                    ClearForm(); // Reset the detail form
                    MessageBox.Show("Shift assignment deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting assignment: {ex.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}