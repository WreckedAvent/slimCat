using System;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Views;
using System.Linq;
using lib;
using System.Windows.Input;
using slimCat.Properties;

namespace ViewModels
{
    /// <summary>
    /// Used for a few channels which are not treated normally and cannot receive/send messages. 
    /// </summary>
    public class UtilityChannelViewModel : ChannelViewModelBase, IDisposable
    {
        private System.Timers.Timer UpdateTimer = new System.Timers.Timer(1000);

        #region Properties
        public string RoughServerUpTime { get { return HelperConverter.DateTimeToRough(CM.ServerUpTime, true, false); } }
        public string RoughClientUpTime { get { return HelperConverter.DateTimeToRough(CM.ClientUptime, true, false); } }

        public int OnlineCount { get { return CM.OnlineCharacters.Count; } }
        public int OnlineFriendsCount
        {
            get { return (CM.OnlineFriends == null ? 0 : CM.OnlineFriends.Count); } 
        }
        public int OnlineBookmarksCount
        {
            get { return (CM.OnlineBookmarks == null ? 0 : CM.OnlineBookmarks.Count); } 
        }
        #endregion

        #region Constructors
        public UtilityChannelViewModel(string name, IUnityContainer contain, IRegionManager regman,
                                       IEventAggregator events)
            : base(contain, regman, events)
        {
            try
            {
                Model = _container.Resolve<GeneralChannelModel>(name);

                _container.RegisterType<object, UtilityChannelView>(Model.ID, new InjectionConstructor(this));

                UpdateTimer.Enabled = true;
                UpdateTimer.Elapsed += (s, e) => 
                { 
                    OnPropertyChanged("RoughServerUpTime");
                    OnPropertyChanged("RoughClientUpTime");
                    OnPropertyChanged("OnlineCount");
                    OnPropertyChanged("OnlineFriendsCount");
                    OnPropertyChanged("OnlineBookmarksCount");
                };
            }

            catch (Exception ex)
            {
                ex.Source = "Utility Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        protected override void SendMessage()
        {
            UpdateError("Cannot send messages to this channel!");
        }
        #endregion

        #region Commands
        private RelayCommand _saveChannels;
        public ICommand SaveChannelsCommand
        {
            get
            {
                if (_saveChannels == null)
                    _saveChannels = new RelayCommand(args =>
                        {
                            Settings.Default.SavedChannels = new System.Collections.Specialized.StringCollection();

                            foreach (var channel in CM.CurrentChannels)
                            {
                                if (!(channel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase)))
                                    Settings.Default.SavedChannels.Add(channel.ID);
                            }

                            Settings.Default.Save();
                            UpdateError("Channels saved.");
                        });
                return _saveChannels;
            }
        }

        #endregion

        protected override void Dispose(bool IsManagedDispose)
        {
            base.Dispose();

            if (IsManagedDispose)
            {
                UpdateTimer.Dispose();
                Model = null;
            }
        }
    }
}
