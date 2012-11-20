using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Views;

namespace ViewModels
{
    /// <summary>
    /// On the channel bar (right-hand side) the 'users' tab, only it shows the entire list
    /// </summary>
    public class GlobalTabViewModel : ChannelbarViewModelCommon
    {
        #region Fields
        private GenderSettingsModel _genderSettings;
        public const string GlobalTabView = "GlobalTabView";
        private int _listCacheCount = -1;
        private const int _updateUsersTabResolution = 5 * 1000; // in ms, how often we check for a change in the users list
        private System.Timers.Timer _updateTick; // this fixes various issues with the users list not updating
        #endregion

        #region Properties
        public GenderSettingsModel GenderSettings { get { return _genderSettings; } }
        public GeneralChannelModel SelectedChan { get { return CM.SelectedChannel as GeneralChannelModel; } }

        public IEnumerable<ICharacter> SortedUsers
        {
            get
            {
                return CM.OnlineCharacters
                            .Where(MeetsFilter)
                            .OrderBy(RelationshipToUser)
                            .ThenBy(x => x.Name);
            }
        }

        public string SortContentString
        {
            get
            {
                return "Global";
            }
        }
        #endregion

        #region filter functions
        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(GenderSettings, SearchSettings, CM, CM.SelectedChannel as GeneralChannelModel);
        }

        private string RelationshipToUser(ICharacter character)
        {
            return character.RelationshipToUser(CM, CM.SelectedChannel as GeneralChannelModel);
        }
        #endregion

        #region Constructors
        public GlobalTabViewModel(IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            _container.RegisterType<object, GlobalTabView>(GlobalTabView);
            _genderSettings = new GenderSettingsModel();

            SearchSettings.Updated += (s, e) =>
            {
                OnPropertyChanged("SortedUsers");
                OnPropertyChanged("SearchSettings");
            };

            GenderSettings.Updated += (s, e) =>
            {
                OnPropertyChanged("GenderSettings");
                OnPropertyChanged("SortedUsers");
            };

            CM.SelectedChannelChanged += (s, e) =>
            {
                OnPropertyChanged("SortContentString");
                OnPropertyChanged("SortedUsers");
            };

            _updateTick = new System.Timers.Timer(_updateUsersTabResolution);
            _updateTick.Elapsed += TickUpdateEvent;
            _updateTick.Start();
            _updateTick.AutoReset = true;
        }
        #endregion

        #region Methods
        private void TickUpdateEvent(object sender = null, EventArgs e = null)
        {
            if (SortedUsers != null)
            {
                try
                {
                    if (_listCacheCount != -1)
                    {
                        if (SortedUsers.Count() != _listCacheCount)
                        {
                            OnPropertyChanged("SortedUsers");
                            _listCacheCount = SortedUsers.Count();
                        }

                    }
                    else
                        _listCacheCount = SortedUsers.Count();
                }

                catch (InvalidOperationException) // if our collection changes while we're going through it, then it's obviously changed
                {
                    OnPropertyChanged("SortedUsers");
                }
            }
        }
        #endregion
    }
}
