using System.Windows.Controls;
using ViewModels;

namespace Views
{
    /// <summary>
    /// The actual view for the tab on the right-hand side labeled 'users'
    /// </summary>
    public partial class ManageListsTabView : UserControl
    {
        private ManageListsViewModel _vm;

        public ManageListsTabView(ManageListsViewModel vm)
        {
            InitializeComponent();
            _vm = vm;

            this.DataContext = _vm;
        }
    }
}
