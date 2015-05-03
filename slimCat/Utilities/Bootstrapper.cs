#region Copyright

// <copyright file="Bootstrapper.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
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

#endregion

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Windows;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Prism.UnityExtensions;
    using Microsoft.Practices.ServiceLocation;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using ViewModels;
    using WebSocket4Net;

    #endregion

    /// <summary>
    ///     Bootstrapper responsible for starting the application,
    ///     as well as dependency injection for the various ViewModels and views
    /// </summary>
    internal class Bootstrapper : UnityBootstrapper
    {
        #region Methods

        /// <summary>
        ///     The configure container.
        /// </summary>
        protected override void ConfigureContainer()
        {
            try
            {
                base.ConfigureContainer();

                // create singletons
                RegisterSingleton<IAccount, AccountModel>();
                RegisterSingleton<IBrowseThings, BrowserService>();
                RegisterSingleton<IGetTickets, TicketService>();
                RegisterSingleton<IHandleApi, FlistService>();
                RegisterSingleton<IHandleChatConnection, FchatService>();
                RegisterSingleton<IChatModel, ChatModel>();
                RegisterSingleton<IManageChannels, ChannelService>();
                RegisterSingleton<IThemeLocator, ApplicationThemeLocator>();
                RegisterSingleton<ICharacterManager, GlobalCharacterManager>();
                RegisterSingleton<IGetPermissions, PermissionService>();
                RegisterSingleton<IAutomateThings, AutomationService>();
                RegisterSingleton<ILogThings, LoggingService>();
                RegisterSingleton<IUpdateChannelLists, ChannelListUpdaterService>();
                RegisterSingleton<IHandleIcons, IconService>();
                RegisterSingleton<IManageNotes, NoteService>();
                RegisterSingleton<IFriendRequestService, FriendRequestService>();
                RegisterSingleton<IGetProfiles, ProfileService>();
                RegisterSingleton<IChatState, ChatState>();
                RegisterSingleton<IUpdateMyself, UpdateService>();

                Register<Application, Application>(Application.Current);
                var host = Container.Resolve<IAccount>().ServerHost;
                if (string.IsNullOrWhiteSpace(host)) host = Constants.ServerHost;

                Register<WebSocket, WebSocket>(new WebSocket(host));

                // these are services that are not directly used by our singletons or modules
                Instantiate<NotificationService>();
                Instantiate<ServerCommandService>();
                Instantiate<UserCommandService>();
                Instantiate<IHandleIcons>();
                Instantiate<ProfileService>();

                // we create our viewmodels which always run here (the ones that manage the main screens, basically)
                Instantiate<LoginViewModel>();
                Instantiate<CharacterSelectViewModel>();
                Instantiate<ChatWrapperViewModel>();
                Instantiate<UserbarViewModel>();
                Instantiate<ChannelbarViewModel>();

                // some resources that are dependent on our singletons
                Application.Current.Resources.Add("BbCodeConverter", Container.Resolve<BbCodeConverter>());
                Application.Current.Resources.Add("BbFlowConverter", Container.Resolve<BbFlowConverter>());
                Application.Current.Resources.Add("BbPostConverter", Container.Resolve<BbCodePostConverter>());
                Application.Current.Resources.Add("GenderColorConverter", Container.Resolve<GenderColorConverter>());
                Application.Current.Resources.Add("NameplateColorConverter",
                    Container.Resolve<NameplateColorConverter>());
                Application.Current.Resources.Add("NameplateMessageColorConverter",
                    Container.Resolve<NameplateMessageColorConverter>());
                Application.Current.Resources.Add("ForegroundBrushConverter",
                    Container.Resolve<ForegroundBrushConverter>());
            }
            catch (Exception ex)
            {
                ex.Source = "Bootstrapper, Configure container";
                Exceptions.HandleException(ex);
            }
        }

        private void RegisterSingleton<TFrom, TTo>()
            where TFrom : class
            where TTo : TFrom
        {
            Container.RegisterType<TFrom, TTo>(new ContainerControlledLifetimeManager());
        }

        private void Instantiate<TInstance>()
            where TInstance : class
        {
            Container.Resolve<TInstance>();
        }

        private void Register<T, TConcrete>(TConcrete instance)
            where T : class
            where TConcrete : T
        {
            Container.RegisterInstance(typeof (T), instance);
        }

        /// <summary>
        ///     The configure module catalog.
        /// </summary>
        protected override void ConfigureModuleCatalog()
        {
            try
            {
                base.ConfigureModuleCatalog();
            }
            catch (Exception ex)
            {
                ex.Source = "Bootstrapper, Configure Modules";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        ///     The create shell.
        /// </summary>
        protected override DependencyObject CreateShell()
        {
            return ServiceLocator.Current.GetInstance<Shell>();
        }

        /// <summary>
        ///     The initialize shell.
        /// </summary>
        protected override void InitializeShell()
        {
            Application.Current.MainWindow = (Window) Shell;
            Application.Current.MainWindow.Show();
            (Container.Resolve<IRegionManager>()).RequestNavigate(slimCat.Shell.MainRegion,
                new Uri(LoginViewModel.LoginViewName, UriKind.Relative));
        }

        #endregion
    }
}