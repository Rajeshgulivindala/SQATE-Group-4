using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
// If you already reference BCrypt.Net-Next via NuGet, uncomment:
// using BCrypt.Net;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class RegisterUserView : UserControl
    {
        private const string ConnStr =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public RegisterUserView()
        {
            InitializeComponent();
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var username = (TxtUsername.Text ?? "").Trim();
            var email = (TxtEmail.Text ?? "").Trim();
            var password = (PwdBox.Password ?? "").Trim();
            var role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Username is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Email is required (needed to link Staff).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show("Please choose a role.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var cn = new SqlConnection(ConnStr))
                {
                    await cn.OpenAsync();

                    // Make sure Users table/columns exist (idempotent)
                    await EnsureUsersSchemaAsync(cn);

                    // Enforce uniqueness on Username/Email
                    if (await ExistsAsync(cn, "SELECT 1 FROM dbo.Users WHERE Username=@u", ("@u", username)))
                    {
                        MessageBox.Show("Username already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (await ExistsAsync(cn, "SELECT 1 FROM dbo.Users WHERE Email=@e", ("@e", email)))
                    {
                        MessageBox.Show("Email already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Hash password if BCrypt available; otherwise store as-is (you can replace later)
                    string passwordHash;
                    try
                    {
                        // passwordHash = BCrypt.HashPassword(password); // if BCrypt referenced
                        passwordHash = password; // fallback if no BCrypt yet
                    }
                    catch
                    {
                        passwordHash = password; // fallback
                    }

                    var sql = @"
INSERT INTO dbo.Users(Username, PasswordHash, Role, Email, IsActive, CreatedDate)
VALUES (@u, @p, @r, @e, 1, SYSUTCDATETIME());";

                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@p", passwordHash);
                        cmd.Parameters.AddWithValue("@r", role);
                        cmd.Parameters.AddWithValue("@e", email);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("User registered.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear form
                TxtUsername.Clear();
                TxtEmail.Clear();
                PwdBox.Clear();
                CmbRole.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to register user: " + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task EnsureUsersSchemaAsync(SqlConnection cn)
        {
            // Create Users table if missing; add Email if missing (safe to run repeatedly)
            const string sql = @"
IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserID       INT IDENTITY(1,1) PRIMARY KEY,
        Username     NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(200) NOT NULL,
        Role         NVARCHAR(50)  NOT NULL,
        IsActive     BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),
        CreatedDate  DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
END;

IF COL_LENGTH('dbo.Users','Email') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD Email NVARCHAR(100) NULL;
    -- Optional: unique index if you want to enforce unique emails
    -- CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email) WHERE Email IS NOT NULL;
END;
";
            using (var cmd = new SqlCommand(sql, cn))
                await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<bool> ExistsAsync(SqlConnection cn, string sql, params (string, object)[] p)
        {
            using (var cmd = new SqlCommand(sql, cn))
            {
                foreach (var (name, val) in p)
                    cmd.Parameters.AddWithValue(name, val ?? DBNull.Value);
                var o = await cmd.ExecuteScalarAsync();
                return o != null;
            }
        }
    }
}
