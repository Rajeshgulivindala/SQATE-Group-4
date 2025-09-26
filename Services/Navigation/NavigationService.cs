using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace HospitalManagementSystem.Services.Navigation
{
    public class HMSNavigationService : INavigationService
    {
        private readonly ContentControl _navigationFrame;
        private readonly Stack<UserControl> _navigationStack;
        private readonly Dictionary<string, Func<UserControl>> _pages;

        public HMSNavigationService(ContentControl navigationFrame)
        {
            _navigationFrame = navigationFrame;
            _navigationStack = new Stack<UserControl>();
            _pages = new Dictionary<string, Func<UserControl>>();
            RegisterPages();
        }

        private void RegisterPages()
        {
            // Register views as you create them
            _pages.Add("Dashboard", () => CreatePlaceholderView("Dashboard Module"));
            _pages.Add("Patients", () => CreatePlaceholderView("Patients Module"));
            _pages.Add("Staff", () => CreatePlaceholderView("Staff Module"));
            _pages.Add("Appointments", () => CreatePlaceholderView("Appointments Module"));
            _pages.Add("Inventory", () => CreatePlaceholderView("Inventory Module"));
            _pages.Add("Reports", () => CreatePlaceholderView("Reports Module"));
        }

        private UserControl CreatePlaceholderView(string moduleName)
        {
            var textBlock = new TextBlock
            {
                Text = $"{moduleName}\n\nThis will be implemented in upcoming weeks.",
                FontSize = 18,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            return new UserControl { Content = textBlock };
        }

        public void NavigateTo(string viewName)
        {
            NavigateTo(viewName, null);
        }

        public void NavigateTo(string viewName, object parameter)
        {
            if (_pages.ContainsKey(viewName))
            {
                if (_navigationFrame.Content != null)
                {
                    _navigationStack.Push(_navigationFrame.Content as UserControl);
                }

                var page = _pages[viewName]();
                _navigationFrame.Content = page;
            }
        }

        public bool CanGoBack => _navigationStack.Count > 0;

        public void GoBack()
        {
            if (CanGoBack)
            {
                _navigationFrame.Content = _navigationStack.Pop();
            }
        }
    }
}