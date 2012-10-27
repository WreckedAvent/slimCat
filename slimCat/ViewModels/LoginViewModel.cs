using System;
using System.Windows.Input;
using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using slimCat.Properties;
using Views;

namespace ViewModels
{
    /// <summary>
    /// The LoginViewModel is responsible for displaying login details to the user.
    /// Fires off 'LoginEvent' when the user clicks the connect button.
    /// Responds to the 'LoginCompletedEvent'.
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        #region Fields
        private IAccount _model; // the model to interact with
        private bool _requestSent = false; // used for determining Login UI state

        private string _relay = "First, Enter your account details ..."; // message relayed to the user

        public const string LoginViewName = "LoginView";
        #endregion

        #region Properties
        public string AccountName
        {
            get { return _model.AccountName; }
            set
            {
                if (_model.AccountName != value)
                {
                    _model.AccountName = value;
                    OnPropertyChanged("AccountName");
                }
            }
        }

        public string Password
        {
            get { return _model.Password; }
            set
            {
                if (_model.Password != value)
                {
                    _model.Password = value;
                    OnPropertyChanged("Password");
                }
            }
        }

        public string RelayMessage
        {
            get { return _relay; }
            set { _relay = value; OnPropertyChanged("RelayMessage"); }
        }

        public bool RequestSent
        {
            get { return _requestSent; }
            set { _requestSent = value; OnPropertyChanged("RequestSent"); }
        }

        public bool SaveLogin
        {
            get { return Settings.Default.SaveLogin; }
            set
            {
                Settings.Default.SaveLogin = value;
                Settings.Default.Save();
            }
        }
        #endregion

        #region Constructors
        public LoginViewModel(IUnityContainer contain, IRegionManager regman, 
                                IAccount acc, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                if (acc == null) throw new ArgumentNullException("acc");
                _model = acc;
            }

            catch (Exception ex)
            {
                ex.Source = "Login ViewModel, Init";
                Exceptions.HandleException(ex);
            }
        }

        public override void Initialize()
        {
            try
            {
                _container.RegisterType<object, LoginView>(LoginViewName);

                _region.RequestNavigate(Shell.MainRegion,
                    new Uri(LoginViewName, UriKind.Relative));
            }
            catch (Exception ex)
            {
                ex.Source = "Login ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Commands
        RelayCommand _loginCommand;
        public ICommand LoginCommand
        {
            get
            {
                if (_loginCommand == null)
                {
                    _loginCommand = new RelayCommand(param => this.SendTicketRequest(),
                        param => this.CanLogin());
                }
                return _loginCommand;
            }
        }

        private bool CanLogin()
        {
            return (!string.IsNullOrWhiteSpace(AccountName) &&
                !string.IsNullOrWhiteSpace(Password) && !RequestSent);
        }

        private void SendTicketRequest()
        {
            RelayMessage = "Great! Logging in ...";
            RequestSent = true;
            this._events.GetEvent<slimCat.LoginEvent>().Publish(true);
            this._events.GetEvent<slimCat.LoginCompleteEvent>().Subscribe(handleLogin, ThreadOption.UIThread);
        }
        #endregion

        #region Methods
        private void handleLogin(bool gotTicket)
        {
            this._events.GetEvent<slimCat.LoginCompleteEvent>().Unsubscribe(handleLogin);


            if (!gotTicket)
            {
                RequestSent = false;
                RelayMessage = "Oops!" + " " + _model.Error;
            }

            else
            {
                if (SaveLogin)
                {
                    Settings.Default.UserName = _model.AccountName;
                    Settings.Default.Password = _model.Password;
                    Settings.Default.Save();
                }

                else
                {
                    Settings.Default.UserName = null;
                    Settings.Default.Password = null;
                    Settings.Default.Save();
                }
            }
        }
        #endregion
    }
}
