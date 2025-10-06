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
            Unloaded += DashboardView_Unloaded;
        }

        private async System.Threading.Tasks.Task InitializeHeaderAsync()
        {
            var svc = HospitalSettingsService.Instance;

            if (svc?.Current == null)
            {
                try { await svc.LoadAsync(); } catch { /* ignore */ }
            }

            ApplySettingsToUI(svc?.Current);
        }

        private void OnSettingsChanged(object sender, HospitalSettingsModel e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ApplySettingsToUI(e));
            }
            else
            {
                ApplySettingsToUI(e);
            }
        }

        private void ApplySettingsToUI(HospitalSettingsModel s)
        {
            const string dash = "-";

            if (s == null)
            {
                TxtHospitalName.Text = "Hospital Management System";
                TxtHospitalAddress.Text = dash;
                TxtSubtitleName.Text = dash;
                TxtPhone.Text = TxtLicense.Text = TxtCurrency.Text = TxtTimezone.Text = dash;
                SetHyperlink(LinkEmail, null);
                SetHyperlink(LinkWebsite, null);
                HospitalLogoImage.Source = null;
                return;
            }

            TxtHospitalName.Text = string.IsNullOrWhiteSpace(s.HospitalName) ? "Hospital Management System" : s.HospitalName;
            TxtHospitalAddress.Text = string.IsNullOrWhiteSpace(s.Address) ? dash : s.Address;
            TxtSubtitleName.Text = string.IsNullOrWhiteSpace(s.HospitalName) ? dash : s.HospitalName;

            TxtPhone.Text = string.IsNullOrWhiteSpace(s.ContactPhone) ? dash : s.ContactPhone;
            TxtLicense.Text = string.IsNullOrWhiteSpace(s.LicenseNumber) ? dash : s.LicenseNumber;
            TxtCurrency.Text = string.IsNullOrWhiteSpace(s.DefaultCurrency) ? dash : s.DefaultCurrency;
            TxtTimezone.Text = string.IsNullOrWhiteSpace(s.TimeZone) ? dash : s.TimeZone;

            var email = s.HospitalEmail?.Trim();
            SetHyperlink(LinkEmail, string.IsNullOrWhiteSpace(email) ? null : "mailto:" + email, email);

            var web = s.Website?.Trim();
            if (!string.IsNullOrWhiteSpace(web) &&
                !web.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !web.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                web = "https://" + web;
            }
            SetHyperlink(LinkWebsite, string.IsNullOrWhiteSpace(web) ? null : web, web);

            try
            {
                HospitalLogoImage.Source = null;
                var raw = s.LogoPath?.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(raw)) return;

                Uri uri = null;
                if (raw.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                {
                    uri = new Uri(raw, UriKind.Absolute);
                }
                else if (Path.IsPathRooted(raw) && File.Exists(raw))
                {
                    uri = new Uri(raw, UriKind.Absolute);
                }
                else
                {
                    var combined = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, raw));
                    if (File.Exists(combined))
                        uri = new Uri(combined, UriKind.Absolute);
                }

                if (uri != null)
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = uri;
                    bmp.EndInit();
                    bmp.Freeze();
                    HospitalLogoImage.Source = bmp;
                }
            }
            catch
            {
                HospitalLogoImage.Source = null;
            }
        }

        private static void SetHyperlink(Hyperlink link, string uri, string display = null)
        {
            if (link == null) return;

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
            Unloaded -= DashboardView_Unloaded;
        }
    }
}
