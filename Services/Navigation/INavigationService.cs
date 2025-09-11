using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementSystem.Services.Navigation
{
    public interface INavigationService
{
    void NavigateTo(string viewName);
    void NavigateTo(string viewName, object parameter);
    bool CanGoBack { get; }
    void GoBack();
}
}