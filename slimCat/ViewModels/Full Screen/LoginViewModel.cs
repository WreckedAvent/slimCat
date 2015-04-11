#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModel.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//  
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Windows;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Properties;
    using Services;
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

        internal const string LoginViewName = "LoginView";

        #endregion

        #region Fields

        private readonly IAccount model; // the model to interact with

        private RelayCommand login;

        private string relayMessage = Constants.FriendlyName; // message relayed to the user

        private bool requestIsSent; // used for determining Login UI state

        private readonly IBrowser browser;

        #endregion

        #region Constructors and Destructors

        public LoginViewModel(IChatState chatState, IBrowser browser)
            : base(chatState)
        {
            try
            {
                model = chatState.Account;
                this.browser = browser;
                CheckForUpdates();

                LoggingSection = "login vm";
            }
            catch (Exception ex)
            {
                ex.Source = "Login ViewModel, Init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

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

        public ICommand LoginCommand
        {
            get
            {
                return login
                       ?? (login = new RelayCommand(param => SendTicketRequest(), param => CanLogin()));
            }
        }

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

        public bool ShowCapslockWarning { get { return Console.CapsLock; } }

        public string ServerHost
        {
            get { return model.ServerHost; }

            set
            {
                if (model.ServerHost == value)
                    return;

                model.ServerHost = value;
                OnPropertyChanged("ServerHost");
            }
        }

        public string RelayMessage
        {
            get { return relayMessage; }

            set
            {
                relayMessage = value;
                OnPropertyChanged("RelayMessage");
            }
        }

        public bool RequestSent
        {
            get { return requestIsSent; }

            set
            {
                requestIsSent = value;
                OnPropertyChanged("RequestSent");
            }
        }

        public bool Advanced
        {
            get { return Settings.Default.Advanced; }
            set {
                Settings.Default.Advanced = value;
            }
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

        public bool HasNewUpdate { get; set; }

        public string UpdateName { get; set; }

        public string UpdateLink { get; set; }

        #endregion

        #region Public Methods and Operators

        public override void Initialize()
        {
            try
            {
                Container.RegisterType<object, LoginView>(LoginViewName);

                RegionManager.RequestNavigate(Shell.MainRegion, new Uri(LoginViewName, UriKind.Relative));
                Log("Requesting login view");
            }
            catch (Exception ex)
            {
                ex.Source = "Login ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        public void UpdateCapsLockWarning()
        {
            OnPropertyChanged("ShowCapslockWarning");
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
            RelayMessage = "Logging in ...";
            RequestSent = true;
            Events.GetEvent<LoginEvent>().Publish(true);
            Events.GetEvent<LoginCompleteEvent>().Subscribe(HandleLogin, ThreadOption.UIThread);
            Log("Sending login request");
        }

        private void HandleLogin(bool gotTicket)
        {
            RequestSent = false;

            if (!gotTicket)
            {
                RelayMessage = "Oops!" + " " + model.Error;
                Log("Login unsuccessful");
            }
            else
            {
                Log("Login successful");
                if (SaveLogin)
                {
                    Settings.Default.UserName = model.AccountName;
                    Settings.Default.Password = model.Password;
                    Settings.Default.Host = model.ServerHost;
                    Settings.Default.Save();
                }
                else
                {
                    Settings.Default.UserName = null;
                    Settings.Default.Password = null;
                    Settings.Default.Host = null;
                    Settings.Default.Save();
                }

                RelayMessage = Constants.FriendlyName;
            }
        }

        private async void CheckForUpdates()
        {
            try
            {
                var resp = await browser.GetResponseAsync(Constants.NewVersionUrl);
                if (resp == null) return;
                var args = resp.Split(',');

                var isNewUpdate = StaticFunctions.IsUpdate(args[0]);
                var updateFailed = false;

                if (isNewUpdate)
                using (var client = new WebClient())
                {
                    var basePath = Path.GetDirectoryName(Settings.Default.BasePath);
                    var tempLocation = Path.GetTempFileName().Replace(".tmp", ".zip");
                    await client.DownloadFileTaskAsync(new Uri(args[1]), tempLocation);

                    using (var zip = ZipFile.OpenRead(tempLocation))
                    foreach (var file in zip.Entries)
                    {
                        var filePath = Path.Combine(basePath, file.FullName);
                        var fileDir = Path.GetDirectoryName(filePath);

                        // don't update theme or bootstrapper
                        if (fileDir.EndsWith("theme", StringComparison.OrdinalIgnoreCase)) continue;
                        if (filePath.EndsWith("bootstrapper.exe", StringComparison.OrdinalIgnoreCase)) continue;

                        if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);

                        try
                        {
                            file.ExtractToFile(filePath, true);
                        }
                        catch
                        {
                            if (fileDir.EndsWith("icons", StringComparison.OrdinalIgnoreCase)) continue;

                            updateFailed = true;
                            break;
                        }
                    }
                }

                if (updateFailed) MessageBox.Show("Oops! Update failed");

                Dispatcher.BeginInvoke((Action) delegate
                {
                    HasNewUpdate = HasNewUpdate;

                    UpdateName = args[0] + " update";
                    UpdateLink = args[1];
                    ApplicationSettings.SlimCatChannelId = args[4];

                    OnPropertyChanged("HasNewUpdate");
                    OnPropertyChanged("UpdateName");
                    OnPropertyChanged("UpdateLink");
                });
            }
            catch
            {
            }
        }

        #endregion
    }
}