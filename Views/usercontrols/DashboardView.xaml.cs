using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Views.UserControls
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            _ = InitializeHeaderAsync();

            HospitalSettingsService.Instance.SettingsChanged += OnSettingsChanged;
            this.Unloaded += DashboardView_Unloaded;
        }

        private async System.Threading.Tasks.Task InitializeHeaderAsync()
        {
            var svc = HospitalSettingsService.Instance;

            if (svc.Current == null)
            {
                try { await svc.LoadAsync(); } catch { /* ignore */ }
            }

            ApplySettingsToUI(svc.Current);
        }

        private void OnSettingsChanged(object sender, HospitalSettingsModel e)
        {
            ApplySettingsToUI(e);
        }

        private void ApplySettingsToUI(HospitalSettingsModel s)
        {
            // Header text and defaults
            if (s == null)
            {
                TxtHospitalName.Text = "Hospital Management System";
                TxtHospitalAddress.Text = "";
                TxtSubtitleName.Text = "-";
                TxtPhone.Text = TxtLicense.Text = TxtCurrency.Text = TxtTimezone.Text = "-";
                SetHyperlink(LinkEmail, null);
                SetHyperlink(LinkWebsite, null);
                HospitalLogoImage.Source = null;
                return;
            }

            TxtHospitalName.Text = string.IsNullOrWhiteSpace(s.HospitalName) ? "Hospital Management System" : s.HospitalName;
            TxtHospitalAddress.Text = s.Address ?? "";
            TxtSubtitleName.Text = string.IsNullOrWhiteSpace(s.HospitalName) ? "-" : s.HospitalName;

            // Details
            TxtPhone.Text = string.IsNullOrWhiteSpace(s.ContactPhone) ? "-" : s.ContactPhone;
            TxtLicense.Text = string.IsNullOrWhiteSpace(s.LicenseNumber) ? "-" : s.LicenseNumber;
            TxtCurrency.Text = string.IsNullOrWhiteSpace(s.DefaultCurrency) ? "-" : s.DefaultCurrency;
            TxtTimezone.Text = string.IsNullOrWhiteSpace(s.TimeZone) ? "-" : s.TimeZone;

            // Email
            var email = s.HospitalEmail == null ? null : s.HospitalEmail.Trim();
            if (!string.IsNullOrWhiteSpace(email))
                SetHyperlink(LinkEmail, "mailto:" + email, email);
            else
                SetHyperlink(LinkEmail, null);

            // Website (prepend https:// if missing)
            var web = s.Website == null ? null : s.Website.Trim();
            if (!string.IsNullOrWhiteSpace(web))
            {
                if (!web.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !web.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    web = "https://" + web;
                }
                SetHyperlink(LinkWebsite, web, web);
            }
            else
            {
                SetHyperlink(LinkWebsite, null);
            }

            // Logo: pack URI, absolute, or relative
            try
            {
                HospitalLogoImage.Source = null;
                var raw = s.LogoPath == null ? null : s.LogoPath.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(raw)) return;

                if (raw.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                {
                    SetImage(new Uri(raw, UriKind.Absolute));
                    return;
                }

                if (Path.IsPathRooted(raw) && File.Exists(raw))
                {
                    SetImage(new Uri(raw, UriKind.Absolute));
                    return;
                }

                var combined = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, raw));
                if (File.Exists(combined))
                {
                    SetImage(new Uri(combined, UriKind.Absolute));
                }
            }
            catch
            {
                HospitalLogoImage.Source = null;
            }
        }

        private void SetImage(Uri uri)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = uri;
            bmp.EndInit();
            bmp.Freeze();
            HospitalLogoImage.Source = bmp;
        }

        // Single helper (C# 7.3 compatible). Remove any other overloads.
        private static void SetHyperlink(Hyperlink link, string uri, string display = null)
        {
            link.Inlines.Clear();
            if (string.IsNullOrWhiteSpace(uri))
            {
                link.Inlines.Add(new Run("-"));
                link.NavigateUri = null;
                link.IsEnabled = false;
            }
            else
            {
                link.Inlines.Add(new Run(string.IsNullOrWhiteSpace(display) ? uri : display));
                link.NavigateUri = new Uri(uri, UriKind.Absolute);
                link.IsEnabled = true;
            }
        }

        private void LinkWebsite_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
            catch { }
            e.Handled = true;
        }

        private void LinkEmail_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
            catch { }
            e.Handled = true;
        }

        private void DashboardView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            HospitalSettingsService.Instance.SettingsChanged -= OnSettingsChanged;
        }
    }
}
