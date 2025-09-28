using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

// Removed FireSharp namespaces: 
// using FireSharp.Config;
// using FireSharp.Interfaces;
// using FireSharp.Response;

namespace HospitalManagementSystem.Views.UserControls
{
    // --- 1. Data Model Class (INotifyPropertyChanged for Data Binding) ---
    /// <summary>
    /// Represents a single medical visit record, implementing INotifyPropertyChanged
    /// to ensure UI updates automatically when properties change.
    /// </summary>
    public class MedicalRecord : INotifyPropertyChanged
    {
        // Property backing fields for INotifyPropertyChanged
        private string _patientID;
        private string _staffID;
        private DateTime? _visitDate;
        private string _chiefComplaint;
        private string _diagnosis;
        private string _treatment;
        private string _prescription;
        private string _vitalSigns;
        private string _notes;
        private DateTime? _followUpDate;
        private string _status;

        // Firestore Document ID (Unique identifier)
        public string RecordID { get; set; }

        public string PatientID { get => _patientID; set { _patientID = value; OnPropertyChanged(nameof(PatientID)); } }
        public string StaffID { get => _staffID; set { _staffID = value; OnPropertyChanged(nameof(StaffID)); } }
        public DateTime? VisitDate { get => _visitDate; set { _visitDate = value; OnPropertyChanged(nameof(VisitDate)); } }
        public string ChiefComplaint { get => _chiefComplaint; set { _chiefComplaint = value; OnPropertyChanged(nameof(ChiefComplaint)); } }
        public string Diagnosis { get => _diagnosis; set { _diagnosis = value; OnPropertyChanged(nameof(Diagnosis)); } }
        public string Treatment { get => _treatment; set { _treatment = value; OnPropertyChanged(nameof(Treatment)); } }
        public string Prescription { get => _prescription; set { _prescription = value; OnPropertyChanged(nameof(Prescription)); } }
        public string VitalSigns { get => _vitalSigns; set { _vitalSigns = value; OnPropertyChanged(nameof(VitalSigns)); } }
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(nameof(Notes)); } }
        public DateTime? FollowUpDate { get => _followUpDate; set { _followUpDate = value; OnPropertyChanged(nameof(FollowUpDate)); } }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }

        // Audit Fields (set automatically on save/update)
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Helper method to create a deep copy for safe editing
        public MedicalRecord Clone()
        {
            return (MedicalRecord)this.MemberwiseClone();
        }
    }


    // --- 2. User Control Code-Behind ---
    public partial class MedicalRecordsView : UserControl
    {
        // Removed private IFirebaseClient _client; as FireSharp is not referenced.
        private string _userId;
        private string _appId;
        private const string CollectionName = "medical_records";

        // ObservableCollection binds to the DataGrid for real-time list updates
        public ObservableCollection<MedicalRecord> RecordsList { get; set; }

        // CurrentRecord binds to the detail form fields
        public MedicalRecord CurrentRecord { get; set; }

        public MedicalRecordsView()
        {
            InitializeComponent();
            RecordsList = new ObservableCollection<MedicalRecord>();

            // Set initial state for the detail form (defaults)
            CurrentRecord = new MedicalRecord { Status = "Open", VisitDate = DateTime.Today };

            // Set the DataContext to this UserControl instance
            this.DataContext = this;

            // Load data when the control is loaded
            Loaded += async (sender, e) => await InitializeAndLoadData();
        }

        private async Task InitializeAndLoadData()
        {
            try
            {
                // Initialize global variables (provided by the Canvas environment)
                _appId = Environment.GetEnvironmentVariable("__app_id") ?? "default-app-id";
                // Assuming we can derive a user ID from the environment
                _userId = Environment.GetEnvironmentVariable("USER_ID") ?? "anonymous_user";

                // IMPORTANT: Firebase setup relies on external configuration 
                // that is often not fully accessible in this environment.

                // Load initial data (simulated read) - Removed 'await' since LoadData is now synchronous
                LoadData();

                // Set initial UI state
                // NOTE: Ensure the XAML file has x:Name="DeleteRecordButton" for this line to compile.
                DeleteRecordButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization Error: {ex.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Added return to ensure the method returns a Task
            await Task.CompletedTask;
        }

        /// <summary>
        /// Simulates fetching data from the database (READ operation).
        /// Changed from async Task to void as it contains only synchronous simulation code.
        /// </summary>
        private void LoadData()
        {
            // The conceptual path for private user data: /artifacts/{appId}/users/{userId}/medical_records/{recordId}
            string firebaseUrlPath = $"artifacts/{_appId}/users/{_userId}/{CollectionName}";

            // --- SIMULATION START ---
            RecordsList.Clear();

            // Add simulated data records
            RecordsList.Add(new MedicalRecord
            {
                RecordID = "REC001",
                PatientID = "P101",
                StaffID = "D05",
                VisitDate = DateTime.Today.AddDays(-10),
                ChiefComplaint = "Fever and cough",
                Diagnosis = "Viral infection",
                Status = "Closed",
                CreatedDate = DateTime.Now.AddDays(-10),
                UpdatedBy = "System"
            });
            RecordsList.Add(new MedicalRecord
            {
                RecordID = "REC002",
                PatientID = "P102",
                StaffID = "D01",
                VisitDate = DateTime.Today.AddDays(-5),
                ChiefComplaint = "Sprained ankle",
                Diagnosis = "Grade I Sprain",
                Treatment = "RICE",
                Status = "Open",
                CreatedDate = DateTime.Now.AddDays(-5),
                UpdatedBy = "D01"
            });

            // NOTE: Ensure the XAML file has x:Name="RecordsDataGrid" for this line to compile.
            RecordsDataGrid.ItemsSource = RecordsList;
            // --- SIMULATION END ---

            // NOTE: Ensure the XAML file has x:Name="StatusComboBox" for this line to compile.
            StatusComboBox.SelectedValue = CurrentRecord.Status;
        }

        // --- 3. CRUD Event Handlers (Matching XAML Click/SelectionChanged handlers) ---

        /// <summary>
        /// Handles the DataGrid selection change event. Clones the selected record for editing.
        /// (Requires SelectionChanged="RecordsDataGrid_SelectionChanged" in XAML)
        /// </summary>
        private void RecordsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecordsDataGrid.SelectedItem is MedicalRecord selectedRecord)
            {
                // Clone the record to enable rollback if the user cancels editing
                CurrentRecord = selectedRecord.Clone();

                // Update the DataContext to refresh the detail form fields
                this.DataContext = null;
                this.DataContext = this;

                // Enable delete button for existing record
                DeleteRecordButton.IsEnabled = true;
            }
            else
            {
                // If selection is cleared, reset the form
                ClearForm();
            }
        }

        /// <summary>
        /// Clears the form and prepares a new record for entry (CREATE mode).
        /// (Requires Click="NewRecordButton_Click" in XAML)
        /// </summary>
        private void NewRecordButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            // Reset CurrentRecord to a fresh instance
            CurrentRecord = new MedicalRecord
            {
                Status = "Open",
                VisitDate = DateTime.Today
            };

            // Force DataContext refresh
            this.DataContext = null;
            this.DataContext = this;

            // Ensure the UI state is correct for a new record
            DeleteRecordButton.IsEnabled = false;
        }

        /// <summary>
        /// Handles saving a record (CREATE or UPDATE).
        /// Changed from async void to void as it contains only synchronous simulation code.
        /// (Requires Click="SaveRecordButton_Click" in XAML)
        /// </summary>
        private void SaveRecordButton_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (string.IsNullOrEmpty(CurrentRecord.PatientID) || CurrentRecord.VisitDate == null)
            {
                MessageBox.Show("Patient ID and Visit Date are required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get the selected status from the ComboBox
            CurrentRecord.Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            try
            {
                DateTime now = DateTime.Now;

                bool isNewRecord = string.IsNullOrEmpty(CurrentRecord.RecordID);

                if (isNewRecord)
                {
                    // CREATE: Generate a unique ID and set audit fields
                    CurrentRecord.RecordID = Guid.NewGuid().ToString("N");
                    CurrentRecord.CreatedDate = now;
                    CurrentRecord.UpdatedDate = now;
                    CurrentRecord.UpdatedBy = _userId;
                }
                else
                {
                    // UPDATE: Only update the 'Updated' audit fields
                    CurrentRecord.UpdatedDate = now;
                    CurrentRecord.UpdatedBy = _userId;
                }

                string recordPath = $"{CollectionName}/{CurrentRecord.RecordID}";

                // --- SIMULATION START: Database interaction ---
                // In a real C# app with a Firebase SDK, the Set/Update call would happen here.

                // Simulate ObservableCollection update:
                if (isNewRecord)
                {
                    // Add the newly created record to the list
                    RecordsList.Add(CurrentRecord);
                }
                else
                {
                    // Find and replace the old version in the list with the updated version
                    var existingRecord = RecordsList.FirstOrDefault(r => r.RecordID == CurrentRecord.RecordID);
                    if (existingRecord != null)
                    {
                        int index = RecordsList.IndexOf(existingRecord);
                        // We must re-assign the clone to the list to trigger the ObservableCollection change notification
                        RecordsList[index] = CurrentRecord.Clone();
                    }
                }
                // --- SIMULATION END ---

                MessageBox.Show(isNewRecord ? "New record created successfully!" : $"Record {CurrentRecord.RecordID} updated successfully!", "Success");

                // Select the newly saved/updated record in the grid
                RecordsDataGrid.SelectedItem = RecordsList.LastOrDefault();
                DeleteRecordButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving record: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles deleting the currently selected record.
        /// Changed from async void to void as it contains only synchronous simulation code.
        /// (Requires Click="DeleteRecordButton_Click" in XAML)
        /// </summary>
        private void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentRecord?.RecordID))
            {
                MessageBox.Show("Please select a record to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Custom confirmation message box (replaces standard alert/confirm)
            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete Record ID: {CurrentRecord.RecordID}? This action cannot be undone.", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string recordPath = $"{CollectionName}/{CurrentRecord.RecordID}";

                    // --- SIMULATION START: Database interaction ---
                    // In a real C# app with a Firebase SDK, the Delete call would happen here.

                    // Simulate ObservableCollection removal:
                    var recordToRemove = RecordsList.FirstOrDefault(r => r.RecordID == CurrentRecord.RecordID);
                    if (recordToRemove != null)
                    {
                        RecordsList.Remove(recordToRemove);
                    }
                    // --- SIMULATION END ---

                    ClearForm(); // Reset the detail form
                    MessageBox.Show("Record deleted successfully.", "Success");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting record: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}