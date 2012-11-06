using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ViewModels;

namespace Views
{
    /// <summary>
    /// Interaction logic for ChanneListView.xaml
    /// </summary>
    public partial class ChannelTabView : UserControl
    {
        private ChannelsTabViewModel _vm;

        public ChannelTabView(ChannelsTabViewModel vm )
        {
            InitializeComponent();
            _vm = vm;

            this.DataContext = _vm;
        }
    }
}
