using System.Windows.Controls;
using ViewModels;

namespace Views
{
    /// <summary>
    /// The actual view for the tab on the right-hand side labeled 'global'
    /// </summary>
    public partial class GlobalTabView : UserControl
    {
        private GlobalTabViewModel _vm;

        public GlobalTabView(GlobalTabViewModel vm)
        {
            InitializeComponent();
            _vm = vm;

            this.DataContext = _vm;
        }
    }
}
