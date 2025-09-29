using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
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
        public int Duration { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class RoomOption { public int RoomID { get; set; } public string Display { get; set; } }
    public class StaffOption { public int StaffID { get; set; } public string Display { get; set; } }
    public class PatientOption { public int PatientID { get; set; } public string Display { get; set; } }

    public partial class AppointmentManagementView : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public ObservableCollection<Appointment> Appointments { get; set; }

        public AppointmentManagementView()
        {
            InitializeComponent();
            Appointments = new ObservableCollection<Appointment>();
            DataContext = this;

            // static lists
            cmbType.ItemsSource = new[] { "Check-up", "Consultation", "Emergency", "Follow-up" };
            cmbStatus.ItemsSource = new[] { "Scheduled", "Completed", "Canceled", "Rescheduled" };

            Loaded += async (_, __) =>
            {
                await LoadPatientDropdownAsync();
                await LoadStaffDropdownAsync();
                await LoadRoomDropdownAsync();
                LoadAllAppointmentsFromDatabase();
            };
        }

        // -------- Dropdown data --------
        private async Task LoadPatientDropdownAsync()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                const string sql = @"SELECT PatientID, FirstName, LastName FROM Patients WHERE IsActive=1 ORDER BY FirstName, LastName;";
                using (var cmd = new SqlCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    var list = new System.Collections.Generic.List<PatientOption>();
                    while (await r.ReadAsync())
                    {
                        int id = r.GetInt32(0);
                        string first = r.IsDBNull(1) ? "" : r.GetString(1);
                        string last = r.IsDBNull(2) ? "" : r.GetString(2);
                        list.Add(new PatientOption { PatientID = id, Display = $"{id} — {first} {last}" });
                    }
                    cmbPatient.ItemsSource = list;
                    if (cmbPatient.Items.Count > 0) cmbPatient.SelectedIndex = 0;
                }
            }
        }

        private async Task LoadStaffDropdownAsync()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();

                // Query ONLY columns that are guaranteed to exist in your schema
                const string sql = @"SELECT StaffID, FirstName, LastName FROM Staffs WHERE IsActive = 1 ORDER BY FirstName, LastName;";
                using (var cmd = new SqlCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    var list = new System.Collections.Generic.List<StaffOption>();
                    while (await r.ReadAsync())
                    {
                        int id = r.GetInt32(0);
                        string fn = r.IsDBNull(1) ? "" : r.GetString(1);
                        string ln = r.IsDBNull(2) ? "" : r.GetString(2);
                        list.Add(new StaffOption
                        {
                            StaffID = id,
                            // Removed role from the display string
                            Display = $"{id} — {fn} {ln}"
                        });
                    }

                    cmbStaff.ItemsSource = list;
                    if (cmbStaff.Items.Count > 0) cmbStaff.SelectedIndex = 0;
                }
            }
        }


        private async Task LoadRoomDropdownAsync()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                const string sql = @"SELECT RoomID, RoomNumber, RoomType FROM Rooms WHERE IsActive=1 ORDER BY RoomNumber;";
                using (var cmd = new SqlCommand(sql, cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    var list = new System.Collections.Generic.List<RoomOption>();
                    while (await r.ReadAsync())
                    {
                        int id = r.GetInt32(0);
                        string num = r.IsDBNull(1) ? "" : r.GetString(1);
                        string type = r.IsDBNull(2) ? "" : r.GetString(2);
                        list.Add(new RoomOption { RoomID = id, Display = $"{id} — {num} ({type})" });
                    }
                    cmbRoom.ItemsSource = list;
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
                    const string sql = @"SELECT AppointmentID, PatientID, StaffID, RoomID, AppointmentDate, Duration,
                                                Type, Status, Reason, Notes, CreatedDate, CreatedBy
                                         FROM Appointments";
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
                MessageBox.Show("Failed to load appointments: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            int pid;
            if (!int.TryParse(txtSearchPatientId.Text, out pid))
            {
                MessageBox.Show("Enter a valid Patient ID.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();
                    const string sql = @"SELECT AppointmentID, PatientID, StaffID, RoomID, AppointmentDate, Duration,
                                                Type, Status, Reason, Notes, CreatedDate, CreatedBy
                                         FROM Appointments
                                         WHERE PatientID = @pid";
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
                MessageBox.Show("Search failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------- Add --------
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            int duration;
            if (!int.TryParse(txtDuration.Text, out duration))
            {
                MessageBox.Show("Enter a valid Duration (minutes).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cmbPatient.SelectedValue == null || cmbStaff.SelectedValue == null || cmbRoom.SelectedValue == null)
            {
                MessageBox.Show("Select Patient, Staff, and Room.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string type = cmbType.SelectedValue as string ?? "Consultation";
            string status = cmbStatus.SelectedValue as string ?? "Scheduled";

            try
            {
                var a = new Appointment
                {
                    PatientID = (int)cmbPatient.SelectedValue,
                    StaffID = (int)cmbStaff.SelectedValue,
                    RoomID = (int)cmbRoom.SelectedValue,
                    AppointmentDate = dpAppointmentDate.SelectedDate ?? DateTime.Now,
                    Duration = duration,
                    Type = type,
                    Status = status,
                    Reason = txtReason.Text ?? "",
                    Notes = txtNotes.Text ?? "",
                    CreatedDate = DateTime.Now,
                    CreatedBy = 1
                };

                await AddAppointmentToDatabase(a);
                Appointments.Add(a);
                MessageBox.Show("Appointment added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Database error adding appointment: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Add failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Select an appointment to update.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int duration;
            if (!int.TryParse(txtDuration.Text, out duration))
            {
                MessageBox.Show("Enter a valid Duration.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cmbPatient.SelectedValue == null || cmbStaff.SelectedValue == null || cmbRoom.SelectedValue == null)
            {
                MessageBox.Show("Select Patient, Staff, and Room.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            selected.PatientID = (int)cmbPatient.SelectedValue;
            selected.StaffID = (int)cmbStaff.SelectedValue;
            selected.RoomID = (int)cmbRoom.SelectedValue;
            selected.AppointmentDate = dpAppointmentDate.SelectedDate ?? selected.AppointmentDate;
            selected.Duration = duration;
            selected.Type = (string)(cmbType.SelectedValue ?? "Consultation");
            selected.Status = (string)(cmbStatus.SelectedValue ?? "Scheduled");
            selected.Reason = txtReason.Text ?? "";
            selected.Notes = txtNotes.Text ?? "";

            try
            {
                await UpdateAppointmentInDatabase(selected);
                MessageBox.Show("Appointment updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadAllAppointmentsFromDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Select an appointment to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await DeleteAppointmentFromDatabase(selected);
                Appointments.Remove(selected);
                MessageBox.Show("Appointment deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // -------- Selection & Clear --------
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
    }
}
