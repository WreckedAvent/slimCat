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
    /// Interaction logic for PMChannelView.xaml
    /// </summary>
    public partial class PMChannelView : UserControl
    {
        private PMChannelViewModel _vm;
        private SnapToBottomManager _manager;

        public PMChannelView(PMChannelViewModel vm)
        {
            try
            {
                InitializeComponent();
                if (vm == null) throw new ArgumentNullException("vm");

                _vm = vm;
                this.DataContext = _vm;

                _manager = new SnapToBottomManager(messages);

                _vm.NewMessageArrived += (s, e) =>
                {
                    bool keepAtCurrent = _vm.Model.Messages.Count >= 300;
                    _manager.AutoDownScroll(keepAtCurrent);
                };

                _vm.StatusChanged += (s, e) =>
                {
                    Dispatcher.Invoke(
                        (Action)delegate
                        {
                            if (!CharacterStatusDisplayer.IsExpanded)
                                CharacterStatusDisplayer.IsExpanded = true;
                        });
                };
            }

            catch (Exception ex)
            {
                ex.Source = "PMChannel View, init";
                Exceptions.HandleException(ex);
            }
        }

        private void OnMessagesLoaded(object sender, EventArgs e)
        {
            _manager.AutoDownScroll(false, true);
        }
    }
}
