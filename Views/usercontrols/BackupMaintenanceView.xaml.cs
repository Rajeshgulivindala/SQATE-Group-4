using System;
using System.Data;
using System.Data.SqlClient;
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
        // This connection string uses Integrated Security, which is a common practice for local development.
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        // This constant is a placeholder for the single record ID for settings.
        private const int BackupSettingsRecordId = 1;

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
                                // Populate the UI controls with data from the database.
                                txtBackupLocation.Text = reader["BackupLocation"].ToString();
                                cmbBackupFrequency.Text = reader["BackupFrequency"].ToString();
                                txtRetentionPeriod.Text = reader["RetentionPeriodDays"].ToString();
                                txtEmailNotifications.Text = reader["EmailNotifications"].ToString();
                                txtBackupTime.Text = reader["BackupTime"].ToString();

                                if (reader["LastBackupDate"] != DBNull.Value)
                                {
                                    txtLastBackup.Text = $"Last Backup: {((DateTime)reader["LastBackupDate"]).ToString("yyyy-MM-dd hh:mm tt")}";
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
                var checkQuery = "SELECT COUNT(*) FROM BackupSettings WHERE Id = @Id";
                using (var checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", BackupSettingsRecordId);
                    int recordCount = (int)await checkCommand.ExecuteScalarAsync();

                    if (recordCount > 0)
                    {
                        // If a record exists, update it.
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
                        // If no record exists, insert a new one.
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
            command.Parameters.AddWithValue("@BackupLocation", settings.BackupLocation ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BackupFrequency", settings.BackupFrequency ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RetentionPeriodDays", settings.RetentionPeriodDays);
            command.Parameters.AddWithValue("@EmailNotifications", settings.EmailNotifications ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BackupTime", settings.BackupTime ?? (object)DBNull.Value);
        }

        /// <summary>
        /// Handles the "Run Backup Now" button click.
        /// </summary>
        private async void btnRunBackupNow_Click(object sender, RoutedEventArgs e)
        {
            // First, save any updated settings from the UI.
            await SaveSettingsFromUiAsync();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // This command uses the T-SQL BACKUP DATABASE command.
                    var sqlCommand = $"BACKUP DATABASE [HMSDatabase] TO DISK = '{txtBackupLocation.Text}\\HMSDatabase.bak' WITH NOFORMAT, NOINIT, NAME = N'HMSDatabase-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
                    using (var command = new SqlCommand(sqlCommand, connection))
                    {
                        await command.ExecuteNonQueryAsync();

                        // Update the Last Backup date in the database after a successful backup.
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
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // This command rebuilds indexes to optimize performance.
                    var sqlCommand = @"EXEC sp_msforeachtable 'ALTER INDEX ALL ON ? REBUILD WITH (ONLINE = ON)';";
                    using (var command = new SqlCommand(sqlCommand, connection))
                    {
                        command.CommandTimeout = 180; // Set a longer timeout for this command.
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
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // This command performs a database consistency check.
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
        /// Saves the settings from the UI to the database.
        /// </summary>
        private async Task SaveSettingsFromUiAsync()
        {
            var settings = new BackupSettings
            {
                BackupLocation = txtBackupLocation.Text,
                BackupFrequency = cmbBackupFrequency.Text,
                RetentionPeriodDays = int.TryParse(txtRetentionPeriod.Text, out int period) ? period : 0,
                EmailNotifications = txtEmailNotifications.Text,
                BackupTime = txtBackupTime.Text
            };
            await SaveBackupSettingsAsync(settings);
        }
    }
}
