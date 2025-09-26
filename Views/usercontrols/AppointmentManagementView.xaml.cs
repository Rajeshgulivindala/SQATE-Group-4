using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    // Data model for an Appointment
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

    public partial class AppointmentManagementView : UserControl
    {
        // TODO: **CRITICAL: Replace this with your actual database connection string.**
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        // ObservableCollection to bind to the DataGrid
        public ObservableCollection<Appointment> Appointments { get; set; }

        public AppointmentManagementView()
        {
            InitializeComponent();
            Appointments = new ObservableCollection<Appointment>();

            this.DataContext = this;

            // Populate ComboBoxes with predefined values
            PopulateComboBoxes();

            // Load all appointments on startup
            LoadAllAppointmentsFromDatabase();
        }

        private void PopulateComboBoxes()
        {
            // Dummy data for comboboxes - in a real app, this would come from the database
            cmbType.ItemsSource = new string[] { "Check-up", "Consultation", "Emergency", "Follow-up" };
            cmbStatus.ItemsSource = new string[] { "Scheduled", "Completed", "Canceled", "Rescheduled" };
        }

        /// <summary>
        /// Loads all appointments from the database.
        /// </summary>
        private async void LoadAllAppointmentsFromDatabase()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "SELECT AppointmentID, PatientID, StaffID, RoomID, AppointmentDate, Duration, Type, Status, Reason, Notes, CreatedDate, CreatedBy FROM Appointments";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            Appointments.Clear();
                            while (await reader.ReadAsync())
                            {
                                Appointments.Add(new Appointment
                                {
                                    AppointmentID = reader.GetInt32(0),
                                    PatientID = reader.GetInt32(1),
                                    StaffID = reader.GetInt32(2),
                                    RoomID = reader.GetInt32(3),
                                    AppointmentDate = reader.GetDateTime(4),
                                    Duration = reader.GetInt32(5),
                                    Type = reader.GetString(6),
                                    Status = reader.GetString(7),
                                    Reason = reader.GetString(8),
                                    Notes = reader.GetString(9),
                                    CreatedDate = reader.GetDateTime(10),
                                    CreatedBy = reader.GetInt32(11)
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
                MessageBox.Show($"Failed to load appointments: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the "Search" button.
        /// </summary>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSearchPatientId.Text, out int patientId))
            {
                await SearchAppointmentsInDatabase(patientId);
            }
            else
            {
                MessageBox.Show("Please enter a valid Patient ID.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the click event for the "Add Appointment" button.
        /// </summary>
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Use TryParse for all integer fields to prevent "Input string was not in a correct format" errors
            if (!int.TryParse(txtPatientID.Text, out int patientId))
            {
                MessageBox.Show("Please enter a valid Patient ID.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtStaffID.Text, out int staffId))
            {
                MessageBox.Show("Please enter a valid Staff ID.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtRoomID.Text, out int roomId))
            {
                MessageBox.Show("Please enter a valid Room ID.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtDuration.Text, out int duration))
            {
                MessageBox.Show("Please enter a valid Duration.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newAppointment = new Appointment
                {
                    PatientID = patientId,
                    StaffID = staffId,
                    RoomID = roomId,
                    AppointmentDate = dpAppointmentDate.SelectedDate ?? DateTime.Now,
                    Duration = duration,
                    Type = cmbType.SelectedValue?.ToString(),
                    Status = cmbStatus.SelectedValue?.ToString(),
                    Reason = txtReason.Text,
                    Notes = txtNotes.Text,
                    CreatedDate = DateTime.Now,
                    CreatedBy = 1 // Placeholder for now
                };

                await AddAppointmentToDatabase(newAppointment);
                Appointments.Add(newAppointment); // Update UI
                MessageBox.Show($"Appointment for Patient ID {newAppointment.PatientID} added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while adding the appointment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the "Update Appointment" button.
        /// </summary>
        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem is Appointment selectedAppt)
            {
                try
                {
                    if (!int.TryParse(txtPatientID.Text, out int patientId) ||
                        !int.TryParse(txtStaffID.Text, out int staffId) ||
                        !int.TryParse(txtRoomID.Text, out int roomId) ||
                        !int.TryParse(txtDuration.Text, out int duration))
                    {
                        MessageBox.Show("Please ensure all ID and Duration fields are valid numbers.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    selectedAppt.PatientID = patientId;
                    selectedAppt.StaffID = staffId;
                    selectedAppt.RoomID = roomId;
                    selectedAppt.AppointmentDate = dpAppointmentDate.SelectedDate ?? selectedAppt.AppointmentDate;
                    selectedAppt.Duration = duration;
                    selectedAppt.Type = cmbType.SelectedValue?.ToString();
                    selectedAppt.Status = cmbStatus.SelectedValue?.ToString();
                    selectedAppt.Reason = txtReason.Text;
                    selectedAppt.Notes = txtNotes.Text;

                    await UpdateAppointmentInDatabase(selectedAppt);
                    MessageBox.Show($"Appointment ID {selectedAppt.AppointmentID} updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadAllAppointmentsFromDatabase(); // Refresh the DataGrid
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while updating the appointment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an appointment to update.", "No Appointment Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the click event for the "Delete Appointment" button.
        /// </summary>
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem is Appointment selectedAppt)
            {
                try
                {
                    await DeleteAppointmentFromDatabase(selectedAppt);
                    Appointments.Remove(selectedAppt);
                    MessageBox.Show($"Appointment ID {selectedAppt.AppointmentID} deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while deleting the appointment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an appointment to delete.", "No Appointment Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Populates the form fields when an item in the DataGrid is selected.
        /// </summary>
        private void AppointmentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem is Appointment selectedAppt)
            {
                txtAppointmentID.Text = selectedAppt.AppointmentID.ToString();
                txtPatientID.Text = selectedAppt.PatientID.ToString();
                txtStaffID.Text = selectedAppt.StaffID.ToString();
                txtRoomID.Text = selectedAppt.RoomID.ToString();
                dpAppointmentDate.SelectedDate = selectedAppt.AppointmentDate;
                txtDuration.Text = selectedAppt.Duration.ToString();
                cmbType.SelectedValue = selectedAppt.Type;
                cmbStatus.SelectedValue = selectedAppt.Status;
                txtReason.Text = selectedAppt.Reason;
                txtNotes.Text = selectedAppt.Notes;
                txtCreatedBy.Text = selectedAppt.CreatedBy.ToString();
            }
        }

        private void ClearForm()
        {
            txtAppointmentID.Text = "";
            txtPatientID.Text = "";
            txtStaffID.Text = "";
            txtRoomID.Text = "";
            dpAppointmentDate.SelectedDate = null;
            txtDuration.Text = "";
            cmbType.SelectedValue = null;
            cmbStatus.SelectedValue = null;
            txtReason.Text = "";
            txtNotes.Text = "";
            txtCreatedBy.Text = "";
        }

        /// <summary>
        /// Searches for appointments in the database by patient ID.
        /// </summary>
        private async Task SearchAppointmentsInDatabase(int patientId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "SELECT AppointmentID, PatientID, StaffID, RoomID, AppointmentDate, Duration, Type, Status, Reason, Notes, CreatedDate, CreatedBy FROM Appointments WHERE PatientID = @PatientID";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@PatientID", patientId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            Appointments.Clear();
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    Appointments.Add(new Appointment
                                    {
                                        AppointmentID = reader.GetInt32(0),
                                        PatientID = reader.GetInt32(1),
                                        StaffID = reader.GetInt32(2),
                                        RoomID = reader.GetInt32(3),
                                        AppointmentDate = reader.GetDateTime(4),
                                        Duration = reader.GetInt32(5),
                                        Type = reader.GetString(6),
                                        Status = reader.GetString(7),
                                        Reason = reader.GetString(8),
                                        Notes = reader.GetString(9),
                                        CreatedDate = reader.GetDateTime(10),
                                        CreatedBy = reader.GetInt32(11)
                                    });
                                }
                            }
                            else
                            {
                                MessageBox.Show("No appointments found for this patient ID.", "Search Result", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show($"Failed to search appointments: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- CRUD Helper Methods ---

        /// <summary>
        /// Checks if a room with the given RoomID exists. If not, it creates a new room and returns the new RoomID.
        /// </summary>
        private async Task<int> CheckAndCreateRoom(int roomId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if the room with the specified ID already exists
                string checkSql = "SELECT RoomID FROM Rooms WHERE RoomID = @RoomID";
                using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@RoomID", roomId);
                    object existingRoomId = await checkCommand.ExecuteScalarAsync();

                    if (existingRoomId != null)
                    {
                        // Room already exists, return its ID
                        return (int)existingRoomId;
                    }
                    else
                    {
                        // Room does not exist, so we will create it.
                        // We must provide values for all non-nullable columns.
                        string createSql = @"
                            INSERT INTO Rooms (RoomNumber, CreatedDate)
                            VALUES (@RoomNumber, GETDATE());
                            SELECT SCOPE_IDENTITY();";

                        using (SqlCommand createCommand = new SqlCommand(createSql, connection))
                        {
                            createCommand.Parameters.AddWithValue("@RoomNumber", roomId);
                            // Get the newly generated ID
                            var newRoomId = await createCommand.ExecuteScalarAsync();
                            MessageBox.Show($"A new room was automatically created with ID: {newRoomId}", "Room Created", MessageBoxButton.OK, MessageBoxImage.Information);
                            return Convert.ToInt32(newRoomId);
                        }
                    }
                }
            }
        }

        private async Task AddAppointmentToDatabase(Appointment appointment)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // First, check and create the room if it doesn't exist and get the correct RoomID
                int finalRoomId = await CheckAndCreateRoom(appointment.RoomID);

                // Now, add the appointment using the correct RoomID
                string sql = @"INSERT INTO Appointments (PatientID, StaffID, RoomID, AppointmentDate, Duration, Type, Status, Reason, Notes, CreatedDate, CreatedBy)
                               VALUES (@PatientID, @StaffID, @RoomID, @AppointmentDate, @Duration, @Type, @Status, @Reason, @Notes, @CreatedDate, @CreatedBy);
                               SELECT SCOPE_IDENTITY();"; // Get the newly created ID
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PatientID", appointment.PatientID);
                    command.Parameters.AddWithValue("@StaffID", appointment.StaffID);
                    command.Parameters.AddWithValue("@RoomID", finalRoomId);
                    command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
                    command.Parameters.AddWithValue("@Duration", appointment.Duration);
                    command.Parameters.AddWithValue("@Type", appointment.Type);
                    command.Parameters.AddWithValue("@Status", appointment.Status);
                    command.Parameters.AddWithValue("@Reason", appointment.Reason);
                    command.Parameters.AddWithValue("@Notes", appointment.Notes);
                    command.Parameters.AddWithValue("@CreatedDate", appointment.CreatedDate);
                    command.Parameters.AddWithValue("@CreatedBy", appointment.CreatedBy);
                    appointment.AppointmentID = Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        private async Task UpdateAppointmentInDatabase(Appointment appointment)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Appointments SET
                                       PatientID = @PatientID, StaffID = @StaffID, RoomID = @RoomID, AppointmentDate = @AppointmentDate,
                                       Duration = @Duration, Type = @Type, Status = @Status, Reason = @Reason, Notes = @Notes
                               WHERE AppointmentID = @AppointmentID";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PatientID", appointment.PatientID);
                    command.Parameters.AddWithValue("@StaffID", appointment.StaffID);
                    command.Parameters.AddWithValue("@RoomID", appointment.RoomID);
                    command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
                    command.Parameters.AddWithValue("@Duration", appointment.Duration);
                    command.Parameters.AddWithValue("@Type", appointment.Type);
                    command.Parameters.AddWithValue("@Status", appointment.Status);
                    command.Parameters.AddWithValue("@Reason", appointment.Reason);
                    command.Parameters.AddWithValue("@Notes", appointment.Notes);
                    command.Parameters.AddWithValue("@AppointmentID", appointment.AppointmentID);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task DeleteAppointmentFromDatabase(Appointment appointment)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Appointments WHERE AppointmentID = @AppointmentID";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", appointment.AppointmentID);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
