using System.Windows.Controls;
using ViewModels;

namespace Views
{
    /// <summary>
    /// The actual view for the tab on the right-hand side labeled 'users'
    /// </summary>
    public partial class UsersTabView : UserControl
    {
        private UsersTabViewModel _vm;

        public UsersTabView(UsersTabViewModel vm)
        {
            InitializeComponent();
            _vm = vm;

            this.DataContext = _vm;
        }
    }
}
