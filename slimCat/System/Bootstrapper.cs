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

namespace slimCat
{
    using System;
    using System.Windows;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Modularity;
    using Microsoft.Practices.Prism.UnityExtensions;
    using Microsoft.Practices.ServiceLocation;
    using Microsoft.Practices.Unity;

    using Models;

    using Services;

    using ViewModels;

    /// <summary>
    ///     Bootstrapper responsible for starting the application,
    ///     as well as dependency injection for the various ViewModels and views
    /// </summary>
    internal class bootstrapper : UnityBootstrapper
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

                // Note: These are manually added and not utilized as modules

                // Create singleton instance of our account information, register
                var _account = new AccountModel();
                this.Container.RegisterInstance<IAccount>(_account);

                // Create singleton instance of our connection service, register
                var _connection = this.Container.Resolve<ListConnection>();
                this.Container.RegisterInstance<IListConnection>(_connection);

                // Create singleton instance of our F-chat connection service, register
                var _chat_connection = this.Container.Resolve<ChatConnection>();
                this.Container.RegisterInstance<IChatConnection>(_chat_connection);

                // Create a singleton instance of our Chat data model, register
                var _model = this.Container.Resolve<ChatModel>();
                this.Container.RegisterInstance<IChatModel>(_model);

                // Create a singleton instance of our Message Daemon, register
                var _daemon = this.Container.Resolve<MessageDaemon>();
                this.Container.RegisterInstance<IChannelManager>(_daemon);

                var _notifications = this.Container.Resolve<NotificationsDaemon>();

                // create singleton instance
                var _inty = this.Container.Resolve<CommandInterceptor>();

                IEventAggregator _event = this.Container.Resolve<EventAggregator>();
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

                Type LoginVM = typeof(LoginViewModel);
                this.ModuleCatalog.AddModule(
                    new ModuleInfo { ModuleName = LoginVM.Name, ModuleType = LoginVM.AssemblyQualifiedName, });

                Type CharacterSelVM = typeof(CharacterSelectViewModel);
                this.ModuleCatalog.AddModule(
                    new ModuleInfo
                        {
                            ModuleName = CharacterSelVM.Name, 
                            ModuleType = CharacterSelVM.AssemblyQualifiedName, 
                        });

                Type ChatWrapperVM = typeof(ChatWrapperViewModel);
                this.ModuleCatalog.AddModule(
                    new ModuleInfo
                        {
                            ModuleName = ChatWrapperVM.Name, 
                            ModuleType = ChatWrapperVM.AssemblyQualifiedName, 
                        });

                Type UserBarVM = typeof(UserbarViewModel);
                this.ModuleCatalog.AddModule(
                    new ModuleInfo { ModuleName = UserBarVM.Name, ModuleType = UserBarVM.AssemblyQualifiedName, });

                Type ChannelBarVM = typeof(ChannelbarViewModel);
                this.ModuleCatalog.AddModule(
                    new ModuleInfo { ModuleName = ChannelBarVM.Name, ModuleType = ChannelBarVM.AssemblyQualifiedName, });
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

        #endregion
    }
}