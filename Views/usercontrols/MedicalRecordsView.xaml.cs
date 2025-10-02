using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Data.SqlClient;

namespace HospitalManagementSystem.Views.UserControls
{
    // ------------ Small ID/Display option for dropdowns ------------
    public class IdOption
    {
        public string Id { get; set; }
        public string Display { get; set; }
    }

    // ------------ 1) Data model ------------
    public class MedicalRecord : INotifyPropertyChanged
    {
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

        public string RecordID { get; set; }

        public string PatientID { get { return _patientID; } set { _patientID = value; OnPropertyChanged(nameof(PatientID)); } }
        public string StaffID { get { return _staffID; } set { _staffID = value; OnPropertyChanged(nameof(StaffID)); } }
        public DateTime? VisitDate { get { return _visitDate; } set { _visitDate = value; OnPropertyChanged(nameof(VisitDate)); } }
        public string ChiefComplaint { get { return _chiefComplaint; } set { _chiefComplaint = value; OnPropertyChanged(nameof(ChiefComplaint)); } }
        public string Diagnosis { get { return _diagnosis; } set { _diagnosis = value; OnPropertyChanged(nameof(Diagnosis)); } }
        public string Treatment { get { return _treatment; } set { _treatment = value; OnPropertyChanged(nameof(Treatment)); } }
        public string Prescription { get { return _prescription; } set { _prescription = value; OnPropertyChanged(nameof(Prescription)); } }
        public string VitalSigns { get { return _vitalSigns; } set { _vitalSigns = value; OnPropertyChanged(nameof(VitalSigns)); } }
        public string Notes { get { return _notes; } set { _notes = value; OnPropertyChanged(nameof(Notes)); } }
        public DateTime? FollowUpDate { get { return _followUpDate; } set { _followUpDate = value; OnPropertyChanged(nameof(FollowUpDate)); } }
        public string Status { get { return _status; } set { _status = value; OnPropertyChanged(nameof(Status)); } }

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            var h = PropertyChanged;
            if (h != null) h(this, new PropertyChangedEventArgs(propertyName));
        }

        public MedicalRecord Clone() { return (MedicalRecord)MemberwiseClone(); }
    }

    // ------------ 2) View code-behind ------------
    public partial class MedicalRecordsView : UserControl
    {
        private string _userId;
        private string _appId;
        private const string CollectionName = "medical_records";

        // TODO: set your real connection string here
        private readonly string _connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public ObservableCollection<MedicalRecord> RecordsList { get; set; }
        public MedicalRecord CurrentRecord { get; set; }

        // Dropdown data
        public ObservableCollection<IdOption> PatientOptions { get; private set; }
        public ObservableCollection<IdOption> StaffOptions { get; private set; }

        // Validation config
        private static readonly Regex IdRegex = new Regex(@"^[A-Za-z0-9_-]{1,50}$", RegexOptions.Compiled);
        private static readonly HashSet<string> AllowedStatuses =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Open", "Pending", "Closed", "Cancelled" };

        // Max lengths
        private const int MaxChiefComplaint = 500;
        private const int MaxDiagnosis = 500;
        private const int MaxTreatment = 1000;
        private const int MaxPrescription = 1000;
        private const int MaxVitalSigns = 500;
        private const int MaxNotes = 2000;

        public MedicalRecordsView()
        {
            InitializeComponent();

            RecordsList = new ObservableCollection<MedicalRecord>();
            PatientOptions = new ObservableCollection<IdOption>();
            StaffOptions = new ObservableCollection<IdOption>();

            CurrentRecord = new MedicalRecord { Status = "Open", VisitDate = DateTime.Today };

            DataContext = this;
            Loaded += async (s, e) => await InitializeAndLoadData();
        }

        private async Task InitializeAndLoadData()
        {
            try
            {
                _appId = Environment.GetEnvironmentVariable("__app_id") ?? "default-app-id";
                _userId = Environment.GetEnvironmentVariable("USER_ID") ?? "anonymous_user";

                // Status options (if not specified in XAML)
                var statusCmb = FindName("StatusComboBox") as ComboBox;
                if (statusCmb != null && statusCmb.Items.Count == 0)
                {
                    foreach (var s in AllowedStatuses) statusCmb.Items.Add(new ComboBoxItem { Content = s });
                }

                await LoadPatientDropdownAsync();
                await LoadStaffDropdownAsync();

                LoadData();

                var del = FindName("DeleteRecordButton") as Button;
                if (del != null) del.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Initialization Error: " + ex.Message, "Application Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            await Task.CompletedTask;
        }

        // ------------ Dropdown loaders ------------
        private async Task LoadPatientDropdownAsync()
        {
            PatientOptions.Clear();
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    await cn.OpenAsync();
                    const string sql = @"SELECT PatientID, FirstName, LastName
                                         FROM Patients
                                         ORDER BY PatientID;";
                    using (var cmd = new SqlCommand(sql, cn))
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            var id = r.GetInt32(0).ToString();
                            var first = r.IsDBNull(1) ? "" : r.GetString(1);
                            var last = r.IsDBNull(2) ? "" : r.GetString(2);
                            PatientOptions.Add(new IdOption
                            {
                                Id = id,
                                Display = id + " — " + (first + " " + last).Trim()
                            });
                        }
                    }
                }
            }
            catch
            {
                // Fallback demo data if DB not reachable
                PatientOptions.Add(new IdOption { Id = "P101", Display = "P101 — Demo Patient A" });
                PatientOptions.Add(new IdOption { Id = "P102", Display = "P102 — Demo Patient B" });
            }

            var cmb = FindName("cmbPatientId") as ComboBox;
            if (cmb != null)
            {
                cmb.ItemsSource = PatientOptions;
                cmb.SelectedValuePath = "Id";
                cmb.DisplayMemberPath = "Display";
                if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            }
        }

        private async Task LoadStaffDropdownAsync()
        {
            StaffOptions.Clear();

            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    await cn.OpenAsync();

                    // Try Staffs, then Staff
                    bool loaded = false;
                    string[] tables = new[] { "Staffs", "Staff" };

                    for (int i = 0; i < tables.Length && !loaded; i++)
                    {
                        string t = tables[i];
                        try
                        {
                            string sql = "SELECT StaffID, FirstName, LastName FROM " + t + " ORDER BY StaffID;";
                            using (var cmd = new SqlCommand(sql, cn))
                            using (var r = await cmd.ExecuteReaderAsync())
                            {
                                while (await r.ReadAsync())
                                {
                                    var id = r.GetInt32(0).ToString();
                                    var fn = r.IsDBNull(1) ? "" : r.GetString(1);
                                    var ln = r.IsDBNull(2) ? "" : r.GetString(2);
                                    StaffOptions.Add(new IdOption
                                    {
                                        Id = id,
                                        Display = id + " — " + (fn + " " + ln).Trim()
                                    });
                                }
                                loaded = StaffOptions.Count > 0;
                            }
                        }
                        catch { /* try next table name */ }
                    }
                }
            }
            catch { /* ignore; fall back below */ }

            if (StaffOptions.Count == 0)
            {
                // Fallback demo data
                StaffOptions.Add(new IdOption { Id = "D01", Display = "D01 — Doctor One" });
                StaffOptions.Add(new IdOption { Id = "D02", Display = "D02 — Doctor Two" });
            }

            var cmb = FindName("cmbStaffId") as ComboBox;
            if (cmb != null)
            {
                cmb.ItemsSource = StaffOptions;
                cmb.SelectedValuePath = "Id";
                cmb.DisplayMemberPath = "Display";
                if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            }
        }

        // ------------ Seed grid demo data ------------
        private void LoadData()
        {
            RecordsList.Clear();

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

            var grid = FindName("RecordsDataGrid") as DataGrid;
            if (grid != null) grid.ItemsSource = RecordsList;

            // Sync status drop
            var cmb = FindName("StatusComboBox") as ComboBox;
            if (cmb != null)
            {
                foreach (ComboBoxItem it in cmb.Items)
                {
                    if (string.Equals(Convert.ToString(it.Content), CurrentRecord.Status, StringComparison.OrdinalIgnoreCase))
                    {
                        cmb.SelectedItem = it;
                        break;
                    }
                }
            }
        }

        // ------------ 3) CRUD handlers ------------
        private void RecordsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = FindName("RecordsDataGrid") as DataGrid;
            if (grid != null && grid.SelectedItem is MedicalRecord selected)
            {
                CurrentRecord = selected.Clone();

                DataContext = null;
                DataContext = this;

                var del = FindName("DeleteRecordButton") as Button;
                if (del != null) del.IsEnabled = true;

                // Sync status
                var sc = FindName("StatusComboBox") as ComboBox;
                if (sc != null)
                {
                    foreach (ComboBoxItem it in sc.Items)
                    {
                        if (string.Equals(Convert.ToString(it.Content), CurrentRecord.Status, StringComparison.OrdinalIgnoreCase))
                        {
                            sc.SelectedItem = it;
                            break;
                        }
                    }
                }

                // Sync patient/staff dropdowns
                var pcmb = FindName("cmbPatientId") as ComboBox;
                if (pcmb != null) pcmb.SelectedValue = CurrentRecord.PatientID;
                var scmb = FindName("cmbStaffId") as ComboBox;
                if (scmb != null) scmb.SelectedValue = CurrentRecord.StaffID;
            }
            else
            {
                ClearForm();
            }
        }

        private void NewRecordButton_Click(object sender, RoutedEventArgs e) { ClearForm(); }

        private void ClearForm()
        {
            CurrentRecord = new MedicalRecord
            {
                Status = "Open",
                VisitDate = DateTime.Today
            };

            DataContext = null;
            DataContext = this;

            var del = FindName("DeleteRecordButton") as Button;
            if (del != null) del.IsEnabled = false;

            ClearValidationHighlights();

            var sc = FindName("StatusComboBox") as ComboBox;
            if (sc != null)
            {
                foreach (ComboBoxItem it in sc.Items)
                {
                    if (string.Equals(Convert.ToString(it.Content), "Open", StringComparison.OrdinalIgnoreCase))
                    {
                        sc.SelectedItem = it;
                        break;
                    }
                }
            }

            var pcmb = FindName("cmbPatientId") as ComboBox;
            if (pcmb != null && pcmb.Items.Count > 0) pcmb.SelectedIndex = 0;
            var scmb = FindName("cmbStaffId") as ComboBox;
            if (scmb != null && scmb.Items.Count > 0) scmb.SelectedIndex = 0;
        }

        private void SaveRecordButton_Click(object sender, RoutedEventArgs e)
        {
            SnapshotFormIntoCurrentRecord();
            NormalizeCurrentRecord();

            string errors;
            if (!ValidateCurrentRecord(out errors))
            {
                MessageBox.Show("Please fix the following:\n\n" + errors,
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isNew = string.IsNullOrEmpty(CurrentRecord.RecordID);
            if (isNew &&
                RecordsList.Any(r =>
                    string.Equals(r.PatientID, CurrentRecord.PatientID, StringComparison.OrdinalIgnoreCase) &&
                    r.VisitDate.HasValue && CurrentRecord.VisitDate.HasValue &&
                    r.VisitDate.Value.Date == CurrentRecord.VisitDate.Value.Date))
            {
                var dupRes = MessageBox.Show(
                    "A record for this patient and date already exists.\nCreate another anyway?",
                    "Possible Duplicate", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (dupRes != MessageBoxResult.Yes) return;
            }

            try
            {
                DateTime now = DateTime.Now;

                if (isNew)
                {
                    CurrentRecord.RecordID = Guid.NewGuid().ToString("N");
                    CurrentRecord.CreatedDate = now;
                    CurrentRecord.UpdatedDate = now;
                    CurrentRecord.UpdatedBy = _userId;
                    RecordsList.Add(CurrentRecord.Clone());
                }
                else
                {
                    CurrentRecord.UpdatedDate = now;
                    CurrentRecord.UpdatedBy = _userId;

                    var existing = RecordsList.FirstOrDefault(r => r.RecordID == CurrentRecord.RecordID);
                    if (existing != null)
                    {
                        int idx = RecordsList.IndexOf(existing);
                        RecordsList[idx] = CurrentRecord.Clone();
                    }
                }

                MessageBox.Show(isNew ? "New record created successfully!"
                                      : ("Record " + CurrentRecord.RecordID + " updated successfully!"),
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                var grid = FindName("RecordsDataGrid") as DataGrid;
                if (grid != null)
                    grid.SelectedItem = RecordsList.FirstOrDefault(r => r.RecordID == CurrentRecord.RecordID);

                var del = FindName("DeleteRecordButton") as Button;
                if (del != null) del.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving record: " + ex.Message, "Save Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentRecord == null || string.IsNullOrEmpty(CurrentRecord.RecordID))
            {
                MessageBox.Show("Please select a record to delete.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Delete Record ID: " + CurrentRecord.RecordID + " ?",
                "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var rec = RecordsList.FirstOrDefault(r => r.RecordID == CurrentRecord.RecordID);
                if (rec != null) RecordsList.Remove(rec);

                ClearForm();
                MessageBox.Show("Record deleted successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting record: " + ex.Message, "Delete Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ------------ 4) Validation & helpers ------------
        private void SnapshotFormIntoCurrentRecord()
        {
            if (CurrentRecord == null) CurrentRecord = new MedicalRecord();

            // Prefer dropdowns; fall back to textboxes if you kept them.
            var pcmb = FindName("cmbPatientId") as ComboBox;
            if (pcmb != null && pcmb.SelectedValue != null)
                CurrentRecord.PatientID = Convert.ToString(pcmb.SelectedValue);
            else
                CurrentRecord.PatientID = ReadTextFrom("txtPatientID", CurrentRecord.PatientID);

            var scmb = FindName("cmbStaffId") as ComboBox;
            if (scmb != null && scmb.SelectedValue != null)
                CurrentRecord.StaffID = Convert.ToString(scmb.SelectedValue);
            else
                CurrentRecord.StaffID = ReadTextFrom("txtStaffID", CurrentRecord.StaffID);

            CurrentRecord.ChiefComplaint = ReadTextFrom("txtChiefComplaint", CurrentRecord.ChiefComplaint);
            CurrentRecord.Diagnosis = ReadTextFrom("txtDiagnosis", CurrentRecord.Diagnosis);
            CurrentRecord.Treatment = ReadTextFrom("txtTreatment", CurrentRecord.Treatment);
            CurrentRecord.Prescription = ReadTextFrom("txtPrescription", CurrentRecord.Prescription);
            CurrentRecord.VitalSigns = ReadTextFrom("txtVitalSigns", CurrentRecord.VitalSigns);
            CurrentRecord.Notes = ReadTextFrom("txtNotes", CurrentRecord.Notes);

            var dpVisit = FindName("dpVisitDate") as DatePicker;
            if (dpVisit != null) CurrentRecord.VisitDate = dpVisit.SelectedDate;

            var dpFollow = FindName("dpFollowUpDate") as DatePicker;
            if (dpFollow != null) CurrentRecord.FollowUpDate = dpFollow.SelectedDate;

            var cmb = FindName("StatusComboBox") as ComboBox;
            if (cmb != null && cmb.SelectedItem is ComboBoxItem ci)
                CurrentRecord.Status = Convert.ToString(ci.Content);
        }

        private string ReadTextFrom(string name, string fallback)
        {
            var tb = FindName(name) as TextBox;
            return tb != null ? (tb.Text ?? string.Empty).Trim() : (fallback ?? string.Empty);
        }

        private void NormalizeCurrentRecord()
        {
            CurrentRecord.PatientID = (CurrentRecord.PatientID ?? "").Trim();
            CurrentRecord.StaffID = (CurrentRecord.StaffID ?? "").Trim();
            CurrentRecord.ChiefComplaint = (CurrentRecord.ChiefComplaint ?? "").Trim();
            CurrentRecord.Diagnosis = (CurrentRecord.Diagnosis ?? "").Trim();
            CurrentRecord.Treatment = (CurrentRecord.Treatment ?? "").Trim();
            CurrentRecord.Prescription = (CurrentRecord.Prescription ?? "").Trim();
            CurrentRecord.VitalSigns = (CurrentRecord.VitalSigns ?? "").Trim();
            CurrentRecord.Notes = (CurrentRecord.Notes ?? "").Trim();
        }

        private bool ValidateCurrentRecord(out string summary)
        {
            var sb = new StringBuilder();
            Control firstInvalid = null;
            ClearValidationHighlights();

            // Patient ID
            if (string.IsNullOrWhiteSpace(CurrentRecord.PatientID))
            {
                AppendError(sb, "Patient ID is required.");
                if (firstInvalid == null) firstInvalid = FindCtrl("cmbPatientId") ?? FindCtrl("txtPatientID");
                MarkInvalidIfExists("cmbPatientId", "Choose a Patient."); MarkInvalidIfExists("txtPatientID", "Enter Patient ID.");
            }
            else if (!IdRegex.IsMatch(CurrentRecord.PatientID))
            {
                AppendError(sb, "Patient ID must use only letters, numbers, _ or -, and be 1–50 characters.");
                if (firstInvalid == null) firstInvalid = FindCtrl("cmbPatientId") ?? FindCtrl("txtPatientID");
                MarkInvalidIfExists("cmbPatientId", "Invalid ID format."); MarkInvalidIfExists("txtPatientID", "Allowed: A–Z, a–z, 0–9, _ or - (1–50).");
            }

            // Staff ID
            if (string.IsNullOrWhiteSpace(CurrentRecord.StaffID))
            {
                AppendError(sb, "Staff ID is required.");
                if (firstInvalid == null) firstInvalid = FindCtrl("cmbStaffId") ?? FindCtrl("txtStaffID");
                MarkInvalidIfExists("cmbStaffId", "Choose a Staff."); MarkInvalidIfExists("txtStaffID", "Enter Staff ID.");
            }
            else if (!IdRegex.IsMatch(CurrentRecord.StaffID))
            {
                AppendError(sb, "Staff ID must use only letters, numbers, _ or -, and be 1–50 characters.");
                if (firstInvalid == null) firstInvalid = FindCtrl("cmbStaffId") ?? FindCtrl("txtStaffID");
                MarkInvalidIfExists("cmbStaffId", "Invalid ID format."); MarkInvalidIfExists("txtStaffID", "Allowed: A–Z, a–z, 0–9, _ or - (1–50).");
            }

            // Visit date
            if (CurrentRecord.VisitDate == null)
            {
                AppendError(sb, "Visit Date is required.");
                if (firstInvalid == null) firstInvalid = FindCtrl("dpVisitDate");
                MarkInvalidIfExists("dpVisitDate", "Select Visit Date.");
            }
            else if (CurrentRecord.VisitDate.Value.Date > DateTime.Today)
            {
                AppendError(sb, "Visit Date cannot be in the future.");
                if (firstInvalid == null) firstInvalid = FindCtrl("dpVisitDate");
                MarkInvalidIfExists("dpVisitDate", "Visit Date cannot be in the future.");
            }

            // Follow-up date (optional)
            if (CurrentRecord.FollowUpDate != null && CurrentRecord.VisitDate != null &&
                CurrentRecord.FollowUpDate.Value.Date < CurrentRecord.VisitDate.Value.Date)
            {
                AppendError(sb, "Follow-up Date cannot be before the Visit Date.");
                if (firstInvalid == null) firstInvalid = FindCtrl("dpFollowUpDate");
                MarkInvalidIfExists("dpFollowUpDate", "Follow-up cannot be before visit date.");
            }

            // Optional text lengths
            CheckMaxLen("txtChiefComplaint", CurrentRecord.ChiefComplaint, MaxChiefComplaint, "Chief Complaint", sb, ref firstInvalid);
            CheckMaxLen("txtDiagnosis", CurrentRecord.Diagnosis, MaxDiagnosis, "Diagnosis", sb, ref firstInvalid);
            CheckMaxLen("txtTreatment", CurrentRecord.Treatment, MaxTreatment, "Treatment", sb, ref firstInvalid);
            CheckMaxLen("txtPrescription", CurrentRecord.Prescription, MaxPrescription, "Prescription", sb, ref firstInvalid);
            CheckMaxLen("txtVitalSigns", CurrentRecord.VitalSigns, MaxVitalSigns, "Vital Signs", sb, ref firstInvalid);
            CheckMaxLen("txtNotes", CurrentRecord.Notes, MaxNotes, "Notes", sb, ref firstInvalid);

            // Status
            if (string.IsNullOrWhiteSpace(CurrentRecord.Status) || !AllowedStatuses.Contains(CurrentRecord.Status))
            {
                AppendError(sb, "Status is required and must be Open, Pending, Closed, or Cancelled.");
                if (firstInvalid == null) firstInvalid = FindCtrl("StatusComboBox");
                MarkInvalidIfExists("StatusComboBox", "Select a valid status.");
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

        private void CheckMaxLen(string ctrlName, string value, int max, string display, StringBuilder sb, ref Control firstInvalid)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > max)
            {
                AppendError(sb, display + " is too long (max " + max + " characters).");
                if (firstInvalid == null) firstInvalid = FindCtrl(ctrlName);
                MarkInvalidIfExists(ctrlName, "Max " + max + " characters.");
            }
        }

        // ---- UI helpers for validation visuals ----
        private void ClearValidationHighlights()
        {
            string[] names = {
                "cmbPatientId","cmbStaffId","txtPatientID","txtStaffID",
                "dpVisitDate","txtChiefComplaint","txtDiagnosis",
                "txtTreatment","txtPrescription","txtVitalSigns","txtNotes","dpFollowUpDate","StatusComboBox"
            };

            foreach (var n in names)
            {
                var c = FindCtrl(n);
                if (c != null)
                {
                    c.ClearValue(Border.BorderBrushProperty);
                    c.ClearValue(Border.BorderThicknessProperty);
                    c.ToolTip = null;
                }
            }
        }

        private Control FindCtrl(string name) { return FindName(name) as Control; }

        private void MarkInvalidIfExists(string name, string tooltip)
        {
            var c = FindCtrl(name);
            if (c != null)
            {
                c.BorderBrush = Brushes.Red;
                c.BorderThickness = new Thickness(1.5);
                c.ToolTip = tooltip;
            }
        }

        private static void AppendError(StringBuilder sb, string msg) { sb.AppendLine("• " + msg); }
    }
}
