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
    /// Interaction logic for ChatWrapperView.xaml
    /// </summary>
    public partial class ChatWrapperView : UserControl
    {
        #region Fields
        public const string UserbarRegion = "UserbarRegion";
        public const string ConversationRegion = "ConversationRegion";
        public const string ChannelbarRegion = "ChannelbarRegion";
        private readonly ViewModelBase _vm;
        #endregion

        public ChatWrapperView(ChatWrapperViewModel vm)
        {
            try
            {
                InitializeComponent();

                _vm = vm;
                if (_vm == null) throw new ArgumentNullException("vm");

                this.DataContext = _vm;
            }
            catch (Exception ex)
            {
                ex.Source = "Chat Wrapper View, init";
                Exceptions.HandleException(ex);
            }
        }
    }
}
