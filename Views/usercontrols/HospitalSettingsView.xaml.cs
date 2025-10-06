using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HospitalManagementSystem.Services;
using Microsoft.Win32;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class HospitalSettingsView : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True";

        public HospitalSettingsView()
        {
            InitializeComponent();
            HospitalSettingsService.Instance.SetConnectionString(connectionString);
            _ = LoadSettingsIntoUI();
        }

        private async Task LoadSettingsIntoUI()
        {
            try
            {
                var settings = await HospitalSettingsService.Instance.LoadAsync();
                if (settings != null)
                {
                    txtHospitalName.Text = settings.HospitalName;
                    txtAddress.Text = settings.Address;
                    txtContactPhone.Text = settings.ContactPhone;
                    txtHospitalEmail.Text = settings.HospitalEmail;
                    txtWebsite.Text = settings.Website;
                    txtLicenseNumber.Text = settings.LicenseNumber;
                    txtDefaultCurrency.Text = settings.DefaultCurrency;
                    txtTimeZone.Text = settings.TimeZone;
                    txtLogoPath.Text = settings.LogoPath;
                }
                else
                {
                    txtDefaultCurrency.Text = "CAD";
                    txtTimeZone.Text = "America/Toronto";
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load hospital settings: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new HospitalSettingsModel
            {
                HospitalName = txtHospitalName.Text == null ? null : txtHospitalName.Text.Trim(),
                Address = txtAddress.Text == null ? null : txtAddress.Text.Trim(),
                ContactPhone = txtContactPhone.Text == null ? null : txtContactPhone.Text.Trim(),
                HospitalEmail = txtHospitalEmail.Text == null ? null : txtHospitalEmail.Text.Trim(),
                Website = txtWebsite.Text == null ? null : txtWebsite.Text.Trim(),
                LicenseNumber = txtLicenseNumber.Text == null ? null : txtLicenseNumber.Text.Trim(),
                DefaultCurrency = txtDefaultCurrency.Text == null ? null : txtDefaultCurrency.Text.Trim(),
                TimeZone = txtTimeZone.Text == null ? null : txtTimeZone.Text.Trim(),
                LogoPath = txtLogoPath.Text == null ? null : txtLogoPath.Text.Trim()
            };

            try
            {
                await HospitalSettingsService.Instance.SaveAsync(settings);
                MessageBox.Show("Hospital settings saved successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while saving settings: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBrowseLogo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Logo Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.ico;*.gif|All Files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                var sourcePath = dlg.FileName.Trim().Trim('"');
                try
                {
                    // Copy into app folder: <app>\Resources\Images\*
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var relDir = Path.Combine("Resources", "Images");
                    var absDir = Path.Combine(baseDir, relDir);

                    if (!Directory.Exists(absDir))
                        Directory.CreateDirectory(absDir);

                    var fileName = Path.GetFileName(sourcePath);
                    var destAbs = Path.Combine(absDir, fileName);
                    var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);

                    int i = 1;
                    while (File.Exists(destAbs))
                    {
                        destAbs = Path.Combine(absDir, nameNoExt + "_" + i + ext);
                        i++;
                    }

                    File.Copy(sourcePath, destAbs, false);

                    // Save relative path (Resources\Images\xyz.png)
                    var relPath = Path.Combine(relDir, Path.GetFileName(destAbs));
                    txtLogoPath.Text = relPath;
                    MessageBox.Show("Logo copied into app resources. It will load reliably.",
                        "Logo Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not copy logo:\n" + ex.Message,
                        "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
