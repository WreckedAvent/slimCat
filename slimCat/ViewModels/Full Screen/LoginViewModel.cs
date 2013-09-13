// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The LoginViewModel is responsible for displaying login details to the user.
//   Fires off 'LoginEvent' when the user clicks the connect button.
//   Responds to the 'LoginCompletedEvent'.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ViewModels
{
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

    /// <summary>
    ///     The LoginViewModel is responsible for displaying login details to the user.
    ///     Fires off 'LoginEvent' when the user clicks the connect button.
    ///     Responds to the 'LoginCompletedEvent'.
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        #region Constants

        /// <summary>
        ///     The login view name.
        /// </summary>
        public const string LoginViewName = "LoginView";

        #endregion

        #region Fields

        private readonly IAccount _model; // the model to interact with

        private RelayCommand _loginCommand;

        private string _relay = "First, Enter your account details ..."; // message relayed to the user

        private bool _requestSent; // used for determining Login UI state

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
        /// </summary>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="acc">
        /// The acc.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        public LoginViewModel(
            IUnityContainer contain, IRegionManager regman, IAccount acc, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                this._model = acc.ThrowIfNull("acc");
            }
            catch (Exception ex)
            {
                ex.Source = "Login ViewModel, Init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the account name.
        /// </summary>
        public string AccountName
        {
            get
            {
                return this._model.AccountName;
            }

            set
            {
                if (this._model.AccountName != value)
                {
                    this._model.AccountName = value;
                    this.OnPropertyChanged("AccountName");
                }
            }
        }

        /// <summary>
        ///     Gets the login command.
        /// </summary>
        public ICommand LoginCommand
        {
            get
            {
                if (this._loginCommand == null)
                {
                    this._loginCommand = new RelayCommand(param => this.SendTicketRequest(), param => this.CanLogin());
                }

                return this._loginCommand;
            }
        }

        /// <summary>
        ///     Gets or sets the password.
        /// </summary>
        public string Password
        {
            get
            {
                return this._model.Password;
            }

            set
            {
                if (this._model.Password != value)
                {
                    this._model.Password = value;
                    this.OnPropertyChanged("Password");
                }
            }
        }

        /// <summary>
        ///     Gets or sets the relay message.
        /// </summary>
        public string RelayMessage
        {
            get
            {
                return this._relay;
            }

            set
            {
                this._relay = value;
                this.OnPropertyChanged("RelayMessage");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether request sent.
        /// </summary>
        public bool RequestSent
        {
            get
            {
                return this._requestSent;
            }

            set
            {
                this._requestSent = value;
                this.OnPropertyChanged("RequestSent");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether save login.
        /// </summary>
        public bool SaveLogin
        {
            get
            {
                return Settings.Default.SaveLogin;
            }

            set
            {
                Settings.Default.SaveLogin = value;
                Settings.Default.Save();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
            try
            {
                this._container.RegisterType<object, LoginView>(LoginViewName);

                this._region.RequestNavigate(Shell.MainRegion, new Uri(LoginViewName, UriKind.Relative));
            }
            catch (Exception ex)
            {
                ex.Source = "Login ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(this.AccountName) && !string.IsNullOrWhiteSpace(this.Password)
                   && !this.RequestSent;
        }

        private void SendTicketRequest()
        {
            this.RelayMessage = "Great! Logging in ...";
            this.RequestSent = true;
            this._events.GetEvent<LoginEvent>().Publish(true);
            this._events.GetEvent<LoginCompleteEvent>().Subscribe(this.handleLogin, ThreadOption.UIThread);
        }

        private void handleLogin(bool gotTicket)
        {
            this._events.GetEvent<LoginCompleteEvent>().Unsubscribe(this.handleLogin);

            if (!gotTicket)
            {
                this.RequestSent = false;
                this.RelayMessage = "Oops!" + " " + this._model.Error;
            }
            else
            {
                if (this.SaveLogin)
                {
                    Settings.Default.UserName = this._model.AccountName;
                    Settings.Default.Password = this._model.Password;
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