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
    public partial class PMChannelView : DisposableView
    {
        #region Fields
        private PMChannelViewModel _vm;
        private SnapToBottomManager _manager;
        #endregion

        #region Constructors
        public PMChannelView(PMChannelViewModel vm)
        {
            try
            {
                InitializeComponent();
                if (vm == null) throw new ArgumentNullException("vm");

                _vm = vm;
                this.DataContext = _vm;

                _manager = new SnapToBottomManager(messages);

                _vm.NewMessageArrived += OnNewMessageArrived;
                _vm.StatusChanged += OnStatusChanged;
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

        private void OnNewMessageArrived(object sender, EventArgs e)
        {
            bool keepAtCurrent = _vm.Model.Messages.Count >= Models.ApplicationSettings.BackLogMax;
            _manager.AutoDownScroll(keepAtCurrent);
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(
                (Action)delegate
                {
                    if (!CharacterStatusDisplayer.IsExpanded)
                        CharacterStatusDisplayer.IsExpanded = true;
                });
        }

        internal override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _vm.StatusChanged -= OnStatusChanged;
                _vm.NewMessageArrived -= OnNewMessageArrived;
                _manager = null;
                this.DataContext = null;
                _vm = null;
            }
        }
        #endregion
    }
}
