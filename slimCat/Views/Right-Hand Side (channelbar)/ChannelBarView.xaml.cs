using System.Windows.Controls;
using ViewModels;

namespace Views
{
    /// <summary>
    /// Interaction logic for ChannelBarView.xaml
    /// </summary>
    public partial class ChannelbarView : UserControl
    {
        private ChannelbarViewModel _vm;

        public ChannelbarViewModel VM { get { return _vm; } }

        public ChannelbarView(ChannelbarViewModel vm)
        {
            InitializeComponent();
            _vm = vm;

            this.DataContext = _vm;

            _vm.OnJumpToNotifications += (s, e) =>
                {
                    if (NotificationButton.IsChecked == false)
                        NotificationButton.IsChecked = true;
                };
        }
    }
}
