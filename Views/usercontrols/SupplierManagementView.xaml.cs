using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Represents a supplier data model.
    /// The 'SupplierId' is assumed to be identity/auto-numbered by the DB.
    /// </summary>
    public class Supplier
    {
        public int SupplierId { get; set; }
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PaymentTerms { get; set; }
        public int? Rating { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Interaction logic for SupplierManagementView.xaml
    /// </summary>
    public partial class SupplierManagementView : UserControl
    {
        // TODO: Replace with your actual connection string.
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        private readonly ObservableCollection<Supplier> suppliers = new ObservableCollection<Supplier>();

        // -------- Validation config --------
        private const int MaxCompanyLen = 150;
        private const int MaxPersonLen = 120;
        private const int MaxPhoneLen = 20;   // after stripping spaces/symbols
        private const int MaxEmailLen = 254;
        private const int MaxAddressLen = 200;
        private const int MaxCityLen = 100;
        private const int MaxCountryLen = 100;
        private const int MaxTermsLen = 120;
        private const int MaxNotesLen = 1000;
        private const int MinPhoneDigits = 7;
        private const int MaxPhoneDigits = 15; // E.164

        private static readonly Regex EmailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PhoneAllowedChars =
            new Regex(@"^[0-9+\-\s()]+$", RegexOptions.Compiled);

        public SupplierManagementView()
        {
            InitializeComponent();
            SuppliersDataGrid.ItemsSource = suppliers; // set directly; XAML does not bind ItemsSource
            Loaded += SupplierManagementView_Loaded;
        }

        private async void SupplierManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSuppliersAsync();
        }

        private async Task LoadSuppliersAsync()
        {
            suppliers.Clear();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sqlQuery =
                        @"SELECT SupplierId, CompanyName, ContactPerson, Phone, Email, Address, City, Country,
                                 PaymentTerms, Rating, IsActive, CreatedDate, Notes
                          FROM Suppliers
                          ORDER BY SupplierId DESC;";
                    using (var command = new SqlCommand(sqlQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Safe conversion helpers
                            object V(int i) => reader.IsDBNull(i) ? null : reader.GetValue(i);
                            string S(int i) => V(i) == null ? null : Convert.ToString(V(i), CultureInfo.InvariantCulture);
                            int? I(int i) => V(i) == null ? (int?)null : Convert.ToInt32(V(i), CultureInfo.InvariantCulture);
                            bool B(int i)
                            {
                                var o = V(i);
                                if (o == null) return false;
                                // Handles BIT, numeric 0/1, strings "true"/"false"
                                return Convert.ToBoolean(o, CultureInfo.InvariantCulture);
                            }
                            DateTime D(int i) => V(i) == null ? DateTime.MinValue : Convert.ToDateTime(V(i), CultureInfo.InvariantCulture);

                            suppliers.Add(new Supplier
                            {
                                // Works for INT/SMALLINT/TINYINT
                                SupplierId = Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                                CompanyName = S(1),
                                ContactPerson = S(2),
                                Phone = S(3),
                                Email = S(4),
                                Address = S(5),
                                City = S(6),
                                Country = S(7),
                                PaymentTerms = S(8),
                                Rating = I(9),
                                IsActive = B(10),
                                CreatedDate = D(11),
                                Notes = S(12)
                            });
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
                MessageBox.Show("Failed to load suppliers: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------- Add / Update / Delete ----------------

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ClearValidationHighlights();
            NormalizeInputs();

            var vr = await ValidateFormAsync(null);
            if (!vr.Ok)
            {
                MessageBox.Show("Please fix the following:\n\n" + vr.Summary, "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sql = @"
INSERT INTO Suppliers
 (CompanyName, ContactPerson, Phone, Email, Address, City, Country, PaymentTerms, Rating, IsActive, CreatedDate, Notes)
VALUES
 (@CompanyName, @ContactPerson, @Phone, @Email, @Address, @City, @Country, @PaymentTerms, @Rating, @IsActive, GETDATE(), @Notes);";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Supplier added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSuppliersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to add supplier: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            var selected = SuppliersDataGrid.SelectedItem as Supplier;
            if (selected == null) return;

            ClearValidationHighlights();
            NormalizeInputs();

            var vr = await ValidateFormAsync(selected.SupplierId);
            if (!vr.Ok)
            {
                MessageBox.Show("Please fix the following:\n\n" + vr.Summary, "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sql = @"
UPDATE Suppliers
   SET CompanyName=@CompanyName, ContactPerson=@ContactPerson, Phone=@Phone, Email=@Email,
       Address=@Address, City=@City, Country=@Country, PaymentTerms=@PaymentTerms,
       Rating=@Rating, IsActive=@IsActive, Notes=@Notes
 WHERE SupplierId=@SupplierId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        AddParameters(command);
                        command.Parameters.AddWithValue("@SupplierId", selected.SupplierId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Supplier updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSuppliersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update supplier: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = SuppliersDataGrid.SelectedItem as Supplier;
            if (selected == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete {selected.CompanyName}?",
                                          "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    const string sql = "DELETE FROM Suppliers WHERE SupplierId = @SupplierId;";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@SupplierId", selected.SupplierId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Supplier deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSuppliersAsync();
                ClearForm();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete supplier: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------- Selection / Clear ----------------

        private void SuppliersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearValidationHighlights();

            var s = SuppliersDataGrid.SelectedItem as Supplier;
            if (s != null)
            {
                txtSupplierId.Text = s.SupplierId.ToString(CultureInfo.InvariantCulture);
                txtCompanyName.Text = s.CompanyName;
                txtContactPerson.Text = s.ContactPerson;
                txtPhone.Text = s.Phone;
                txtEmail.Text = s.Email;
                txtAddress.Text = s.Address;
                txtCity.Text = s.City;
                txtCountry.Text = s.Country;
                txtPaymentTerms.Text = s.PaymentTerms;
                txtRating.Text = s.Rating.HasValue ? s.Rating.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
                chkIsActive.IsChecked = s.IsActive;
                txtNotes.Text = s.Notes;

                btnAdd.IsEnabled = false;
                btnUpdate.IsEnabled = true;
                btnDelete.IsEnabled = true;
            }
            else
            {
                ClearForm();
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            ClearValidationHighlights();

            txtSupplierId.Clear();
            txtCompanyName.Clear();
            txtContactPerson.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
            txtCity.Clear();
            txtCountry.Clear();
            txtPaymentTerms.Clear();
            txtRating.Clear();
            chkIsActive.IsChecked = false;
            txtNotes.Clear();

            SuppliersDataGrid.UnselectAll();
            btnAdd.IsEnabled = true;
            btnUpdate.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        // ---------------- DB param helper ----------------

        private void AddParameters(SqlCommand command)
        {
            command.Parameters.AddWithValue("@CompanyName", (txtCompanyName.Text ?? string.Empty).Trim());
            command.Parameters.AddWithValue("@ContactPerson",
                string.IsNullOrWhiteSpace(txtContactPerson.Text) ? (object)DBNull.Value : txtContactPerson.Text.Trim());
            command.Parameters.AddWithValue("@Phone",
                string.IsNullOrWhiteSpace(txtPhone.Text) ? (object)DBNull.Value : txtPhone.Text.Trim());
            command.Parameters.AddWithValue("@Email",
                string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text.Trim());
            command.Parameters.AddWithValue("@Address",
                string.IsNullOrWhiteSpace(txtAddress.Text) ? (object)DBNull.Value : txtAddress.Text.Trim());
            command.Parameters.AddWithValue("@City",
                string.IsNullOrWhiteSpace(txtCity.Text) ? (object)DBNull.Value : txtCity.Text.Trim());
            command.Parameters.AddWithValue("@Country",
                string.IsNullOrWhiteSpace(txtCountry.Text) ? (object)DBNull.Value : txtCountry.Text.Trim());
            command.Parameters.AddWithValue("@PaymentTerms",
                string.IsNullOrWhiteSpace(txtPaymentTerms.Text) ? (object)DBNull.Value : txtPaymentTerms.Text.Trim());

            int ratingVal;
            if (int.TryParse((txtRating.Text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ratingVal))
                command.Parameters.AddWithValue("@Rating", ratingVal);
            else
                command.Parameters.AddWithValue("@Rating", DBNull.Value);

            command.Parameters.AddWithValue("@IsActive", chkIsActive.IsChecked ?? false);
            command.Parameters.AddWithValue("@Notes",
                string.IsNullOrWhiteSpace(txtNotes.Text) ? (object)DBNull.Value : txtNotes.Text.Trim());
        }

        // ---------------- Validation ----------------

        private void NormalizeInputs()
        {
            if (txtCompanyName != null) txtCompanyName.Text = (txtCompanyName.Text ?? "").Trim();
            if (txtContactPerson != null) txtContactPerson.Text = (txtContactPerson.Text ?? "").Trim();
            if (txtPhone != null) txtPhone.Text = (txtPhone.Text ?? "").Trim();
            if (txtEmail != null) txtEmail.Text = (txtEmail.Text ?? "").Trim();
            if (txtAddress != null) txtAddress.Text = (txtAddress.Text ?? "").Trim();
            if (txtCity != null) txtCity.Text = (txtCity.Text ?? "").Trim();
            if (txtCountry != null) txtCountry.Text = (txtCountry.Text ?? "").Trim();
            if (txtPaymentTerms != null) txtPaymentTerms.Text = (txtPaymentTerms.Text ?? "").Trim();
            if (txtRating != null) txtRating.Text = (txtRating.Text ?? "").Trim();
            if (txtNotes != null) txtNotes.Text = (txtNotes.Text ?? "").Trim();
        }

        // returns (Ok, Summary) — async methods can’t use out/ref
        private async Task<(bool Ok, string Summary)> ValidateFormAsync(int? editingSupplierId)
        {
            var sb = new StringBuilder();
            Control first = null;

            // Company Name (required + length + uniqueness)
            var company = (txtCompanyName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(company))
            {
                Append(sb, "Company Name is required.");
                MarkInvalid(txtCompanyName, "Required.");
                if (first == null) first = txtCompanyName;
            }
            else if (company.Length > MaxCompanyLen)
            {
                Append(sb, "Company Name is too long (max " + MaxCompanyLen + ").");
                MarkInvalid(txtCompanyName, "Max " + MaxCompanyLen + " characters.");
                if (first == null) first = txtCompanyName;
            }
            else
            {
                if (await CompanyExistsAsync(company, editingSupplierId))
                {
                    Append(sb, "A supplier with this Company Name already exists.");
                    MarkInvalid(txtCompanyName, "Duplicate Company Name.");
                    if (first == null) first = txtCompanyName;
                }
            }

            // Contact person (optional)
            var person = (txtContactPerson.Text ?? "").Trim();
            if (person.Length > MaxPersonLen)
            {
                Append(sb, "Contact Person is too long (max " + MaxPersonLen + ").");
                MarkInvalid(txtContactPerson, "Max " + MaxPersonLen + " characters.");
                if (first == null) first = txtContactPerson;
            }

            // Phone (optional)
            var phoneRaw = (txtPhone.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(phoneRaw))
            {
                if (phoneRaw.Length > MaxPhoneLen || !PhoneAllowedChars.IsMatch(phoneRaw))
                {
                    Append(sb, "Phone allows +, digits, spaces, dashes, parentheses (max " + MaxPhoneLen + " chars).");
                    MarkInvalid(txtPhone, "Allowed: + 0-9 space - ( )");
                    if (first == null) first = txtPhone;
                }
                var digitsOnly = Regex.Replace(phoneRaw, "[^0-9]", "");
                if (digitsOnly.Length < MinPhoneDigits || digitsOnly.Length > MaxPhoneDigits)
                {
                    Append(sb, "Phone must contain between " + MinPhoneDigits + " and " + MaxPhoneDigits + " digits.");
                    MarkInvalid(txtPhone, "Enter a valid phone (consider +country code).");
                    if (first == null) first = txtPhone;
                }
            }

            // Email (optional)
            var email = (txtEmail.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(email))
            {
                if (email.Length > MaxEmailLen || !EmailRegex.IsMatch(email))
                {
                    Append(sb, "Email format appears invalid or length exceeds " + MaxEmailLen + ".");
                    MarkInvalid(txtEmail, "Example: name@domain.com");
                    if (first == null) first = txtEmail;
                }
            }

            // Address / City / Country / Terms / Notes (lengths)
            if (!string.IsNullOrEmpty(txtAddress.Text) && txtAddress.Text.Length > MaxAddressLen)
            {
                Append(sb, "Address is too long (max " + MaxAddressLen + ").");
                MarkInvalid(txtAddress, "Max " + MaxAddressLen + " characters.");
                if (first == null) first = txtAddress;
            }
            if (!string.IsNullOrEmpty(txtCity.Text) && txtCity.Text.Length > MaxCityLen)
            {
                Append(sb, "City is too long (max " + MaxCityLen + ").");
                MarkInvalid(txtCity, "Max " + MaxCityLen + " characters.");
                if (first == null) first = txtCity;
            }
            if (!string.IsNullOrEmpty(txtCountry.Text) && txtCountry.Text.Length > MaxCountryLen)
            {
                Append(sb, "Country is too long (max " + MaxCountryLen + ").");
                MarkInvalid(txtCountry, "Max " + MaxCountryLen + " characters.");
                if (first == null) first = txtCountry;
            }
            if (!string.IsNullOrEmpty(txtPaymentTerms.Text) && txtPaymentTerms.Text.Length > MaxTermsLen)
            {
                Append(sb, "Payment Terms is too long (max " + MaxTermsLen + ").");
                MarkInvalid(txtPaymentTerms, "Max " + MaxTermsLen + " characters.");
                if (first == null) first = txtPaymentTerms;
            }
            if (!string.IsNullOrEmpty(txtNotes.Text) && txtNotes.Text.Length > MaxNotesLen)
            {
                Append(sb, "Notes is too long (max " + MaxNotesLen + ").");
                MarkInvalid(txtNotes, "Max " + MaxNotesLen + " characters.");
                if (first == null) first = txtNotes;
            }

            // Rating (optional 1..5)
            var rtxt = (txtRating.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(rtxt))
            {
                int ratingVal;
                if (!int.TryParse(rtxt, NumberStyles.Integer, CultureInfo.InvariantCulture, out ratingVal) ||
                    ratingVal < 1 || ratingVal > 5)
                {
                    Append(sb, "Rating must be a whole number between 1 and 5, or left blank.");
                    MarkInvalid(txtRating, "Enter 1–5 or leave empty.");
                    if (first == null) first = txtRating;
                }
            }

            if (sb.Length > 0)
            {
                if (first != null) first.Focus();
                return (false, sb.ToString());
            }

            return (true, string.Empty);
        }

        private async Task<bool> CompanyExistsAsync(string companyName, int? excludeId)
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    await cn.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Suppliers WHERE CompanyName = @n";
                    if (excludeId.HasValue) sql += " AND SupplierId <> @id";
                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@n", companyName);
                        if (excludeId.HasValue) cmd.Parameters.AddWithValue("@id", excludeId.Value);
                        var o = await cmd.ExecuteScalarAsync();
                        int count = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
                        return count > 0;
                    }
                }
            }
            catch
            {
                // On lookup error, do not block saving.
                return false;
            }
        }

        // ---------------- Small UI helpers ----------------

        private void ClearValidationHighlights()
        {
            Control[] ctrls = new Control[]
            {
                txtCompanyName, txtContactPerson, txtPhone, txtEmail, txtAddress, txtCity,
                txtCountry, txtPaymentTerms, txtRating, txtNotes
            };

            foreach (var c in ctrls)
            {
                if (c == null) continue;
                c.ClearValue(Border.BorderBrushProperty);
                c.ClearValue(Border.BorderThicknessProperty);
                c.ToolTip = null;
            }
        }

        private static void MarkInvalid(Control c, string tip)
        {
            if (c == null) return;
            c.BorderBrush = Brushes.Red;
            c.BorderThickness = new Thickness(1.5);
            c.ToolTip = tip;
        }

        private static void Append(StringBuilder sb, string msg) => sb.AppendLine("• " + msg);
    }
}
