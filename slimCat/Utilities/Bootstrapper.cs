#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Bootstrapper.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Windows;
    using Microsoft.Practices.Prism.Modularity;
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
                RegisterSingleton<IBrowser, Browser>();
                RegisterSingleton<ITicketProvider, TicketProvider>();
                RegisterSingleton<IListConnection, ListConnection>();
                RegisterSingleton<IChatConnection, ChatConnection>();
                RegisterSingleton<IChatModel, ChatModel>();
                RegisterSingleton<IChannelManager, MessageDaemon>();
                RegisterSingleton<IThemeLocator, ApplicationThemeLocator>();
                RegisterSingleton<ICharacterManager, GlobalCharacterManager>();

                Register<Application, Application>(Application.Current);
                Register<WebSocket, WebSocket>(new WebSocket(Constants.ServerHost));

                // these are services that are not directly used by our singletons or modules
                Instantiate<NotificationsDaemon>();
                Instantiate<CommandInterceptor>();

                // some resources that are dependant on our singletons
                Application.Current.Resources.Add("BbCodeConverter", Container.Resolve<BbCodeConverter>());
                Application.Current.Resources.Add("BbFlowConverter", Container.Resolve<BbFlowConverter>());
                Application.Current.Resources.Add("BbPostConverter", Container.Resolve<BbCodePostConverter>());
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

                // modules 
                AddModule(typeof (LoginViewModel));
                AddModule(typeof (CharacterSelectViewModel));

                AddModule(typeof (ChatWrapperViewModel));

                AddModule(typeof (UserbarViewModel));
                AddModule(typeof (ChannelbarViewModel));
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
        /// <returns>
        ///     The <see cref="DependencyObject" />.
        /// </returns>
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
        }

        private void AddModule(Type moduleType)
        {
            ModuleCatalog.AddModule(
                new ModuleInfo
                    {
                        ModuleName = moduleType.Name,
                        ModuleType = moduleType.AssemblyQualifiedName
                    });
        }

        #endregion
    }
}