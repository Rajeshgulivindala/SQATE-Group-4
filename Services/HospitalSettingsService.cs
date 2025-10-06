using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace HospitalManagementSystem.Services
{
    public class HospitalSettingsModel
    {
        public int SettingID { get; set; }
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

    public sealed class HospitalSettingsService
    {
        private static readonly Lazy<HospitalSettingsService> _lazy =
            new Lazy<HospitalSettingsService>(() => new HospitalSettingsService());
        public static HospitalSettingsService Instance => _lazy.Value;

        private HospitalSettingsService() { }

        private string _connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True";

        public HospitalSettingsModel Current { get; private set; }
        public event EventHandler<HospitalSettingsModel> SettingsChanged;

        public void SetConnectionString(string connectionString)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
                _connectionString = connectionString;
        }

        public async Task<HospitalSettingsModel> LoadAsync()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                const string sql = "SELECT TOP 1 * FROM HospitalSettings ORDER BY SettingID ASC";
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        Current = new HospitalSettingsModel
                        {
                            SettingID = reader["SettingID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SettingID"]),
                            HospitalName = reader["HospitalName"] == DBNull.Value ? null : reader["HospitalName"].ToString(),
                            Address = reader["Address"] == DBNull.Value ? null : reader["Address"].ToString(),
                            ContactPhone = reader["ContactPhone"] == DBNull.Value ? null : reader["ContactPhone"].ToString(),
                            HospitalEmail = reader["HospitalEmail"] == DBNull.Value ? null : reader["HospitalEmail"].ToString(),
                            Website = reader["Website"] == DBNull.Value ? null : reader["Website"].ToString(),
                            LicenseNumber = reader["LicenseNumber"] == DBNull.Value ? null : reader["LicenseNumber"].ToString(),
                            DefaultCurrency = reader["DefaultCurrency"] == DBNull.Value ? null : reader["DefaultCurrency"].ToString(),
                            TimeZone = reader["TimeZone"] == DBNull.Value ? null : reader["TimeZone"].ToString(),
                            LogoPath = reader["LogoPath"] == DBNull.Value ? null : reader["LogoPath"].ToString()
                        };
                        return Current;
                    }
                }
            }
            Current = null;
            return null;
        }

        public async Task SaveAsync(HospitalSettingsModel settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                int? existingId = null;
                using (var check = new SqlCommand("SELECT TOP 1 SettingID FROM HospitalSettings ORDER BY SettingID ASC", conn))
                {
                    var obj = await check.ExecuteScalarAsync();
                    if (obj != null && obj != DBNull.Value) existingId = Convert.ToInt32(obj);
                }

                if (existingId.HasValue)
                {
                    const string updateSql = @"
UPDATE HospitalSettings
SET HospitalName=@HospitalName, Address=@Address, ContactPhone=@ContactPhone,
    HospitalEmail=@HospitalEmail, Website=@Website, LicenseNumber=@LicenseNumber,
    DefaultCurrency=@DefaultCurrency, TimeZone=@TimeZone, LogoPath=@LogoPath
WHERE SettingID=@SettingID;";
                    using (var cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@SettingID", existingId.Value);
                        cmd.Parameters.AddWithValue("@HospitalName", (object)settings.HospitalName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object)settings.Address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ContactPhone", (object)settings.ContactPhone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HospitalEmail", (object)settings.HospitalEmail ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Website", (object)settings.Website ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LicenseNumber", (object)settings.LicenseNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DefaultCurrency", (object)settings.DefaultCurrency ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TimeZone", (object)settings.TimeZone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LogoPath", (object)settings.LogoPath ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                        settings.SettingID = existingId.Value;
                    }
                }
                else
                {
                    const string insertSql = @"
INSERT INTO HospitalSettings
(HospitalName, Address, ContactPhone, HospitalEmail, Website, LicenseNumber, DefaultCurrency, TimeZone, LogoPath)
OUTPUT INSERTED.SettingID
VALUES
(@HospitalName, @Address, @ContactPhone, @HospitalEmail, @Website, @LicenseNumber, @DefaultCurrency, @TimeZone, @LogoPath);";
                    using (var cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@HospitalName", (object)settings.HospitalName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object)settings.Address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ContactPhone", (object)settings.ContactPhone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HospitalEmail", (object)settings.HospitalEmail ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Website", (object)settings.Website ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LicenseNumber", (object)settings.LicenseNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DefaultCurrency", (object)settings.DefaultCurrency ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TimeZone", (object)settings.TimeZone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LogoPath", (object)settings.LogoPath ?? DBNull.Value);

                        var newId = (int)await cmd.ExecuteScalarAsync();
                        settings.SettingID = newId;
                    }
                }
            }

            Current = settings;
            SettingsChanged?.Invoke(this, Current);
        }
    }
}
