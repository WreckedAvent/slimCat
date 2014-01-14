#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModel.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace Slimcat.ViewModels
{
    #region Usings

    using System;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Properties;
    using Utilities;
    using Views;

    #endregion

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
        internal const string LoginViewName = "LoginView";

        #endregion

        #region Fields

        private readonly IAccount model; // the model to interact with

        private RelayCommand login;

        private string relayMessage = "First, Enter your account details ..."; // message relayed to the user

        private bool requestIsSent; // used for determining Login UI state

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="LoginViewModel" /> class.
        /// </summary>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="acc">
        ///     The acc.
        /// </param>
        /// <param name="events">
        ///     The events.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        public LoginViewModel(
            IUnityContainer contain, IRegionManager regman, IAccount acc, IEventAggregator events, IChatModel cm,
            ICharacterManager lists)
            : base(contain, regman, events, cm, lists)
        {
            try
            {
                model = acc.ThrowIfNull("acc");
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
            get { return model.AccountName; }

            set
            {
                if (model.AccountName == value)
                    return;

                model.AccountName = value;
                OnPropertyChanged("AccountName");
            }
        }

        /// <summary>
        ///     Gets the login command.
        /// </summary>
        public ICommand LoginCommand
        {
            get
            {
                return login
                       ?? (login = new RelayCommand(param => SendTicketRequest(), param => CanLogin()));
            }
        }

        /// <summary>
        ///     Gets or sets the password.
        /// </summary>
        public string Password
        {
            get { return model.Password; }

            set
            {
                if (model.Password == value)
                    return;

                model.Password = value;
                OnPropertyChanged("Password");
            }
        }

        /// <summary>
        ///     Gets or sets the relay message.
        /// </summary>
        public string RelayMessage
        {
            get { return relayMessage; }

            set
            {
                relayMessage = value;
                OnPropertyChanged("RelayMessage");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether request sent.
        /// </summary>
        public bool RequestSent
        {
            get { return requestIsSent; }

            set
            {
                requestIsSent = value;
                OnPropertyChanged("RequestSent");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether save login.
        /// </summary>
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

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
            try
            {
                Container.RegisterType<object, LoginView>(LoginViewName);

                RegionManager.RequestNavigate(Shell.MainRegion, new Uri(LoginViewName, UriKind.Relative));
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
            return !string.IsNullOrWhiteSpace(AccountName) && !string.IsNullOrWhiteSpace(Password)
                   && !RequestSent;
        }

        private void SendTicketRequest()
        {
            RelayMessage = "Great! Logging in ...";
            RequestSent = true;
            Events.GetEvent<LoginEvent>().Publish(true);
            Events.GetEvent<LoginCompleteEvent>().Subscribe(HandleLogin, ThreadOption.UIThread);
        }

        private void HandleLogin(bool gotTicket)
        {
            Events.GetEvent<LoginCompleteEvent>().Unsubscribe(HandleLogin);

            if (!gotTicket)
            {
                RequestSent = false;
                RelayMessage = "Oops!" + " " + model.Error;
            }
            else
            {
                if (SaveLogin)
                {
                    Settings.Default.UserName = model.AccountName;
                    Settings.Default.Password = model.Password;
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