using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class PatientManagementView : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        public ObservableCollection<Patient> Patients { get; set; }

        // Validation config
        private static readonly Regex CodeRegex = new Regex(@"^[A-Za-z0-9_-]{1,20}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly string[] AllowedBloodTypes = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        private const int NameMinLen = 2, NameMaxLen = 100;
        private const int AddressMinLen = 5, AddressMaxLen = 250;
        private const int MaxAllergies = 1000, MaxMedical = 1000;
        private const int MaxInsuranceProvider = 120, MaxInsuranceNumber = 80;

        public PatientManagementView()
        {
            InitializeComponent();
            Patients = new ObservableCollection<Patient>();
            this.DataContext = this;

            LoadPatientsFromDatabase();
        }

        // ------- UI Validation helpers -------
        private static void MarkInvalid(Control c, string tip)
        {
            if (c == null) return;
            c.BorderBrush = Brushes.Red;
            c.BorderThickness = new Thickness(1.5);
            c.ToolTip = tip;
        }
        private static void ClearInvalid(Control c)
        {
            if (c == null) return;
            c.ClearValue(Border.BorderBrushProperty);
            c.ClearValue(Border.BorderThicknessProperty);
            c.ToolTip = null;
        }
        private void ClearAllValidation()
        {
            ClearInvalid(txtPatientCode);
            ClearInvalid(txtFirstName);
            ClearInvalid(txtLastName);
            ClearInvalid(dpDOB);
            ClearInvalid(cmbGender);
            ClearInvalid(txtPhone);
            ClearInvalid(txtEmail);
            ClearInvalid(txtAddress);
            ClearInvalid(txtEmergencyContact);
            ClearInvalid(txtEmergencyPhone);
            ClearInvalid(txtInsuranceProvider);
            ClearInvalid(txtInsuranceNumber);
            ClearInvalid(txtBloodType);
            ClearInvalid(txtAllergies);
            ClearInvalid(txtMedicalConditions);
        }

        // ------- Load -------
        private async void LoadPatientsFromDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sqlQuery = @"
SELECT PatientID, PatientCode, FirstName, LastName, DOB, Gender, Phone, Email, Address, 
       EmergencyContact, EmergencyPhone, InsuranceProvider, InsuranceNumber, BloodType, Allergies, 
       MedicalConditions, CreatedDate, IsActive 
FROM Patients";
                    using (var command = new SqlCommand(sqlQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        Patients.Clear();
                        while (await reader.ReadAsync())
                        {
                            var p = new Patient
                            {
                                PatientID = SafeGetInt(reader, 0),
                                PatientCode = SafeGetString(reader, 1),
                                FirstName = SafeGetString(reader, 2),
                                LastName = SafeGetString(reader, 3),
                                DOB = SafeGetDate(reader, 4) ?? DateTime.Today,
                                Gender = SafeGetString(reader, 5),
                                Phone = SafeGetString(reader, 6),
                                Email = SafeGetString(reader, 7),
                                Address = SafeGetString(reader, 8),
                                EmergencyContact = SafeGetString(reader, 9),
                                EmergencyPhone = SafeGetString(reader, 10),
                                InsuranceProvider = SafeGetString(reader, 11),
                                InsuranceNumber = SafeGetString(reader, 12),
                                BloodType = SafeGetString(reader, 13),
                                Allergies = SafeGetString(reader, 14),
                                MedicalConditions = SafeGetString(reader, 15),
                                CreatedDate = SafeGetDate(reader, 16) ?? DateTime.Now,
                                IsActive = SafeGetBool(reader, 17)
                            };
                            Patients.Add(p);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load patients: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ------- Add -------
        private async void AddPatientButton_Click(object sender, RoutedEventArgs e)
        {
            ClearAllValidation();

            if (!TryBuildPatientFromInputs(out var toAdd, isUpdate: false)) return;

            try
            {
                toAdd.CreatedDate = DateTime.Now;
                int newId = await AddPatientToDatabase(toAdd); // get IDENTITY
                toAdd.PatientID = newId;

                Patients.Add(toAdd);
                MessageBox.Show($"Patient '{toAdd.FirstName} {toAdd.LastName}' added successfully.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while adding the patient: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ------- Update -------
        private async void UpdatePatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(PatientsDataGrid.SelectedItem is Patient selectedPatient))
            {
                MessageBox.Show("Please select a patient to update.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ClearAllValidation();

            if (!TryBuildPatientFromInputs(out var updatedCandidate, isUpdate: true, existingId: selectedPatient.PatientID))
                return;

            updatedCandidate.CreatedDate = selectedPatient.CreatedDate;

            try
            {
                await UpdatePatientInDatabase(updatedCandidate);

                CopyPatient(updatedCandidate, selectedPatient);

                MessageBox.Show($"Patient '{selectedPatient.FirstName} {selectedPatient.LastName}' has been updated.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while updating the patient: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ------- Delete -------
        private async void DeletePatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(PatientsDataGrid.SelectedItem is Patient selectedPatient))
            {
                MessageBox.Show("Please select a patient to delete.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete patient '{selectedPatient.FirstName} {selectedPatient.LastName}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await DeletePatientFromDatabase(selectedPatient);
                Patients.Remove(selectedPatient);
                MessageBox.Show($"Patient '{selectedPatient.FirstName} {selectedPatient.LastName}' has been deleted.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while deleting the patient: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ------- Search -------
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtSearch.Text, out var patientId))
            {
                MessageBox.Show("Please enter a valid Patient ID.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            await SearchPatientInDatabase(patientId);
        }

        // ------- Selection changed: populate form -------
        private void PatientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientsDataGrid.SelectedItem is Patient selectedPatient)
            {
                txtPatientCode.Text = selectedPatient.PatientCode;
                txtFirstName.Text = selectedPatient.FirstName;
                txtLastName.Text = selectedPatient.LastName;
                dpDOB.SelectedDate = selectedPatient.DOB;

                if (cmbGender.Items != null)
                {
                    foreach (ComboBoxItem item in cmbGender.Items)
                    {
                        if (item?.Content?.ToString() == selectedPatient.Gender)
                        {
                            cmbGender.SelectedItem = item; break;
                        }
                    }
                }

                txtPhone.Text = selectedPatient.Phone;
                txtEmail.Text = selectedPatient.Email;
                txtAddress.Text = selectedPatient.Address;
                txtEmergencyContact.Text = selectedPatient.EmergencyContact;
                txtEmergencyPhone.Text = selectedPatient.EmergencyPhone;
                txtInsuranceProvider.Text = selectedPatient.InsuranceProvider;
                txtInsuranceNumber.Text = selectedPatient.InsuranceNumber;
                txtBloodType.Text = selectedPatient.BloodType;
                txtAllergies.Text = selectedPatient.Allergies;
                txtMedicalConditions.Text = selectedPatient.MedicalConditions;
                chkIsActive.IsChecked = selectedPatient.IsActive;

                ClearAllValidation();
            }
        }

        // ------- Build + validate -------
        private bool TryBuildPatientFromInputs(out Patient patient, bool isUpdate, int existingId = 0)
        {
            patient = null;
            var sb = new StringBuilder();
            Control firstInvalid = null;

            string patientCode = (txtPatientCode.Text ?? "").Trim();
            string firstName = (txtFirstName.Text ?? "").Trim();
            string lastName = (txtLastName.Text ?? "").Trim();
            DateTime? dob = dpDOB.SelectedDate;
            string gender = ((cmbGender.SelectedItem as ComboBoxItem)?.Content ?? "").ToString();
            string phoneRaw = (txtPhone.Text ?? "").Trim();
            string email = (txtEmail.Text ?? "").Trim();
            string address = (txtAddress.Text ?? "").Trim();
            string emergencyContact = (txtEmergencyContact.Text ?? "").Trim();
            string emergencyPhoneRaw = (txtEmergencyPhone.Text ?? "").Trim();
            string insuranceProvider = (txtInsuranceProvider.Text ?? "").Trim();
            string insuranceNumber = (txtInsuranceNumber.Text ?? "").Trim();
            string bloodType = (txtBloodType.Text ?? "").Trim().ToUpperInvariant();
            string allergies = (txtAllergies.Text ?? "").Trim();
            string medicalConditions = (txtMedicalConditions.Text ?? "").Trim();
            bool isActive = chkIsActive.IsChecked ?? false;

            // PatientCode
            if (string.IsNullOrWhiteSpace(patientCode) || !CodeRegex.IsMatch(patientCode))
                AddErr("Patient Code is required (letters/digits/_/- up to 20).", txtPatientCode, ref firstInvalid, sb);

            // FirstName
            if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < NameMinLen || firstName.Length > NameMaxLen)
                AddErr($"First Name is required (length {NameMinLen}-{NameMaxLen}).", txtFirstName, ref firstInvalid, sb);

            // LastName
            if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < NameMinLen || lastName.Length > NameMaxLen)
                AddErr($"Last Name is required (length {NameMinLen}-{NameMaxLen}).", txtLastName, ref firstInvalid, sb);

            // DOB
            if (dob == null)
            {
                AddErr("Date of Birth is required.", dpDOB, ref firstInvalid, sb);
            }
            else
            {
                var today = DateTime.Today;
                if (dob.Value.Date > today)
                {
                    AddErr("Date of Birth cannot be in the future.", dpDOB, ref firstInvalid, sb);
                }
                else
                {
                    int age = today.Year - dob.Value.Year;
                    if (dob.Value.Date > today.AddYears(-age)) age--;
                    if (age < 0 || age > 130)
                        AddErr("Date of Birth seems invalid (age must be between 0 and 130).", dpDOB, ref firstInvalid, sb);
                }
            }

            // Gender
            if (string.IsNullOrWhiteSpace(gender))
                AddErr("Gender is required.", cmbGender, ref firstInvalid, sb);

            // Phone
            string phoneNormalized;
            string phoneError;
            if (!TryValidatePhoneNumber(phoneRaw, required: true, out phoneNormalized, out phoneError))
                AddErr(phoneError, txtPhone, ref firstInvalid, sb);

            // Email
            if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
                AddErr("Valid Email is required (e.g., name@host.tld).", txtEmail, ref firstInvalid, sb);

            // Address
            if (string.IsNullOrWhiteSpace(address) || address.Length < AddressMinLen || address.Length > AddressMaxLen)
                AddErr($"Address is required (length {AddressMinLen}-{AddressMaxLen}).", txtAddress, ref firstInvalid, sb);

            // Emergency phone (optional) with dependency
            string emergencyPhoneNormalized = null;
            if (!string.IsNullOrWhiteSpace(emergencyPhoneRaw))
            {
                string emErr;
                if (!TryValidatePhoneNumber(emergencyPhoneRaw, required: false, out emergencyPhoneNormalized, out emErr))
                    AddErr(emErr, txtEmergencyPhone, ref firstInvalid, sb);

                if (string.IsNullOrWhiteSpace(emergencyContact))
                    AddErr("Emergency Contact is required when Emergency Phone is provided.", txtEmergencyContact, ref firstInvalid, sb);
            }

            // Insurance (optional)
            if (insuranceProvider.Length > MaxInsuranceProvider)
                AddErr($"Insurance Provider is too long (max {MaxInsuranceProvider}).", txtInsuranceProvider, ref firstInvalid, sb);
            if (insuranceNumber.Length > MaxInsuranceNumber)
                AddErr($"Insurance Number is too long (max {MaxInsuranceNumber}).", txtInsuranceNumber, ref firstInvalid, sb);

            // Blood type (optional, must be valid if provided)
            if (!string.IsNullOrWhiteSpace(bloodType) && !AllowedBloodTypes.Contains(bloodType))
                AddErr("Blood Type must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.", txtBloodType, ref firstInvalid, sb);

            // Text caps
            if (allergies.Length > MaxAllergies)
                AddErr($"Allergies text is too long (max {MaxAllergies}).", txtAllergies, ref firstInvalid, sb);
            if (medicalConditions.Length > MaxMedical)
                AddErr($"Medical Conditions text is too long (max {MaxMedical}).", txtMedicalConditions, ref firstInvalid, sb);

            if (sb.Length > 0)
            {
                MessageBox.Show("Please fix the following:\n\n" + sb, "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                if (firstInvalid != null) firstInvalid.Focus();
                return false;
            }

            // Build Patient model (store normalized phone(s))
            patient = new Patient
            {
                PatientID = isUpdate ? existingId : 0,
                PatientCode = patientCode,
                FirstName = firstName,
                LastName = lastName,
                DOB = dob ?? DateTime.Today,
                Gender = gender,
                Phone = phoneNormalized,                   // normalized
                Email = email,
                Address = address,
                EmergencyContact = emergencyContact,
                EmergencyPhone = emergencyPhoneNormalized, // normalized or null
                InsuranceProvider = insuranceProvider,
                InsuranceNumber = insuranceNumber,
                BloodType = bloodType,
                Allergies = allergies,
                MedicalConditions = medicalConditions,
                // CreatedDate set during Add; preserved during Update
                IsActive = isActive
            };
            return true;
        }

        /// <summary>
        /// Phone validation:
        /// - Without country code: exactly 10 digits (format characters ignored).
        /// - With country code: must start with '+', then 1–3 digit country code, then 10-digit number.
        /// Returns normalized: "1234567890" or "+<cc><10digits>".
        /// </summary>
        private static bool TryValidatePhoneNumber(string raw, bool required, out string normalized, out string error)
        {
            normalized = null;
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                if (required)
                {
                    error = "Phone is required. Enter 10 digits (e.g., 9876543210) or include country code (e.g., +91 9876543210).";
                    return false;
                }
                return true; // optional empty OK
            }

            string trimmed = raw.Trim();
            bool hasPlus = trimmed.StartsWith("+");
            string digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());

            if (hasPlus)
            {
                // +<1-3 digits country code><10 digits>
                if (digitsOnly.Length < 11 || digitsOnly.Length > 13)
                {
                    error = "With country code, use +<1–3 digit country code> + 10-digit number (e.g., +1 5551234567, +91 9876543210).";
                    return false;
                }
                int ccLen = digitsOnly.Length - 10;
                if (ccLen < 1 || ccLen > 3)
                {
                    error = "Country code must be 1–3 digits followed by a 10-digit number (e.g., +44 7123456789).";
                    return false;
                }
                normalized = "+" + digitsOnly;
                return true;
            }
            else
            {
                if (digitsOnly.Length != 10)
                {
                    error = "Enter exactly 10 digits (e.g., 9876543210), or include a country code like +91 9876543210.";
                    return false;
                }
                normalized = digitsOnly;
                return true;
            }
        }

        private static void AddErr(string msg, Control control, ref Control first, StringBuilder sb)
        {
            sb.AppendLine("• " + msg);
            MarkInvalid(control, msg);
            if (first == null) first = control;
        }

        private static void CopyPatient(Patient src, Patient dst)
        {
            dst.PatientID = src.PatientID;
            dst.PatientCode = src.PatientCode;
            dst.FirstName = src.FirstName;
            dst.LastName = src.LastName;
            dst.DOB = src.DOB;
            dst.Gender = src.Gender;
            dst.Phone = src.Phone;
            dst.Email = src.Email;
            dst.Address = src.Address;
            dst.EmergencyContact = src.EmergencyContact;
            dst.EmergencyPhone = src.EmergencyPhone;
            dst.InsuranceProvider = src.InsuranceProvider;
            dst.InsuranceNumber = src.InsuranceNumber;
            dst.BloodType = src.BloodType;
            dst.Allergies = src.Allergies;
            dst.MedicalConditions = src.MedicalConditions;
            dst.CreatedDate = src.CreatedDate;
            dst.IsActive = src.IsActive;
        }

        // ------- DB Ops -------
        private async Task<int> AddPatientToDatabase(Patient patient)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
INSERT INTO Patients 
(PatientCode, FirstName, LastName, DOB, Gender, Phone, Email, Address, 
 EmergencyContact, EmergencyPhone, InsuranceProvider, InsuranceNumber, 
 BloodType, Allergies, MedicalConditions, CreatedDate, IsActive)
OUTPUT INSERTED.PatientID
VALUES 
(@PatientCode, @FirstName, @LastName, @DOB, @Gender, @Phone, @Email, @Address, 
 @EmergencyContact, @EmergencyPhone, @InsuranceProvider, @InsuranceNumber, 
 @BloodType, @Allergies, @MedicalConditions, @CreatedDate, @IsActive)";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    AddParams(cmd, patient);
                    var result = await cmd.ExecuteScalarAsync();
                    return (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                }
            }
        }

        private async Task UpdatePatientInDatabase(Patient patient)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
UPDATE Patients SET
    PatientCode=@PatientCode,
    FirstName=@FirstName,
    LastName=@LastName,
    DOB=@DOB,
    Gender=@Gender,
    Phone=@Phone,
    Email=@Email,
    Address=@Address,
    EmergencyContact=@EmergencyContact,
    EmergencyPhone=@EmergencyPhone,
    InsuranceProvider=@InsuranceProvider,
    InsuranceNumber=@InsuranceNumber,
    BloodType=@BloodType,
    Allergies=@Allergies,
    MedicalConditions=@MedicalConditions,
    IsActive=@IsActive
WHERE PatientID=@PatientID";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    AddParams(cmd, patient);
                    cmd.Parameters.AddWithValue("@PatientID", patient.PatientID);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task DeletePatientFromDatabase(Patient patient)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Patients WHERE PatientID=@PatientID";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@PatientID", patient.PatientID);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task SearchPatientInDatabase(int patientId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sqlQuery = @"
SELECT PatientID, PatientCode, FirstName, LastName, DOB, Gender, Phone, Email, Address, 
       EmergencyContact, EmergencyPhone, InsuranceProvider, InsuranceNumber, BloodType, Allergies, 
       MedicalConditions, CreatedDate, IsActive 
FROM Patients WHERE PatientID=@PatientID";
                    using (var cmd = new SqlCommand(sqlQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            Patients.Clear();
                            if (await reader.ReadAsync())
                            {
                                var p = new Patient
                                {
                                    PatientID = SafeGetInt(reader, 0),
                                    PatientCode = SafeGetString(reader, 1),
                                    FirstName = SafeGetString(reader, 2),
                                    LastName = SafeGetString(reader, 3),
                                    DOB = SafeGetDate(reader, 4) ?? DateTime.Today,
                                    Gender = SafeGetString(reader, 5),
                                    Phone = SafeGetString(reader, 6),
                                    Email = SafeGetString(reader, 7),
                                    Address = SafeGetString(reader, 8),
                                    EmergencyContact = SafeGetString(reader, 9),
                                    EmergencyPhone = SafeGetString(reader, 10),
                                    InsuranceProvider = SafeGetString(reader, 11),
                                    InsuranceNumber = SafeGetString(reader, 12),
                                    BloodType = SafeGetString(reader, 13),
                                    Allergies = SafeGetString(reader, 14),
                                    MedicalConditions = SafeGetString(reader, 15),
                                    CreatedDate = SafeGetDate(reader, 16) ?? DateTime.Now,
                                    IsActive = SafeGetBool(reader, 17)
                                };
                                Patients.Add(p);
                            }
                            else
                            {
                                MessageBox.Show("Patient not found.", "Search Result",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to search for patient: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void AddParams(SqlCommand cmd, Patient p)
        {
            cmd.Parameters.AddWithValue("@PatientCode", (object)NullIfEmpty(p.PatientCode) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FirstName", (object)NullIfEmpty(p.FirstName) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LastName", (object)NullIfEmpty(p.LastName) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DOB", p.DOB);
            cmd.Parameters.AddWithValue("@Gender", (object)NullIfEmpty(p.Gender) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", (object)NullIfEmpty(p.Phone) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object)NullIfEmpty(p.Email) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object)NullIfEmpty(p.Address) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmergencyContact", (object)NullIfEmpty(p.EmergencyContact) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmergencyPhone", (object)NullIfEmpty(p.EmergencyPhone) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InsuranceProvider", (object)NullIfEmpty(p.InsuranceProvider) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InsuranceNumber", (object)NullIfEmpty(p.InsuranceNumber) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BloodType", (object)NullIfEmpty(p.BloodType) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Allergies", (object)NullIfEmpty(p.Allergies) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MedicalConditions", (object)NullIfEmpty(p.MedicalConditions) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedDate", p.CreatedDate);
            cmd.Parameters.AddWithValue("@IsActive", p.IsActive);
        }

        private static string NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

        private static int SafeGetInt(SqlDataReader r, int i) => r.IsDBNull(i) ? 0 : r.GetInt32(i);
        private static string SafeGetString(SqlDataReader r, int i) => r.IsDBNull(i) ? "" : r.GetString(i);
        private static DateTime? SafeGetDate(SqlDataReader r, int i) => r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i);
        private static bool SafeGetBool(SqlDataReader r, int i) => !r.IsDBNull(i) && r.GetBoolean(i);
    }
}
