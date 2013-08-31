/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
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
                _model = acc.ThrowIfNull("acc");
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
