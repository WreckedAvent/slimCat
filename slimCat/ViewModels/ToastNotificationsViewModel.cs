using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using lib;
using Views;

namespace ViewModels
{
    /// <summary>
    /// A light-weight viewmodel for toastnofications
    /// </summary>
    public class ToastNotificationsViewModel : SysProp, IDisposable
    {
        #region Fields
        private string _content = "";
        System.Timers.Timer _hideDelay = new System.Timers.Timer(5000);
        NotificationsView _view;
        #endregion

        #region Properties
        public ToastNotificationsViewModel()
        {
            _hideDelay.Elapsed += (s, e) =>
                {
                    HideNotifications();
                };
        }
        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                OnPropertyChanged("Content");
            }
        }
        #endregion

        #region Methods
        public void ShowNotifications()
        {
            _hideDelay.Stop();
            if (_view == null) _view = new NotificationsView(this);
            Dispatcher.Invoke(
                (Action)delegate
                {
                    _view.OnShowCommand();
                });
            _hideDelay.Start();
        }

        public void HideNotifications()
        {
            Dispatcher.Invoke(
            (Action)delegate
            {
                _view.OnHideCommand();
                _hideDelay.Stop();
            });
        }

        public void UpdateNotification(string content)
        {
            Content = content;
            ShowNotifications();
            _view.OnContentChanged();
        }

        #region Commands
        private RelayCommand _hide;
        public ICommand HideCommand
        {
            get
            {
                if (_hide == null)
                    _hide = new RelayCommand(args => HideNotifications());
                return _hide;
            }
        }
        #endregion
        #endregion

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool isManagedDispose)
        {
            if (isManagedDispose)
            {
                _hideDelay.Dispose();
                _view.Close();
                _view = null;
            }
        }
    }
}
