using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Interaction logic for RoomManagementView.xaml
    /// </summary>
    public partial class RoomManagementView : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True";

        public class Room
        {
            public int RoomID { get; set; }
            public int DepartmentID { get; set; }
            public string RoomNumber { get; set; }
            public string RoomType { get; set; }
            public int Capacity { get; set; }
            public string Equipment { get; set; }
            public string Status { get; set; }
            public int Floor { get; set; }
            public string Description { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        public ObservableCollection<Room> Rooms { get; set; } = new ObservableCollection<Room>();

        public RoomManagementView()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += async (_, __) => await LoadRoomsFromDatabase();
        }

        // ---------- Load ----------
        private async Task LoadRoomsFromDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sqlQuery = @"
SELECT RoomID, DepartmentID, RoomNumber, RoomType, Capacity, Equipment, Status, Floor, Description, IsActive, CreatedDate
FROM Rooms
ORDER BY RoomNumber;";

                    using (var command = new SqlCommand(sqlQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        Rooms.Clear();

                        while (await reader.ReadAsync())
                        {
                            var room = new Room
                            {
                                RoomID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                DepartmentID = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                RoomNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                RoomType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Capacity = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                Equipment = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Status = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Floor = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                Description = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                IsActive = !reader.IsDBNull(9) && reader.GetBoolean(9),
                                CreatedDate = reader.IsDBNull(10) ? DateTime.MinValue : reader.GetDateTime(10)
                            };
                            Rooms.Add(room);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load rooms: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Add ----------
        private async void btnAddRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read + validate inputs
                string deptText = (txtDepartmentID.Text ?? "").Trim();
                string roomNo = (txtRoomNumber.Text ?? "").Trim();
                string roomType = (cmbRoomType.Text ?? "").Trim();
                string capText = (txtCapacity.Text ?? "").Trim();
                string equipment = (txtEquipment.Text ?? "").Trim();
                string status = (txtStatus.Text ?? "").Trim();
                string floorText = (txtFloor.Text ?? "").Trim();
                string descr = (txtDescription.Text ?? "").Trim();
                bool isActive = chkIsActive.IsChecked ?? false;

                if (string.IsNullOrWhiteSpace(roomNo))
                {
                    MessageBox.Show("Room Number is required.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtRoomNumber.Focus(); return;
                }
                if (string.IsNullOrWhiteSpace(roomType))
                {
                    MessageBox.Show("Room Type is required.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbRoomType.Focus(); return;
                }

                int departmentId;
                if (!int.TryParse(deptText, NumberStyles.Integer, CultureInfo.InvariantCulture, out departmentId))
                {
                    MessageBox.Show("Department ID must be a whole number.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDepartmentID.Focus(); return;
                }

                int capacity;
                if (!int.TryParse(capText, NumberStyles.Integer, CultureInfo.InvariantCulture, out capacity) || capacity < 0)
                {
                    MessageBox.Show("Capacity must be a non-negative whole number.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCapacity.Focus(); return;
                }

                int floor;
                if (!int.TryParse(floorText, NumberStyles.Integer, CultureInfo.InvariantCulture, out floor))
                {
                    MessageBox.Show("Floor must be a whole number.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtFloor.Focus(); return;
                }

                var newRoom = new Room
                {
                    DepartmentID = departmentId,
                    RoomNumber = roomNo,
                    RoomType = roomType,
                    Capacity = capacity,
                    Equipment = equipment,
                    Status = status,
                    Floor = floor,
                    Description = descr,
                    IsActive = isActive,
                    CreatedDate = DateTime.Now
                };

                // Insert and get new RoomID
                int newId = await AddRoomToDatabase(newRoom);
                if (newId <= 0)
                {
                    MessageBox.Show("Insert failed (no ID returned).", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                newRoom.RoomID = newId;
                Rooms.Add(newRoom);

                MessageBox.Show("Room '" + newRoom.RoomNumber + "' added successfully.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding room: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Update ----------
        private async void btnUpdateRoom_Click(object sender, RoutedEventArgs e)
        {
            var selected = RoomsDataGrid.SelectedItem as Room;
            if (selected == null)
            {
                MessageBox.Show("Please select a room to update.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Validate and apply edits from inputs
                int departmentId, capacity, floor;

                if (!int.TryParse((txtDepartmentID.Text ?? "").Trim(), out departmentId))
                {
                    MessageBox.Show("Department ID must be a whole number.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                if (!int.TryParse((txtCapacity.Text ?? "").Trim(), out capacity) || capacity < 0)
                {
                    MessageBox.Show("Capacity must be a non-negative whole number.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                if (!int.TryParse((txtFloor.Text ?? "").Trim(), out floor))
                {
                    MessageBox.Show("Floor must be a whole number.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }

                string roomNo = (txtRoomNumber.Text ?? "").Trim();
                string roomType = (cmbRoomType.Text ?? "").Trim();

                if (string.IsNullOrWhiteSpace(roomNo))
                {
                    MessageBox.Show("Room Number is required.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                if (string.IsNullOrWhiteSpace(roomType))
                {
                    MessageBox.Show("Room Type is required.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }

                selected.DepartmentID = departmentId;
                selected.RoomNumber = roomNo;
                selected.RoomType = roomType;
                selected.Capacity = capacity;
                selected.Equipment = (txtEquipment.Text ?? "").Trim();
                selected.Status = (txtStatus.Text ?? "").Trim();
                selected.Floor = floor;
                selected.Description = (txtDescription.Text ?? "").Trim();
                selected.IsActive = chkIsActive.IsChecked ?? false;

                await UpdateRoomInDatabase(selected);

                MessageBox.Show("Room '" + selected.RoomNumber + "' has been updated.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating room: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Delete ----------
        private async void btnDeleteRoom_Click(object sender, RoutedEventArgs e)
        {
            var selected = RoomsDataGrid.SelectedItem as Room;
            if (selected == null)
            {
                MessageBox.Show("Please select a room to delete.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Delete room '" + selected.RoomNumber + "' (ID " + selected.RoomID + ")?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await DeleteRoomFromDatabase(selected.RoomID);
                Rooms.Remove(selected);

                MessageBox.Show("Room '" + selected.RoomNumber + "' has been deleted.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting room: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearInputs()
        {
            txtDepartmentID.Clear();
            txtRoomNumber.Clear();
            cmbRoomType.SelectedIndex = -1;
            txtCapacity.Clear();
            txtEquipment.Clear();
            txtStatus.Clear();
            txtFloor.Clear();
            txtDescription.Clear();
            chkIsActive.IsChecked = false;
            RoomsDataGrid.SelectedItem = null;
        }

        // ---------- SQL helpers ----------
        private async Task<int> AddRoomToDatabase(Room room)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                const string sql = @"
INSERT INTO Rooms (DepartmentID, RoomNumber, RoomType, Capacity, Equipment, Status, Floor, Description, IsActive, CreatedDate)
VALUES (@DepartmentID, @RoomNumber, @RoomType, @Capacity, @Equipment, @Status, @Floor, @Description, @IsActive, @CreatedDate);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@DepartmentID", room.DepartmentID);
                    command.Parameters.AddWithValue("@RoomNumber", room.RoomNumber ?? "");
                    command.Parameters.AddWithValue("@RoomType", room.RoomType ?? "");
                    command.Parameters.AddWithValue("@Capacity", room.Capacity);
                    command.Parameters.AddWithValue("@Equipment", (object)(room.Equipment ?? "") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Status", (object)(room.Status ?? "") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Floor", room.Floor);
                    command.Parameters.AddWithValue("@Description", (object)(room.Description ?? "") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IsActive", room.IsActive);
                    command.Parameters.AddWithValue("@CreatedDate", room.CreatedDate);

                    object o = await command.ExecuteScalarAsync();
                    return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
            }
        }

        private async Task UpdateRoomInDatabase(Room room)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                const string sql = @"
UPDATE Rooms
SET DepartmentID=@DepartmentID, RoomNumber=@RoomNumber, RoomType=@RoomType,
    Capacity=@Capacity, Equipment=@Equipment, Status=@Status, Floor=@Floor,
    Description=@Description, IsActive=@IsActive
WHERE RoomID=@RoomID;";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@DepartmentID", room.DepartmentID);
                    command.Parameters.AddWithValue("@RoomNumber", room.RoomNumber ?? "");
                    command.Parameters.AddWithValue("@RoomType", room.RoomType ?? "");
                    command.Parameters.AddWithValue("@Capacity", room.Capacity);
                    command.Parameters.AddWithValue("@Equipment", (object)(room.Equipment ?? "") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Status", (object)(room.Status ?? "") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Floor", room.Floor);
                    command.Parameters.AddWithValue("@Description", (object)(room.Description ?? "") ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IsActive", room.IsActive);
                    command.Parameters.AddWithValue("@RoomID", room.RoomID);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task DeleteRoomFromDatabase(int roomId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                const string sql = "DELETE FROM Rooms WHERE RoomID=@RoomID;";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RoomID", roomId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
