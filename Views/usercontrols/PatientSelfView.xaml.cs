using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Services.Authentication;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class PatientSelfView : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public PatientSelfView()
        {
            InitializeComponent();
            _ = LoadMyProfileAsync();
        }

        private sealed class UserRow
        {
            public int UserId;
            public string Username;
        }

        private void ClearLabels()
        {
            string dash = "—";
            TxtCode.Text = TxtFirstName.Text = TxtLastName.Text = TxtDOB.Text =
            TxtGender.Text = TxtPhone.Text = TxtEmail.Text = TxtAddress.Text =
            TxtInsurance.Text = TxtBloodType.Text = TxtAllergies.Text = dash;
        }

        private static bool HasColumn(IDataRecord r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (r.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string GetString(IDataRecord r, params string[] names)
        {
            foreach (var n in names)
            {
                if (HasColumn(r, n))
                {
                    var v = r[n];
                    if (v != DBNull.Value) return v.ToString();
                }
            }
            return null;
        }

        private static DateTime? GetDate(IDataRecord r, params string[] names)
        {
            foreach (var n in names)
            {
                if (HasColumn(r, n))
                {
                    var v = r[n];
                    if (v != DBNull.Value)
                    {
                        if (v is DateTime dt) return dt;
                        if (DateTime.TryParse(v.ToString(), out var parsed)) return parsed;
                    }
                }
            }
            return null;
        }

        private async Task<bool> ColumnExistsAsync(SqlConnection con, string table, string column)
        {
            using (var cmd = new SqlCommand(
                @"SELECT 1 FROM sys.columns c 
                  JOIN sys.objects o ON o.object_id = c.object_id
                  WHERE o.type='U' AND o.name=@t AND c.name=@c", con))
            {
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@c", column);
                var result = await cmd.ExecuteScalarAsync();
                return result != null;
            }
        }

        private async Task LoadMyProfileAsync()
        {
            TxtInfo.Text = "Loading your profile…";
            ClearLabels();

            var current = AuthenticationService.CurrentUser;
            var username = current?.Username ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username))
            {
                TxtInfo.Text = "Not signed in.";
                return;
            }

            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    // 1) Resolve UserID from Users by Username (NO Email here)
                    UserRow user = null;
                    using (var userCmd = new SqlCommand(
                        @"SELECT TOP(1) UserID, Username 
                          FROM dbo.Users 
                          WHERE Username = @u", con))
                    {
                        userCmd.Parameters.AddWithValue("@u", username);
                        using (var r = await userCmd.ExecuteReaderAsync(CommandBehavior.SingleRow))
                        {
                            if (await r.ReadAsync())
                            {
                                user = new UserRow
                                {
                                    UserId = Convert.ToInt32(r["UserID"]),
                                    Username = r["Username"]?.ToString()
                                };
                            }
                        }
                    }

                    if (user == null)
                    {
                        TxtInfo.Text = "No linked user record found.";
                        return;
                    }

                    // 2) Build WHERE strategy for Patients:
                    //    Prefer Patients.UserID if it exists; otherwise try Patients.Username
                    string whereClause;
                    SqlCommand pCmd;

                    bool hasUserId = await ColumnExistsAsync(con, "Patients", "UserID");
                    if (hasUserId)
                    {
                        whereClause = "UserID = @uid";
                        pCmd = new SqlCommand($@"SELECT TOP(1) * FROM dbo.Patients WHERE {whereClause}", con);
                        pCmd.Parameters.AddWithValue("@uid", user.UserId);
                    }
                    else
                    {
                        // Fall back to username match if Patients.Username exists
                        bool hasUsername = await ColumnExistsAsync(con, "Patients", "Username");
                        if (!hasUsername)
                        {
                            TxtInfo.Text = "Patient linking not found (no Patients.UserID or Patients.Username). Ask your doctor to link your profile.";
                            return;
                        }

                        whereClause = "Username = @uname";
                        pCmd = new SqlCommand($@"SELECT TOP(1) * FROM dbo.Patients WHERE {whereClause}", con);
                        pCmd.Parameters.AddWithValue("@uname", user.Username);
                    }

                    using (var pr = await pCmd.ExecuteReaderAsync(CommandBehavior.SingleRow))
                    {
                        if (!await pr.ReadAsync())
                        {
                            TxtInfo.Text = "No patient profile found. (Ask your doctor to create it.)";
                            return;
                        }

                        // Read with aliases to tolerate schema differences
                        TxtCode.Text = GetString(pr, "PatientCode", "Code") ?? "—";
                        TxtFirstName.Text = GetString(pr, "FirstName", "GivenName") ?? "—";
                        TxtLastName.Text = GetString(pr, "LastName", "Surname", "FamilyName") ?? "—";

                        var dob = GetDate(pr, "DOB", "DateOfBirth", "BirthDate");
                        TxtDOB.Text = dob.HasValue ? dob.Value.ToShortDateString() : "—";

                        TxtGender.Text = GetString(pr, "Gender", "Sex") ?? "—";
                        TxtPhone.Text = GetString(pr, "Phone", "PhoneNumber", "Mobile") ?? "—";
                        TxtEmail.Text = GetString(pr, "Email", "EmailAddress") ?? "—";
                        TxtAddress.Text = GetString(pr, "Address", "StreetAddress") ?? "—";
                        TxtInsurance.Text = GetString(pr, "InsuranceProvider", "InsuranceCompany") ?? "—";
                        TxtBloodType.Text = GetString(pr, "BloodType", "BloodGroup") ?? "—";
                        TxtAllergies.Text = GetString(pr, "Allergies", "AllergyNotes") ?? "—";
                    }

                    TxtInfo.Text = "Loaded.";
                }
            }
            catch (Exception ex)
            {
                TxtInfo.Text = $"Error: {ex.Message}";
#if DEBUG
                MessageBox.Show(ex.ToString(), "PatientSelfView error");
#endif
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadMyProfileAsync();
        }
    }
}
