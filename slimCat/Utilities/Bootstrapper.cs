// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Bootstrapper.cs" company="Justin Kadrovach">
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
//   Bootstrapper responsible for starting the application,
//   as well as dependency injection for the various ViewModels and views
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Utilities
{
    using System;
    using System.Windows;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Modularity;
    using Microsoft.Practices.Prism.UnityExtensions;
    using Microsoft.Practices.ServiceLocation;
    using Microsoft.Practices.Unity;

    using Models;

    using Slimcat.Services;

    using Slimcat.ViewModels;

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
                this.Container.RegisterType<IAccount, AccountModel>(new ContainerControlledLifetimeManager());
                this.Container.RegisterType<IListConnection, ListConnection>(new ContainerControlledLifetimeManager());
                this.Container.RegisterType<IChatConnection, ChatConnection>(new ContainerControlledLifetimeManager());
                this.Container.RegisterType<IChatModel, ChatModel>(new ContainerControlledLifetimeManager());
                this.Container.RegisterType<IChannelManager, MessageDaemon>(new ContainerControlledLifetimeManager());
                this.Container.RegisterType<EventAggregator>(new ContainerControlledLifetimeManager());

                // these are services that are not directly used by our singletons or modules
                this.Container.Resolve<NotificationsDaemon>();
                this.Container.Resolve<CommandInterceptor>();

                // some resources that are dependant on our singletons
                Application.Current.Resources.Add("BBCodeConverter", this.Container.Resolve<BBCodeConverter>());
                Application.Current.Resources.Add("BBFlowConverter", this.Container.Resolve<BBFlowConverter>());
                Application.Current.Resources.Add("BBPostConverter", this.Container.Resolve<BBCodePostConverter>());

            }
            catch (Exception ex)
            {
                ex.Source = "Bootstrapper, Configure container";
                Exceptions.HandleException(ex);
            }
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
                this.AddModule(typeof(LoginViewModel));
                this.AddModule(typeof(CharacterSelectViewModel));

                this.AddModule(typeof(ChatWrapperViewModel));

                this.AddModule(typeof(UserbarViewModel));
                this.AddModule(typeof(ChannelbarViewModel));
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
            Application.Current.MainWindow = (Window)this.Shell;
            Application.Current.MainWindow.Show();
        }

        private void AddModule(Type moduleType)
        {
            this.ModuleCatalog.AddModule(
                new ModuleInfo
                    {
                        ModuleName = moduleType.Name,
                        ModuleType = moduleType.AssemblyQualifiedName
                    });
        }

        #endregion
    }
}