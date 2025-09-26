using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Interaction logic for HospitalSettingsView.xaml
    /// </summary>
    public partial class HospitalSettingsView : UserControl
    {
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True";

        /// <summary>
        /// A data model for hospital settings.
        /// </summary>
        public class HospitalSettings
        {
            public string HospitalName { get; set; }
            public string Address { get; set; }
            public string ContactPhone { get; set; }
            public string HospitalEmail { get; set; }
            public string Website { get; set; }
            public string LicenseNumber { get; set; }
            public string DefaultCurrency { get; set; }
            public string TimeZone { get; set; }
            public string LogoPath { get; set; }
        }

        public HospitalSettingsView()
        {
            InitializeComponent();

            // Load settings when the view is initialized.
            LoadSettingsFromDatabase();
        }

        /// <summary>
        /// Loads hospital settings from the database and populates the UI.
        /// </summary>
        private async void LoadSettingsFromDatabase()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // We assume there is only one row for hospital settings.
                    string sqlQuery = "SELECT TOP 1 * FROM HospitalSettings";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Populate the UI controls with data from the database.
                                txtHospitalName.Text = reader["HospitalName"].ToString();
                                txtAddress.Text = reader["Address"].ToString();
                                txtContactPhone.Text = reader["ContactPhone"].ToString();
                                txtHospitalEmail.Text = reader["HospitalEmail"].ToString();
                                txtWebsite.Text = reader["Website"].ToString();
                                txtLicenseNumber.Text = reader["LicenseNumber"].ToString();
                                txtDefaultCurrency.Text = reader["DefaultCurrency"].ToString();
                                txtTimeZone.Text = reader["TimeZone"].ToString();
                                txtLogoPath.Text = reader["LogoPath"].ToString();
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
                MessageBox.Show($"Failed to load hospital settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the "Save Settings" button.
        /// </summary>
        private async void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new HospitalSettings
            {
                HospitalName = txtHospitalName.Text,
                Address = txtAddress.Text,
                ContactPhone = txtContactPhone.Text,
                HospitalEmail = txtHospitalEmail.Text,
                Website = txtWebsite.Text,
                LicenseNumber = txtLicenseNumber.Text,
                DefaultCurrency = txtDefaultCurrency.Text,
                TimeZone = txtTimeZone.Text,
                LogoPath = txtLogoPath.Text
            };

            try
            {
                await SaveSettingsToDatabase(settings);
                MessageBox.Show("Hospital settings saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves or updates the hospital settings in the database.
        /// </summary>
        private async Task SaveSettingsToDatabase(HospitalSettings settings)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Step 1: Check if a record exists.
                string checkQuery = "SELECT COUNT(*) FROM HospitalSettings";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    int recordCount = (int)await checkCommand.ExecuteScalarAsync();

                    if (recordCount > 0)
                    {
                        // Step 2: If a record exists, update it.
                        string updateSql = @"
                            UPDATE HospitalSettings
                            SET HospitalName = @HospitalName, Address = @Address, ContactPhone = @ContactPhone, 
                                HospitalEmail = @HospitalEmail, Website = @Website, LicenseNumber = @LicenseNumber, 
                                DefaultCurrency = @DefaultCurrency, TimeZone = @TimeZone, LogoPath = @LogoPath
                            WHERE Id = 1;"; // Assumes the single record has Id = 1
                        using (SqlCommand updateCommand = new SqlCommand(updateSql, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@HospitalName", settings.HospitalName);
                            updateCommand.Parameters.AddWithValue("@Address", settings.Address);
                            updateCommand.Parameters.AddWithValue("@ContactPhone", settings.ContactPhone);
                            updateCommand.Parameters.AddWithValue("@HospitalEmail", settings.HospitalEmail);
                            updateCommand.Parameters.AddWithValue("@Website", settings.Website);
                            updateCommand.Parameters.AddWithValue("@LicenseNumber", settings.LicenseNumber);
                            updateCommand.Parameters.AddWithValue("@DefaultCurrency", settings.DefaultCurrency);
                            updateCommand.Parameters.AddWithValue("@TimeZone", settings.TimeZone);
                            updateCommand.Parameters.AddWithValue("@LogoPath", settings.LogoPath);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Step 2: If no record exists, insert a new one.
                        string insertSql = @"
                            INSERT INTO HospitalSettings (HospitalName, Address, ContactPhone, HospitalEmail, Website, LicenseNumber, DefaultCurrency, TimeZone, LogoPath)
                            VALUES (@HospitalName, @Address, @ContactPhone, @HospitalEmail, @Website, @LicenseNumber, @DefaultCurrency, @TimeZone, @LogoPath);";
                        using (SqlCommand insertCommand = new SqlCommand(insertSql, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@HospitalName", settings.HospitalName);
                            insertCommand.Parameters.AddWithValue("@Address", settings.Address);
                            insertCommand.Parameters.AddWithValue("@ContactPhone", settings.ContactPhone);
                            insertCommand.Parameters.AddWithValue("@HospitalEmail", settings.HospitalEmail);
                            insertCommand.Parameters.AddWithValue("@Website", settings.Website);
                            insertCommand.Parameters.AddWithValue("@LicenseNumber", settings.LicenseNumber);
                            insertCommand.Parameters.AddWithValue("@DefaultCurrency", settings.DefaultCurrency);
                            insertCommand.Parameters.AddWithValue("@TimeZone", settings.TimeZone);
                            insertCommand.Parameters.AddWithValue("@LogoPath", settings.LogoPath);
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }
    }
}
