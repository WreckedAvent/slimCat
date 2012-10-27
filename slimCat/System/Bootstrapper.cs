using System;
using System.Windows;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.UnityExtensions;
using ViewModels;
using Microsoft.Practices.ServiceLocation;
using System.Diagnostics;
using Microsoft.Practices.Prism.Regions;
using lib;
using Microsoft.Practices.Prism.Events;

namespace slimCat
{
    /// <summary>
    /// Bootstrapper responsible for starting the application,
    /// as well as dependency injection for the various ViewModels and views
    /// </summary>
    class bootstrapper : UnityBootstrapper
    {
        #region Shell code
        protected override DependencyObject CreateShell()
        {
            return ServiceLocator.Current.GetInstance<Shell>();
        }

        protected override void InitializeShell()
        {
            App.Current.MainWindow = (Window)this.Shell;
            App.Current.MainWindow.Show();
        }
        #endregion

        #region Module and Container Code
        protected override void ConfigureModuleCatalog()
        {
            try
            {
                base.ConfigureModuleCatalog();

                Type LoginVM = typeof(LoginViewModel);
                ModuleCatalog.AddModule(
                    new ModuleInfo()
                    {
                        ModuleName = LoginVM.Name,
                        ModuleType = LoginVM.AssemblyQualifiedName,
                    });

                Type CharacterSelVM = typeof(CharacterSelectViewModel);
                ModuleCatalog.AddModule(
                    new ModuleInfo()
                    {
                        ModuleName = CharacterSelVM.Name,
                        ModuleType = CharacterSelVM.AssemblyQualifiedName,
                    });

                Type ChatWrapperVM = typeof(ChatWrapperViewModel);
                ModuleCatalog.AddModule(
                    new ModuleInfo()
                    {
                        ModuleName = ChatWrapperVM.Name,
                        ModuleType = ChatWrapperVM.AssemblyQualifiedName,
                    });

                Type UserBarVM = typeof(UserbarViewModel);
                ModuleCatalog.AddModule(
                    new ModuleInfo()
                    {
                        ModuleName = UserBarVM.Name,
                        ModuleType = UserBarVM.AssemblyQualifiedName,
                    });

                Type ChannelBarVM = typeof(ChannelbarViewModel);
                ModuleCatalog.AddModule(
                    new ModuleInfo()
                    {
                        ModuleName = ChannelBarVM.Name,
                        ModuleType = ChannelBarVM.AssemblyQualifiedName,
                    });
            }
            catch (Exception ex)
            {
                ex.Source = "Bootstrapper, Configure Modules";
                Exceptions.HandleException(ex);
            }
        }

        protected override void ConfigureContainer()
        {
            try
            {
                base.ConfigureContainer();
                // Note: These are manually added and not utilized as modules

                // Create singleton instance of our account information, register
                Models.AccountModel _account = new Models.AccountModel();
                this.Container.RegisterInstance<Models.IAccount>(_account);

                // Create singleton instance of our connection service, register
                Services.ListConnection _connection = Container.Resolve<Services.ListConnection>();
                this.Container.RegisterInstance<Services.IListConnection>(_connection);

                // Create singleton instance of our F-chat connection service, register
                Services.ChatConnection _chat_connection = Container.Resolve<Services.ChatConnection>();
                this.Container.RegisterInstance<Services.IChatConnection>(_chat_connection);

                // Create a singleton instance of our Chat data model, register
                Models.ChatModel _model = Container.Resolve<Models.ChatModel>();
                this.Container.RegisterInstance<Models.IChatModel>(_model);

                // Create a singleton instance of our Message Daemon, register
                Services.MessageDaemon _daemon = Container.Resolve<Services.MessageDaemon>();
                this.Container.RegisterInstance<Services.IChannelManager>(_daemon);

                Services.NotificationsDaemon _notifications = Container.Resolve<Services.NotificationsDaemon>();

                // create singleton instance
                Services.CommandInterceptor _inty = Container.Resolve<Services.CommandInterceptor>();

                IEventAggregator _event = Container.Resolve<EventAggregator>();
            }
            catch (Exception ex)
            {
                ex.Source = "Bootstrapper, Configure container";
                Exceptions.HandleException(ex);
            }
        }
        #endregion
    }
}
