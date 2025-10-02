using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    // Models
    public class Appointment
    {
        public int AppointmentID { get; set; }
        public int PatientID { get; set; }
        public int StaffID { get; set; }
        public int RoomID { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int Duration { get; set; } // minutes
        public string Type { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class RoomOption { public int RoomID { get; set; } public string Display { get; set; } }
    public class StaffOption { public int StaffID { get; set; } public string Display { get; set; } }

    // Patient dropdown item (shows PatientCode if available)
    public class PatientOption
    {
        public int PatientID { get; set; }
        public string PatientCode { get; set; }  // may be null
        public string Display { get; set; }      // rendered text
    }

    public partial class AppointmentManagementView : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public ObservableCollection<Appointment> Appointments { get; set; }

        private static readonly string[] AllowedTypes = { "Check-up", "Consultation", "Emergency", "Follow-up" };
        private static readonly string[] AllowedStatuses = { "Scheduled", "Completed", "Canceled", "Rescheduled" };

        private const int MinDuration = 5;
        private const int MaxDuration = 480;
        private const int MaxReasonLen = 500;
        private const int MaxNotesLen = 1000;

        public AppointmentManagementView()
        {
            InitializeComponent();
            Appointments = new ObservableCollection<Appointment>();
            DataContext = this;

            cmbType.ItemsSource = AllowedTypes;
            cmbStatus.ItemsSource = AllowedStatuses;

            Loaded += async (_, __) =>
            {
                await LoadPatientDropdownAsync(); // shows PatientCode — Name (fallback to ID)
                await LoadStaffDropdownAsync();
                await LoadRoomDropdownAsync();
                LoadAllAppointmentsFromDatabase();
            };
        }

        // -------- Patient dropdown (by PatientCode if exists) --------
        private async Task LoadPatientDropdownAsync()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();

                // First try query WITH PatientCode
                try
                {
                    const string sqlWithCode = @"
SELECT PatientID, PatientCode, FirstName, LastName
FROM Patients
ORDER BY COALESCE(PatientCode, CAST(PatientID AS varchar(20))), FirstName, LastName;";
                    using (var cmd = new SqlCommand(sqlWithCode, cn))
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<PatientOption>();
                        while (await r.ReadAsync())
                        {
                            int id = r.GetInt32(0);
                            string code = r.IsDBNull(1) ? null : r.GetString(1);
                            string first = r.IsDBNull(2) ? "" : r.GetString(2);
                            string last = r.IsDBNull(3) ? "" : r.GetString(3);

                            list.Add(new PatientOption
                            {
                                PatientID = id,
                                PatientCode = code,
                                Display = !string.IsNullOrWhiteSpace(code) ? $"{code} — {first} {last}" : $"{id} — {first} {last}"
                            });
                        }

                        cmbPatient.ItemsSource = list;
                        cmbPatient.SelectedValuePath = nameof(PatientOption.PatientID);
                        cmbPatient.DisplayMemberPath = nameof(PatientOption.Display);
                        if (list.Count > 0) cmbPatient.SelectedIndex = 0;
                        return;
                    }
                }
                catch (SqlException)
                {
                    // fall through to no-code query
                }

                // Fallback: table without PatientCode column
                const string sqlNoCode = @"
SELECT PatientID, FirstName, LastName
FROM Patients
ORDER BY FirstName, LastName;";
                using (var cmd = new SqlCommand(sqlNoCode, cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    var list = new List<PatientOption>();
                    while (await r.ReadAsync())
                    {
                        int id = r.GetInt32(0);
                        string first = r.IsDBNull(1) ? "" : r.GetString(1);
                        string last = r.IsDBNull(2) ? "" : r.GetString(2);

                        list.Add(new PatientOption
                        {
                            PatientID = id,
                            PatientCode = null,
                            Display = $"{id} — {first} {last}"
                        });
                    }

                    cmbPatient.ItemsSource = list;
                    cmbPatient.SelectedValuePath = nameof(PatientOption.PatientID);
                    cmbPatient.DisplayMemberPath = nameof(PatientOption.Display);
                    if (list.Count > 0) cmbPatient.SelectedIndex = 0;
                }
            }
        }

        // -------- Staff dropdown (robust to table naming) --------
        private async Task LoadStaffDropdownAsync()
        {
            async Task<List<StaffOption>> TryLoadAsync(SqlConnection cn, string tableName, bool filterActive)
            {
                string sql = filterActive
                    ? $@"SELECT StaffID, FirstName, LastName FROM {tableName} WHERE IsActive = 1 ORDER BY FirstName, LastName;"
                    : $@"SELECT StaffID, FirstName, LastName FROM {tableName} ORDER BY FirstName, LastName;";

                using (var cmd = new SqlCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    var list = new List<StaffOption>();
                    while (await r.ReadAsync())
                    {
                        int id = r.GetInt32(0);
                        string fn = r.IsDBNull(1) ? "" : r.GetString(1);
                        string ln = r.IsDBNull(2) ? "" : r.GetString(2);
                        list.Add(new StaffOption { StaffID = id, Display = $"{id} — {fn} {ln}" });
                    }
                    return list;
                }
            }

            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();

                    List<StaffOption> list = null;
                    try { list = await TryLoadAsync(cn, "Staffs", true); } catch { }
                    if (list == null || list.Count == 0) { try { list = await TryLoadAsync(cn, "Staffs", false); } catch { } }
                    if (list == null || list.Count == 0) { try { list = await TryLoadAsync(cn, "Staff", true); } catch { } }
                    if (list == null || list.Count == 0) { try { list = await TryLoadAsync(cn, "Staff", false); } catch { } }

                    cmbStaff.ItemsSource = list ?? new List<StaffOption>();
                    cmbStaff.SelectedValuePath = nameof(StaffOption.StaffID);
                    cmbStaff.DisplayMemberPath = nameof(StaffOption.Display);
                    if (cmbStaff.Items.Count > 0) cmbStaff.SelectedIndex = 0;

                    if (cmbStaff.Items.Count == 0)
                        MessageBox.Show("No staff found (Staff/Staffs table).", "Staff list empty",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load staff: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------- Rooms dropdown --------
        private async Task LoadRoomDropdownAsync()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                const string sql = @"
SELECT RoomID, RoomNumber, RoomType
FROM Rooms
ORDER BY RoomNumber;";
                using (var cmd = new SqlCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    var list = new List<RoomOption>();
                    while (await r.ReadAsync())
                    {
                        int id = r.GetInt32(0);
                        string num = r.IsDBNull(1) ? "" : r.GetString(1);
                        string type = r.IsDBNull(2) ? "" : r.GetString(2);
                        list.Add(new RoomOption { RoomID = id, Display = $"{id} — {num} ({type})" });
                    }
                    cmbRoom.ItemsSource = list;
                    cmbRoom.SelectedValuePath = nameof(RoomOption.RoomID);
                    cmbRoom.DisplayMemberPath = nameof(RoomOption.Display);
                    if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
                }
            }
        }

        // -------- Load & search --------
        private async void LoadAllAppointmentsFromDatabase()
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();
                    const string sql = @"
SELECT AppointmentID, PatientID, StaffID, RoomID, AppointmentDate, Duration,
       Type, Status, Reason, Notes, CreatedDate, CreatedBy
FROM Appointments
ORDER BY AppointmentDate DESC, AppointmentID DESC;";
                    using (var cmd = new SqlCommand(sql, cn))
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        Appointments.Clear();
                        while (await r.ReadAsync())
                        {
                            Appointments.Add(new Appointment
                            {
                                AppointmentID = r.GetInt32(0),
                                PatientID = r.GetInt32(1),
                                StaffID = r.GetInt32(2),
                                RoomID = r.GetInt32(3),
                                AppointmentDate = r.GetDateTime(4),
                                Duration = r.GetInt32(5),
                                Type = r.GetString(6),
                                Status = r.GetString(7),
                                Reason = r.IsDBNull(8) ? "" : r.GetString(8),
                                Notes = r.IsDBNull(9) ? "" : r.GetString(9),
                                CreatedDate = r.GetDateTime(10),
                                CreatedBy = r.GetInt32(11)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load appointments: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtSearchPatientId.Text, out var pid) || pid <= 0)
            {
                MessageBox.Show("Enter a valid Patient ID (positive integer).", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();
                    const string sql = @"
SELECT AppointmentID, PatientID, StaffID, RoomID, AppointmentDate, Duration,
       Type, Status, Reason, Notes, CreatedDate, CreatedBy
FROM Appointments
WHERE PatientID = @pid
ORDER BY AppointmentDate DESC, AppointmentID DESC;";
                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@pid", pid);
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            Appointments.Clear();
                            while (await r.ReadAsync())
                            {
                                Appointments.Add(new Appointment
                                {
                                    AppointmentID = r.GetInt32(0),
                                    PatientID = r.GetInt32(1),
                                    StaffID = r.GetInt32(2),
                                    RoomID = r.GetInt32(3),
                                    AppointmentDate = r.GetDateTime(4),
                                    Duration = r.GetInt32(5),
                                    Type = r.GetString(6),
                                    Status = r.GetString(7),
                                    Reason = r.IsDBNull(8) ? "" : r.GetString(8),
                                    Notes = r.IsDBNull(9) ? "" : r.GetString(9),
                                    CreatedDate = r.GetDateTime(10),
                                    CreatedBy = r.GetInt32(11)
                                });
                            }
                            if (Appointments.Count == 0)
                                MessageBox.Show("No appointments found for this Patient ID.", "Search Result",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------- Add --------
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var candidate = BuildAppointmentFromForm(existingId: 0, defaultCreatedBy: 1);
            var (ok, message) = await ValidateAppointmentAsync(candidate, isUpdate: false);
            if (!ok)
            {
                MessageBox.Show(message, "Validation failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await AddAppointmentToDatabase(candidate);
                Appointments.Add(candidate);
                MessageBox.Show("Appointment added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                LoadAllAppointmentsFromDatabase();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Database error adding appointment: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Add failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddAppointmentToDatabase(Appointment a)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                const string sql = @"
INSERT INTO Appointments
 (PatientID, StaffID, RoomID, AppointmentDate, Duration, Type, Status, Reason, Notes, CreatedDate, CreatedBy)
VALUES
 (@PatientID, @StaffID, @RoomID, @AppointmentDate, @Duration, @Type, @Status, @Reason, @Notes, @CreatedDate, @CreatedBy);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@PatientID", a.PatientID);
                    cmd.Parameters.AddWithValue("@StaffID", a.StaffID);
                    cmd.Parameters.AddWithValue("@RoomID", a.RoomID);
                    cmd.Parameters.AddWithValue("@AppointmentDate", a.AppointmentDate);
                    cmd.Parameters.AddWithValue("@Duration", a.Duration);
                    cmd.Parameters.AddWithValue("@Type", a.Type);
                    cmd.Parameters.AddWithValue("@Status", a.Status);
                    cmd.Parameters.AddWithValue("@Reason", (object)(a.Reason ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", (object)(a.Notes ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedDate", a.CreatedDate);
                    cmd.Parameters.AddWithValue("@CreatedBy", a.CreatedBy);

                    var o = await cmd.ExecuteScalarAsync();
                    a.AppointmentID = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
            }
        }

        // -------- Update --------
        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = AppointmentsDataGrid.SelectedItem as Appointment;
            if (selected == null)
            {
                MessageBox.Show("Select an appointment to update.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var updated = BuildAppointmentFromForm(existingId: selected.AppointmentID, defaultCreatedBy: selected.CreatedBy);
            updated.CreatedDate = selected.CreatedDate; // preserve

            var (ok, message) = await ValidateAppointmentAsync(updated, isUpdate: true);
            if (!ok)
            {
                MessageBox.Show(message, "Validation failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await UpdateAppointmentInDatabase(updated);
                MessageBox.Show("Appointment updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadAllAppointmentsFromDatabase();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateAppointmentInDatabase(Appointment a)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                const string sql = @"
UPDATE Appointments
SET PatientID=@PatientID, StaffID=@StaffID, RoomID=@RoomID, AppointmentDate=@AppointmentDate,
    Duration=@Duration, Type=@Type, Status=@Status, Reason=@Reason, Notes=@Notes
WHERE AppointmentID=@AppointmentID;";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@PatientID", a.PatientID);
                    cmd.Parameters.AddWithValue("@StaffID", a.StaffID);
                    cmd.Parameters.AddWithValue("@RoomID", a.RoomID);
                    cmd.Parameters.AddWithValue("@AppointmentDate", a.AppointmentDate);
                    cmd.Parameters.AddWithValue("@Duration", a.Duration);
                    cmd.Parameters.AddWithValue("@Type", a.Type);
                    cmd.Parameters.AddWithValue("@Status", a.Status);
                    cmd.Parameters.AddWithValue("@Reason", (object)(a.Reason ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", (object)(a.Notes ?? "") ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AppointmentID", a.AppointmentID);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // -------- Delete --------
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = AppointmentsDataGrid.SelectedItem as Appointment;
            if (selected == null)
            {
                MessageBox.Show("Select an appointment to delete.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show("Are you sure you want to delete this appointment?",
                                          "Confirm Delete",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await DeleteAppointmentFromDatabase(selected);
                Appointments.Remove(selected);
                MessageBox.Show("Appointment deleted.", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteAppointmentFromDatabase(Appointment a)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                const string sql = "DELETE FROM Appointments WHERE AppointmentID=@id;";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@id", a.AppointmentID);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // -------- Selection & helpers --------
        private void AppointmentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var a = AppointmentsDataGrid.SelectedItem as Appointment;
            if (a == null) return;

            txtAppointmentID.Text = a.AppointmentID.ToString();
            dpAppointmentDate.SelectedDate = a.AppointmentDate;
            txtDuration.Text = a.Duration.ToString();
            cmbType.SelectedValue = a.Type;
            cmbStatus.SelectedValue = a.Status;
            txtReason.Text = a.Reason ?? "";
            txtNotes.Text = a.Notes ?? "";
            txtCreatedBy.Text = a.CreatedBy.ToString();

            cmbPatient.SelectedValue = a.PatientID;
            cmbStaff.SelectedValue = a.StaffID;
            cmbRoom.SelectedValue = a.RoomID;
        }

        private void ClearForm()
        {
            txtAppointmentID.Text = "";
            dpAppointmentDate.SelectedDate = null;
            txtDuration.Text = "";
            cmbType.SelectedValue = null;
            cmbStatus.SelectedValue = null;
            txtReason.Text = "";
            txtNotes.Text = "";
            txtCreatedBy.Text = "";
            if (cmbPatient.Items.Count > 0) cmbPatient.SelectedIndex = 0;
            if (cmbStaff.Items.Count > 0) cmbStaff.SelectedIndex = 0;
            if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
        }

        private Appointment BuildAppointmentFromForm(int existingId, int defaultCreatedBy)
        {
            int.TryParse(txtDuration.Text, out var duration);
            int createdBy = defaultCreatedBy;
            if (int.TryParse(txtCreatedBy.Text, out var parsed) && parsed > 0)
                createdBy = parsed;

            return new Appointment
            {
                AppointmentID = existingId,
                PatientID = (cmbPatient?.SelectedValue is int pid) ? pid : 0,
                StaffID = (cmbStaff?.SelectedValue is int sid) ? sid : 0,
                RoomID = (cmbRoom?.SelectedValue is int rid) ? rid : 0,
                AppointmentDate = dpAppointmentDate.SelectedDate ?? DateTime.MinValue,
                Duration = duration,
                Type = (cmbType?.SelectedValue as string) ?? "",
                Status = (cmbStatus?.SelectedValue as string) ?? "",
                Reason = (txtReason?.Text ?? "").Trim(),
                Notes = (txtNotes?.Text ?? "").Trim(),
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy
            };
        }

        private async Task<(bool ok, string message)> ValidateAppointmentAsync(Appointment a, bool isUpdate)
        {
            var errors = new StringBuilder();

            if (a.PatientID <= 0) errors.AppendLine("• Select a Patient.");
            if (a.StaffID <= 0) errors.AppendLine("• Select a Staff.");
            if (a.RoomID <= 0) errors.AppendLine("• Select a Room.");

            if (a.AppointmentDate == DateTime.MinValue)
                errors.AppendLine("• Select an Appointment Date/Time.");
            else
            {
                var now = DateTime.Now.AddMinutes(-1);
                var max = DateTime.Now.AddDays(365);
                if (a.AppointmentDate < now) errors.AppendLine("• Appointment time must be in the future.");
                if (a.AppointmentDate > max) errors.AppendLine("• Appointment time cannot be more than 365 days from today.");
            }

            if (a.Duration < MinDuration || a.Duration > MaxDuration)
                errors.AppendLine($"• Duration must be between {MinDuration} and {MaxDuration} minutes.");

            if (string.IsNullOrWhiteSpace(a.Type) || Array.IndexOf(AllowedTypes, a.Type) < 0) errors.AppendLine("• Select a valid Type.");
            if (string.IsNullOrWhiteSpace(a.Status) || Array.IndexOf(AllowedStatuses, a.Status) < 0) errors.AppendLine("• Select a valid Status.");

            if (a.Reason?.Length > MaxReasonLen) errors.AppendLine($"• Reason is too long (max {MaxReasonLen}).");
            if (a.Notes?.Length > MaxNotesLen) errors.AppendLine($"• Notes are too long (max {MaxNotesLen}).");

            if (a.CreatedBy <= 0) errors.AppendLine("• CreatedBy must be a positive integer.");

            if (errors.Length > 0) return (false, errors.ToString());

            a.Reason = Truncate(a.Reason, MaxReasonLen);
            a.Notes = Truncate(a.Notes, MaxNotesLen);

            var conflict = await CheckScheduleConflictsAsync(a, isUpdate);
            if (!string.IsNullOrEmpty(conflict)) return (false, conflict);

            return (true, "OK");
        }

        private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));

        private async Task<string> CheckScheduleConflictsAsync(Appointment a, bool isUpdate)
        {
            var start = a.AppointmentDate;
            var end = a.AppointmentDate.AddMinutes(a.Duration);

            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                string exclude = isUpdate ? "AND AppointmentID <> @ThisId" : "";

                const string staffSqlBase = @"
SELECT TOP 1 AppointmentID
FROM Appointments
WHERE StaffID = @StaffID
  AND (@Start < DATEADD(MINUTE, Duration, AppointmentDate))
  AND (AppointmentDate < @End) ";

                using (var staffCmd = new SqlCommand(staffSqlBase + exclude + ";", cn))
                {
                    staffCmd.Parameters.AddWithValue("@StaffID", a.StaffID);
                    staffCmd.Parameters.AddWithValue("@Start", start);
                    staffCmd.Parameters.AddWithValue("@End", end);
                    if (isUpdate) staffCmd.Parameters.AddWithValue("@ThisId", a.AppointmentID);

                    var exists = await staffCmd.ExecuteScalarAsync();
                    if (exists != null && exists != DBNull.Value)
                        return "• The selected Staff has another appointment that overlaps with this time.";
                }

                const string roomSqlBase = @"
SELECT TOP 1 AppointmentID
FROM Appointments
WHERE RoomID = @RoomID
  AND (@Start < DATEADD(MINUTE, Duration, AppointmentDate))
  AND (AppointmentDate < @End) ";

                using (var roomCmd = new SqlCommand(roomSqlBase + exclude + ";", cn))
                {
                    roomCmd.Parameters.AddWithValue("@RoomID", a.RoomID);
                    roomCmd.Parameters.AddWithValue("@Start", start);
                    roomCmd.Parameters.AddWithValue("@End", end);
                    if (isUpdate) roomCmd.Parameters.AddWithValue("@ThisId", a.AppointmentID);

                    var exists = await roomCmd.ExecuteScalarAsync();
                    if (exists != null && exists != DBNull.Value)
                        return "• The selected Room is already booked during this time.";
                }
            }
            return string.Empty;
        }
    }
}
