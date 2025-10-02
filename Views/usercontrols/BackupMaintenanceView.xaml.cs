using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Interaction logic for BackupMaintenanceView.xaml
    /// </summary>
    public partial class BackupMaintenanceView : UserControl
    {
        // TODO: **CRITICAL: Replace this with your actual database connection string.**
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        private const int BackupSettingsRecordId = 1;

        // Allowed values for frequency validation
        private static readonly string[] AllowedFrequencies = new[] { "Daily", "Weekly", "Monthly" };

        public BackupMaintenanceView()
        {
            InitializeComponent();
            this.Loaded += BackupMaintenanceView_Loaded;
        }

        private async void BackupMaintenanceView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadBackupSettingsAsync();
        }

        /// <summary>
        /// Represents the data model for backup settings.
        /// </summary>
        public class BackupSettings
        {
            public int BackupSettingID { get; set; }
            public string BackupLocation { get; set; }
            public string BackupFrequency { get; set; }
            public int RetentionPeriodDays { get; set; }
            public string EmailNotifications { get; set; }
            public string BackupTime { get; set; }
            public DateTime? LastBackupDate { get; set; }
        }

        /// <summary>
        /// Loads backup settings from the database and populates the UI.
        /// </summary>
        private async Task LoadBackupSettingsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var sqlQuery = "SELECT * FROM BackupSettings WHERE BackupSettingId = @Id";
                    using (var command = new SqlCommand(sqlQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Id", BackupSettingsRecordId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                txtBackupLocation.Text = reader["BackupLocation"].ToString();
                                cmbBackupFrequency.Text = reader["BackupFrequency"].ToString();
                                txtRetentionPeriod.Text = reader["RetentionPeriodDays"].ToString();
                                txtEmailNotifications.Text = reader["EmailNotifications"].ToString();
                                txtBackupTime.Text = reader["BackupTime"].ToString();

                                if (reader["LastBackupDate"] != DBNull.Value)
                                {
                                    txtLastBackup.Text = $"Last Backup: {((DateTime)reader["LastBackupDate"]).ToString("yyyy-MM-dd hh:mm tt")}";
                                }
                                else
                                {
                                    txtLastBackup.Text = "Last Backup: (none)";
                                }
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred while loading settings: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load backup settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves or updates the backup settings in the database.
        /// </summary>
        private async Task SaveBackupSettingsAsync(BackupSettings settings)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Check if a record exists.
                var checkQuery = "SELECT COUNT(*) FROM BackupSettings WHERE BackupSettingId = @Id";
                using (var checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", BackupSettingsRecordId);
                    int recordCount = (int)await checkCommand.ExecuteScalarAsync();

                    if (recordCount > 0)
                    {
                        // Update
                        var updateSql = @"
                            UPDATE BackupSettings
                            SET BackupLocation = @BackupLocation, BackupFrequency = @BackupFrequency, 
                                RetentionPeriodDays = @RetentionPeriodDays, EmailNotifications = @EmailNotifications, 
                                BackupTime = @BackupTime
                            WHERE BackupSettingId = @Id;";
                        using (var updateCommand = new SqlCommand(updateSql, connection))
                        {
                            AddParameters(updateCommand, settings);
                            updateCommand.Parameters.AddWithValue("@Id", BackupSettingsRecordId);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Insert new
                        var insertSql = @"
                            INSERT INTO BackupSettings (BackupSettingId, BackupLocation, BackupFrequency, RetentionPeriodDays, EmailNotifications, BackupTime)
                            VALUES (@Id, @BackupLocation, @BackupFrequency, @RetentionPeriodDays, @EmailNotifications, @BackupTime);";
                        using (var insertCommand = new SqlCommand(insertSql, connection))
                        {
                            AddParameters(insertCommand, settings);
                            insertCommand.Parameters.AddWithValue("@Id", BackupSettingsRecordId);
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds parameters to the SQL command to prevent SQL injection.
        /// </summary>
        private void AddParameters(SqlCommand command, BackupSettings settings)
        {
            command.Parameters.AddWithValue("@BackupLocation", (object)settings.BackupLocation ?? DBNull.Value);
            command.Parameters.AddWithValue("@BackupFrequency", (object)settings.BackupFrequency ?? DBNull.Value);
            command.Parameters.AddWithValue("@RetentionPeriodDays", settings.RetentionPeriodDays);
            command.Parameters.AddWithValue("@EmailNotifications", (object)settings.EmailNotifications ?? DBNull.Value);
            command.Parameters.AddWithValue("@BackupTime", (object)settings.BackupTime ?? DBNull.Value);
        }

        /// <summary>
        /// Handles the "Run Backup Now" button click.
        /// </summary>
        private async void btnRunBackupNow_Click(object sender, RoutedEventArgs e)
        {
            // Gather from UI
            var settings = GatherSettingsFromUi();

            // Validate settings (including filesystem checks)
            if (!TryValidateSettings(settings, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ensure folder exists (prompt to create)
            if (!EnsureBackupDirectoryExists(settings.BackupLocation))
            {
                // user cancelled or failed to create
                return;
            }

            // Save settings first (so LastBackupDate update aligns with the same record)
            await SaveBackupSettingsAsync(settings);

            try
            {
                string bakFile = Path.Combine(settings.BackupLocation, "HMSDatabase.bak");

                // Additional path sanity check
                if (!IsPathWritable(settings.BackupLocation))
                {
                    MessageBox.Show("The selected backup folder is not writable. Choose a different folder.", "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // BACKUP DATABASE requires a full absolute path and sufficient permissions.
                    var sqlCommand = "BACKUP DATABASE [HMSDatabase] TO DISK = @disk WITH NOFORMAT, NOINIT, NAME = N'HMSDatabase-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
                    using (var command = new SqlCommand(sqlCommand, connection))
                    {
                        command.Parameters.AddWithValue("@disk", bakFile);
                        command.CommandTimeout = 600;
                        await command.ExecuteNonQueryAsync();

                        // Update LastBackupDate on success
                        var updateQuery = "UPDATE BackupSettings SET LastBackupDate = GETDATE() WHERE BackupSettingId = @Id";
                        using (var updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Id", BackupSettingsRecordId);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

                MessageBox.Show("Database backup completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadBackupSettingsAsync(); // Refresh the UI with the new backup date.
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to run backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Optimize Database" button click.
        /// </summary>
        private async void btnOptimizeDatabase_Click(object sender, RoutedEventArgs e)
        {
            // Optimization does not need all settings, but we can ensure DB connection string exists
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show("Database connection is not configured.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // NOTE: sp_msforeachtable is undocumented; this is fine for a small project,
                    // but for robustness you may loop tables from sys.tables and rebuild.
                    var sqlCommand = @"EXEC sp_msforeachtable 'ALTER INDEX ALL ON ? REBUILD WITH (ONLINE = ON)';";
                    using (var command = new SqlCommand(sqlCommand, connection))
                    {
                        command.CommandTimeout = 180; // longer timeout
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Database optimization completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to optimize database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the "Check Database Integrity" button click.
        /// </summary>
        private async void btnCheckIntegrity_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show("Database connection is not configured.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var sqlCommand = "DBCC CHECKDB ('HMSDatabase') WITH NO_INFOMSGS;";
                    using (var command = new SqlCommand(sqlCommand, connection))
                    {
                        command.CommandTimeout = 180;
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Database integrity check completed successfully! No issues were found.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check database integrity: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gathers settings from the UI controls into a model.
        /// </summary>
        private BackupSettings GatherSettingsFromUi()
        {
            return new BackupSettings
            {
                BackupLocation = txtBackupLocation.Text?.Trim(),
                BackupFrequency = cmbBackupFrequency.Text?.Trim(),
                RetentionPeriodDays = int.TryParse(txtRetentionPeriod.Text, out int period) ? period : 0,
                EmailNotifications = txtEmailNotifications.Text?.Trim(),
                BackupTime = txtBackupTime.Text?.Trim()
            };
        }

        /// <summary>
        /// Saves the settings from the UI to the database (with validations).
        /// </summary>
        private async Task SaveSettingsFromUiAsync()
        {
            var settings = GatherSettingsFromUi();

            if (!TryValidateSettings(settings, out string errorMessage, skipFilesystemChecks: true))
            {
                MessageBox.Show(errorMessage, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await SaveBackupSettingsAsync(settings);
            MessageBox.Show("Settings saved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // -------------------------
        // Validation helpers below
        // -------------------------

        private bool TryValidateSettings(BackupSettings s, out string message, bool skipFilesystemChecks = false)
        {
            // Backup location
            if (string.IsNullOrWhiteSpace(s.BackupLocation))
            {
                message = "Please choose a backup folder.";
                return false;
            }

            // Very basic path sanity (no invalid chars)
            if (s.BackupLocation.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                message = "Backup folder path contains invalid characters.";
                return false;
            }

            // Frequency
            if (string.IsNullOrWhiteSpace(s.BackupFrequency) ||
                !AllowedFrequencies.Contains(s.BackupFrequency, StringComparer.OrdinalIgnoreCase))
            {
                message = "Backup Frequency must be one of: Daily, Weekly, or Monthly.";
                return false;
            }

            // Retention (1–3650 days)
            if (s.RetentionPeriodDays < 1 || s.RetentionPeriodDays > 3650)
            {
                message = "Retention Period must be between 1 and 3650 days.";
                return false;
            }

            // Time (either HH:mm or hh:mm tt)
            if (!IsValidTime(s.BackupTime))
            {
                message = "Backup Time must be in 24-hr 'HH:mm' (e.g., 21:30) or 12-hr 'hh:mm tt' (e.g., 09:30 PM) format.";
                return false;
            }

            // Emails (optional but if present, must be valid)
            if (!string.IsNullOrWhiteSpace(s.EmailNotifications) && !IsEmailListValid(s.EmailNotifications))
            {
                message = "Email Notifications must be a comma/semicolon separated list of valid email addresses.";
                return false;
            }

            // Filesystem checks (when actually running backup)
            if (!skipFilesystemChecks)
            {
                // If folder exists but not writable
                if (Directory.Exists(s.BackupLocation) && !IsPathWritable(s.BackupLocation))
                {
                    message = "The selected backup folder is not writable.";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        private bool EnsureBackupDirectoryExists(string folder)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    var result = MessageBox.Show(
                        $"The folder \"{folder}\" does not exist. Do you want to create it?",
                        "Create Folder?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Directory.CreateDirectory(folder);
                        // quick writability test
                        if (!IsPathWritable(folder))
                        {
                            MessageBox.Show("Folder was created but is not writable. Choose a different location.", "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to prepare backup folder: {ex.Message}", "Folder Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static bool IsPathWritable(string folderPath)
        {
            try
            {
                string testFile = Path.Combine(folderPath, $".__writetest_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidTime(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Accept HH:mm (24h) or hh:mm tt (12h)
            // Examples: 07:05, 23:59, 09:30 PM, 12:00 am
            var twentyFourHr = Regex.IsMatch(input, @"^(?:[01]\d|2[0-3]):[0-5]\d$");
            var twelveHr = Regex.IsMatch(input, @"^(0?[1-9]|1[0-2]):[0-5]\d\s?(AM|PM|am|pm)$");

            return twentyFourHr || twelveHr;
        }

        private static bool IsEmailListValid(string emails)
        {
            // Split by comma or semicolon
            var parts = emails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(p => p.Trim())
                              .Where(p => p.Length > 0);

            // Very lightweight email pattern (enough for UI validation)
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            foreach (var e in parts)
            {
                if (!emailRegex.IsMatch(e))
                    return false;
            }
            return true;
        }
    }
}
