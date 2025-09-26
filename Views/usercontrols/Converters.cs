using System;
using System.Globalization;
using System.Windows.Data;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Converts a value to a boolean. Returns false if the value is null, true otherwise.
    /// Used to enable buttons when an item is selected.
    /// </summary>
    public class NullToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a value to a boolean. Returns true if the value is null, false otherwise.
    /// Used to enable the "Add" button when no item is selected.
    /// </summary>
    public class NullToTrueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts the status of a PurchaseOrder to a boolean based on a parameter.
    /// Used to enable/disable the Approve/Reject buttons based on the order's status.
    /// </summary>
    public class StatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PurchaseOrder order && parameter is string status)
            {
                return order.Status == status;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
