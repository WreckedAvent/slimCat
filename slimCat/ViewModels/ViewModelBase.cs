using System;
using System.Diagnostics;
using System.Web;
using System.Windows.Input;
using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;

namespace ViewModels
{
    public abstract class ViewModelBase : SysProp, IModule
    {
        #region Fields
        protected readonly IUnityContainer _container;
        protected readonly IRegionManager _region;
        protected readonly IEventAggregator _events;
        #endregion

        #region Shared Constructors
        public ViewModelBase(IUnityContainer contain, IRegionManager regman, IEventAggregator events)
        {
            try
            {
                if (regman == null) throw new ArgumentNullException("contain");
                _container = contain;

                if (regman == null) throw new ArgumentNullException("regman");
                _region = regman;

                if (events == null) throw new ArgumentNullException("events");
                _events = events;
            }

            catch (Exception ex)
            {
                ex.Source = "Generic ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        public abstract void Initialize();
        #endregion


        #region Global Commands
        private RelayCommand _link;
        public ICommand NavigateTo
        {
            get
            {
                if (_link == null)
                    _link = new RelayCommand(StartLinkInDefaultBrowser);
                return _link;
            }
        }

        protected void StartLinkInDefaultBrowser(object link)
        {
            string interpret = link as string;
            if (!interpret.Contains(".") || interpret.Contains(" "))
                interpret = "http://www.f-list.net/c/" + HttpUtility.UrlEncode(interpret);

            if (!String.IsNullOrEmpty(interpret))
                Process.Start(interpret);
        }
        #endregion
    }
}
