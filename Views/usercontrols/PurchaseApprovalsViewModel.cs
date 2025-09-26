using System.Windows.Controls;

namespace HospitalManagementSystem.Views.UserControls
{
    /// <summary>
    /// Interaction logic for PurchaseApprovalsView.xaml
    /// </summary>
    public partial class PurchaseApprovalsView : UserControl
    {
        public PurchaseApprovalsView()
        {
            InitializeComponent();
            // Set the DataContext to the ViewModel, which handles all the logic and data
            this.DataContext = new PurchaseApprovalsViewModel();
        }
    }
}
