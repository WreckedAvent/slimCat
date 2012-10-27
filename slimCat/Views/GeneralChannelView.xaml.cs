using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViewModels;

namespace Views
{
    /// <summary>
    /// Interaction logic for GeneralChannelView.xaml
    /// </summary>
    public partial class GeneralChannelView : UserControl
    {
        #region Fields
        private GeneralChannelViewModel _vm;
        private SnapToBottomManager _manager;
        private SnapToBottomManager _filter;
        #endregion

        #region Constructors
        public GeneralChannelView(GeneralChannelViewModel vm)
        {
            try
            {
                InitializeComponent();
                if (vm == null) throw new ArgumentNullException("vm");

                _vm = vm;
                this.DataContext = _vm;

                _manager = new SnapToBottomManager(messages);
                _filter = new SnapToBottomManager(filtered);

                _vm.NewAdArrived += (s, e) =>
                    {
                        bool keepAtCurrent = _vm.Model.Messages.Count >= 300;

                        var scroller = _vm.IsSearching ? _filter : _manager;
                        scroller.AutoDownScroll(keepAtCurrent);
                    };

                _vm.NewMessageArrived += (s, e) =>
                    {
                        bool keepAtCurrent = _vm.Model.Messages.Count >= 300;

                        var scroller = _vm.IsSearching ? _filter : _manager;
                        scroller.AutoDownScroll(keepAtCurrent);
                    };
            }

            catch (Exception ex)
            {
                ex.Source = "PMChannel View, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        private void OnMessagesLoaded(object sender, EventArgs e)
        {
            _manager.AutoDownScroll(false, true);
        }

        private void OnFilterLoaded(object sender, EventArgs e)
        {
            _filter.AutoDownScroll(false, true);
        }
        #endregion
    }
}
