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
    public partial class GeneralChannelView : DisposableView
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

                _vm.NewAdArrived += OnNewAdArrived;
                _vm.NewMessageArrived += OnNewMessageArrived;
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

        private void OnNewAdArrived(object sender, EventArgs e)
        {
            bool keepAtCurrent = _vm.Model.Messages.Count >= Models.ApplicationSettings.BackLogMax;

            var scroller = _vm.IsSearching ? _filter : _manager;
            scroller.AutoDownScroll(keepAtCurrent);
        }

        private void OnNewMessageArrived(object sender, EventArgs e)
        {
            bool keepAtCurrent = _vm.Model.Messages.Count >= Models.ApplicationSettings.BackLogMax;

            var scroller = _vm.IsSearching ? _filter : _manager;
            scroller.AutoDownScroll(keepAtCurrent);
        }
        #endregion

        internal override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                _vm.NewAdArrived -= OnNewAdArrived;
                _vm.NewMessageArrived -= OnNewMessageArrived;
                _vm = null;
                _filter = null;
                _manager = null;
                messages.ItemsSource = null;
                filtered.ItemsSource = null;
                this.DataContext = null;
            }
        }
    }
}
