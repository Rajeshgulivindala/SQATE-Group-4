using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class PatientManagementView : UserControl
    {
        // TODO: **CRITICAL: Replace this with your actual database connection string.**
        private readonly string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        // An ObservableCollection for the patients, which automatically updates the UI.
        public ObservableCollection<Patient> Patients { get; set; }

        public PatientManagementView()
        {
            InitializeComponent();
            Patients = new ObservableCollection<Patient>();

            // Set the DataContext to this instance to enable XAML binding.
            this.DataContext = this;

            // Load patients when the view is initialized.
            LoadPatientsFromDatabase();
        }

        /// <summary>
        /// Loads patient data from the database.
        /// </summary>
        private async void LoadPatientsFromDatabase()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sqlQuery = "SELECT PatientID, PatientCode, FirstName, LastName, DOB, Gender, Phone, Email, Address, EmergencyContact, EmergencyPhone, InsuranceProvider, InsuranceNumber, BloodType, Allergies, MedicalConditions, CreatedDate, IsActive FROM Patients";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            Patients.Clear();
                            while (await reader.ReadAsync())
                            {
                                var patient = new Patient
                                {
                                    PatientID = reader.GetInt32(0),
                                    PatientCode = reader.GetString(1),
                                    FirstName = reader.GetString(2),
                                    LastName = reader.GetString(3),
                                    DOB = reader.GetDateTime(4),
                                    Gender = reader.GetString(5),
                                    Phone = reader.GetString(6),
                                    Email = reader.GetString(7),
                                    Address = reader.GetString(8),
                                    EmergencyContact = reader.GetString(9),
                                    EmergencyPhone = reader.GetString(10),
                                    InsuranceProvider = reader.GetString(11),
                                    InsuranceNumber = reader.GetString(12),
                                    BloodType = reader.GetString(13),
                                    Allergies = reader.GetString(14),
                                    MedicalConditions = reader.GetString(15),
                                    CreatedDate = reader.GetDateTime(16),
                                    IsActive = reader.GetBoolean(17)
                                };
                                Patients.Add(patient);
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
                MessageBox.Show($"Failed to load patients: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the "Add Patient" button.
        /// </summary>
        private async void AddPatientButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a new Patient object from the UI input
                var newPatient = new Patient
                {
                    PatientCode = txtPatientCode.Text,
                    FirstName = txtFirstName.Text,
                    LastName = txtLastName.Text,
                    DOB = dpDOB.SelectedDate ?? DateTime.Now,
                    Gender = ((ComboBoxItem)cmbGender.SelectedItem)?.Content.ToString(),
                    Phone = txtPhone.Text,
                    Email = txtEmail.Text,
                    Address = txtAddress.Text,
                    EmergencyContact = txtEmergencyContact.Text,
                    EmergencyPhone = txtEmergencyPhone.Text,
                    InsuranceProvider = txtInsuranceProvider.Text,
                    InsuranceNumber = txtInsuranceNumber.Text,
                    BloodType = txtBloodType.Text,
                    Allergies = txtAllergies.Text,
                    MedicalConditions = txtMedicalConditions.Text,
                    CreatedDate = DateTime.Now,
                    IsActive = chkIsActive.IsChecked ?? false
                };

                await AddPatientToDatabase(newPatient);
                Patients.Add(newPatient); // Add to ObservableCollection for UI update
                MessageBox.Show($"Patient '{newPatient.FirstName} {newPatient.LastName}' added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while adding the patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the "Update Patient" button.
        /// </summary>
        private async void UpdatePatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (PatientsDataGrid.SelectedItem is Patient selectedPatient)
            {
                try
                {
                    // Update the selectedPatient object with values from your UI controls.
                    selectedPatient.PatientCode = txtPatientCode.Text;
                    selectedPatient.FirstName = txtFirstName.Text;
                    selectedPatient.LastName = txtLastName.Text;
                    selectedPatient.DOB = dpDOB.SelectedDate ?? DateTime.Now;
                    selectedPatient.Gender = ((ComboBoxItem)cmbGender.SelectedItem)?.Content.ToString();
                    selectedPatient.Phone = txtPhone.Text;
                    selectedPatient.Email = txtEmail.Text;
                    selectedPatient.Address = txtAddress.Text;
                    selectedPatient.EmergencyContact = txtEmergencyContact.Text;
                    selectedPatient.EmergencyPhone = txtEmergencyPhone.Text;
                    selectedPatient.InsuranceProvider = txtInsuranceProvider.Text;
                    selectedPatient.InsuranceNumber = txtInsuranceNumber.Text;
                    selectedPatient.BloodType = txtBloodType.Text;
                    selectedPatient.Allergies = txtAllergies.Text;
                    selectedPatient.MedicalConditions = txtMedicalConditions.Text;
                    selectedPatient.IsActive = chkIsActive.IsChecked ?? false;

                    await UpdatePatientInDatabase(selectedPatient);
                    MessageBox.Show($"Patient '{selectedPatient.FirstName} {selectedPatient.LastName}' has been updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while updating the patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a patient to update.", "No Patient Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the click event for the "Delete Patient" button.
        /// </summary>
        private async void DeletePatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (PatientsDataGrid.SelectedItem is Patient selectedPatient)
            {
                try
                {
                    await DeletePatientFromDatabase(selectedPatient);
                    Patients.Remove(selectedPatient);
                    MessageBox.Show($"Patient '{selectedPatient.FirstName} {selectedPatient.LastName}' has been deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while deleting the patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a patient to delete.", "No Patient Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the click event for the "Search" button.
        /// </summary>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSearch.Text, out int patientId))
            {
                await SearchPatientInDatabase(patientId);
            }
            else
            {
                MessageBox.Show("Please enter a valid Patient ID.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the selection change event of the DataGrid.
        /// </summary>
        private void PatientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientsDataGrid.SelectedItem is Patient selectedPatient)
            {
                txtPatientCode.Text = selectedPatient.PatientCode;
                txtFirstName.Text = selectedPatient.FirstName;
                txtLastName.Text = selectedPatient.LastName;
                dpDOB.SelectedDate = selectedPatient.DOB;
                // Find and select the ComboBoxItem for the Gender
                foreach (ComboBoxItem item in cmbGender.Items)
                {
                    if (item.Content.ToString() == selectedPatient.Gender)
                    {
                        cmbGender.SelectedItem = item;
                        break;
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
            }
        }

        /// <summary>
        /// Adds a patient to the database.
        /// </summary>
        private async Task AddPatientToDatabase(Patient patient)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO Patients (PatientCode, FirstName, LastName, DOB, Gender, Phone, Email, Address, EmergencyContact, EmergencyPhone, InsuranceProvider, InsuranceNumber, BloodType, Allergies, MedicalConditions, CreatedDate, IsActive)
                               VALUES (@PatientCode, @FirstName, @LastName, @DOB, @Gender, @Phone, @Email, @Address, @EmergencyContact, @EmergencyPhone, @InsuranceProvider, @InsuranceNumber, @BloodType, @Allergies, @MedicalConditions, @CreatedDate, @IsActive)";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PatientCode", patient.PatientCode);
                    command.Parameters.AddWithValue("@FirstName", patient.FirstName);
                    command.Parameters.AddWithValue("@LastName", patient.LastName);
                    command.Parameters.AddWithValue("@DOB", patient.DOB);
                    command.Parameters.AddWithValue("@Gender", patient.Gender);
                    command.Parameters.AddWithValue("@Phone", patient.Phone);
                    command.Parameters.AddWithValue("@Email", patient.Email);
                    command.Parameters.AddWithValue("@Address", patient.Address);
                    command.Parameters.AddWithValue("@EmergencyContact", patient.EmergencyContact);
                    command.Parameters.AddWithValue("@EmergencyPhone", patient.EmergencyPhone);
                    command.Parameters.AddWithValue("@InsuranceProvider", patient.InsuranceProvider);
                    command.Parameters.AddWithValue("@InsuranceNumber", patient.InsuranceNumber);
                    command.Parameters.AddWithValue("@BloodType", patient.BloodType);
                    command.Parameters.AddWithValue("@Allergies", patient.Allergies);
                    command.Parameters.AddWithValue("@MedicalConditions", patient.MedicalConditions);
                    command.Parameters.AddWithValue("@CreatedDate", patient.CreatedDate);
                    command.Parameters.AddWithValue("@IsActive", patient.IsActive);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Updates a patient in the database.
        /// </summary>
        private async Task UpdatePatientInDatabase(Patient patient)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Patients
                               SET PatientCode = @PatientCode,
                                   FirstName = @FirstName,
                                   LastName = @LastName,
                                   DOB = @DOB,
                                   Gender = @Gender,
                                   Phone = @Phone,
                                   Email = @Email,
                                   Address = @Address,
                                   EmergencyContact = @EmergencyContact,
                                   EmergencyPhone = @EmergencyPhone,
                                   InsuranceProvider = @InsuranceProvider,
                                   InsuranceNumber = @InsuranceNumber,
                                   BloodType = @BloodType,
                                   Allergies = @Allergies,
                                   MedicalConditions = @MedicalConditions,
                                   IsActive = @IsActive
                               WHERE PatientID = @PatientID";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PatientCode", patient.PatientCode);
                    command.Parameters.AddWithValue("@FirstName", patient.FirstName);
                    command.Parameters.AddWithValue("@LastName", patient.LastName);
                    command.Parameters.AddWithValue("@DOB", patient.DOB);
                    command.Parameters.AddWithValue("@Gender", patient.Gender);
                    command.Parameters.AddWithValue("@Phone", patient.Phone);
                    command.Parameters.AddWithValue("@Email", patient.Email);
                    command.Parameters.AddWithValue("@Address", patient.Address);
                    command.Parameters.AddWithValue("@EmergencyContact", patient.EmergencyContact);
                    command.Parameters.AddWithValue("@EmergencyPhone", patient.EmergencyPhone);
                    command.Parameters.AddWithValue("@InsuranceProvider", patient.InsuranceProvider);
                    command.Parameters.AddWithValue("@InsuranceNumber", patient.InsuranceNumber);
                    command.Parameters.AddWithValue("@BloodType", patient.BloodType);
                    command.Parameters.AddWithValue("@Allergies", patient.Allergies);
                    command.Parameters.AddWithValue("@MedicalConditions", patient.MedicalConditions);
                    command.Parameters.AddWithValue("@IsActive", patient.IsActive);
                    command.Parameters.AddWithValue("@PatientID", patient.PatientID);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Deletes a patient from the database.
        /// </summary>
        private async Task DeletePatientFromDatabase(Patient patient)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Patients WHERE PatientID = @PatientID";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PatientID", patient.PatientID);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Searches for a patient in the database by their ID.
        /// </summary>
        private async Task SearchPatientInDatabase(int patientId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sqlQuery = "SELECT PatientID, PatientCode, FirstName, LastName, DOB, Gender, Phone, Email, Address, EmergencyContact, EmergencyPhone, InsuranceProvider, InsuranceNumber, BloodType, Allergies, MedicalConditions, CreatedDate, IsActive FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        command.Parameters.AddWithValue("@PatientID", patientId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            Patients.Clear();
                            if (await reader.ReadAsync())
                            {
                                var patient = new Patient
                                {
                                    PatientID = reader.GetInt32(0),
                                    PatientCode = reader.GetString(1),
                                    FirstName = reader.GetString(2),
                                    LastName = reader.GetString(3),
                                    DOB = reader.GetDateTime(4),
                                    Gender = reader.GetString(5),
                                    Phone = reader.GetString(6),
                                    Email = reader.GetString(7),
                                    Address = reader.GetString(8),
                                    EmergencyContact = reader.GetString(9),
                                    EmergencyPhone = reader.GetString(10),
                                    InsuranceProvider = reader.GetString(11),
                                    InsuranceNumber = reader.GetString(12),
                                    BloodType = reader.GetString(13),
                                    Allergies = reader.GetString(14),
                                    MedicalConditions = reader.GetString(15),
                                    CreatedDate = reader.GetDateTime(16),
                                    IsActive = reader.GetBoolean(17)
                                };
                                Patients.Add(patient);
                            }
                            else
                            {
                                MessageBox.Show("Patient not found.", "Search Result", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show($"Failed to search for patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
