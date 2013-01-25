using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Views;

namespace ViewModels
{
    public class ManageListsViewModel : ChannelbarViewModelCommon
    {
        #region Fields
        private GenderSettingsModel _genderSettings;
        public const string ManageListsTabView = "ManageListsTabView";
        IList<ICharacter> _friends;
        IList<ICharacter> _bookmarks;
        IList<ICharacter> _interested;
        IList<ICharacter> _notInterested;
        IList<ICharacter> _ignored;

        IList<ICharacter> _roomMods;
        IList<ICharacter> _roomBans;

        private bool _showOffline = true;
        #endregion

        #region Properties
        public bool ShowMods { get { return _cm.SelectedChannel is GeneralChannelModel; } }

        public IList<ICharacter> Friends { get { _friends = update(_cm.Friends, _friends); return _friends; } }
        public IList<ICharacter> Bookmarks { get { _bookmarks = update(_cm.Bookmarks, _bookmarks); return _bookmarks; } }
        public IList<ICharacter> Interested { get { _interested = update(ApplicationSettings.Interested, _interested); return _interested; } }
        public IList<ICharacter> NotInterested { get { _notInterested = update(ApplicationSettings.NotInterested, _notInterested); return _notInterested; } }
        public IList<ICharacter> Ignored { get { _ignored = update(CM.Ignored, _ignored); return _ignored; } }

        public IList<ICharacter> Moderators 
        { 
            get 
            {
                if (HasUsers)
                    return update(((GeneralChannelModel)CM.SelectedChannel).Moderators, _roomMods);
                else
                    return null;
            } 
        }

        public IList<ICharacter> Banned
        {
            get
            {
                if (HasUsers)
                    return update(((GeneralChannelModel)CM.SelectedChannel).Banned, _roomBans);
                else
                    return null;
            }
        }

        public GenderSettingsModel GenderSettings { get { return _genderSettings; } }

        public bool ShowOffline
        {
            get { return _showOffline; }
            set
            {
                _showOffline = value;
                updateBindings();
            }
        }

        public bool HasBanned
        {
            get { return HasUsers && (((GeneralChannelModel)CM.SelectedChannel).Banned.Count > 0); }
        }
        #endregion

        #region Methods
        private IList<ICharacter> update(IList<string> CharacterNames, IList<ICharacter> CurrentList)
        {
            if (CharacterNames == null)
                return CurrentList;

            if (CurrentList == null || CurrentList.Count != CharacterNames.Count)
            {
                CurrentList = new List<ICharacter>();
                foreach (var characterName in CharacterNames)
                {
                    var toAdd = CM.FindCharacter(characterName);

                    if (toAdd.Status == StatusType.offline && !_showOffline)
                        continue;
                    if (MeetsFilter(toAdd))
                        CurrentList.Add(toAdd);
                }
            }

            return CurrentList;
        }

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(GenderSettings, SearchSettings, CM, CM.SelectedChannel as GeneralChannelModel);
        }

        private void updateBindings()
        {
            _friends = new List<ICharacter>();
            OnPropertyChanged("Friends");

            _bookmarks = new List<ICharacter>();
            OnPropertyChanged("Bookmarks");

            _interested = new List<ICharacter>();
            OnPropertyChanged("Interested");

            _notInterested = new List<ICharacter>();
            OnPropertyChanged("NotInterested");

            _ignored = new List<ICharacter>();
            OnPropertyChanged("Ignored");

            _roomMods = new List<ICharacter>();
            OnPropertyChanged("Moderators");

            _roomBans = new List<ICharacter>();
            OnPropertyChanged("Banned");
        }
        #endregion

        #region Constructor
        public ManageListsViewModel(IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            _container.RegisterType<object, ManageListsTabView>(ManageListsTabView);

            _genderSettings = new GenderSettingsModel();
            SearchSettings.ShowNotInterested = true;
            SearchSettings.ShowIgnored = true;

            SearchSettings.Updated += (s, e) =>
            {
                OnPropertyChanged("SearchSettings");
                updateBindings();
            };

            GenderSettings.Updated += (s, e) =>
            {
                OnPropertyChanged("GenderSettings");
                updateBindings();
            };

            _events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                {
                    var thisChannelUpdate = args as ChannelUpdateModel;
                    if (thisChannelUpdate != null
                        && thisChannelUpdate.Arguments is ChannelUpdateModel.ChannelTypeBannedListEventArgs)
                    {
                        OnPropertyChanged("HasBanned");
                        OnPropertyChanged("Banned");
                    }

                    var thisUpdate = args as CharacterUpdateModel;
                    if (thisUpdate == null) return;

                    var thisArguments = thisUpdate.Arguments as CharacterUpdateModel.ListChangedEventArgs;
                    if (thisArguments == null) return;

                    switch (thisArguments.ListArgument)
                    {
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.interested:
                            OnPropertyChanged("Interested"); OnPropertyChanged("NotInterested"); break;
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.ignored:
                            OnPropertyChanged("Ignored"); break;
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.notinterested:
                            OnPropertyChanged("NotInterested"); OnPropertyChanged("Interested"); break;
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.bookmarks:
                            OnPropertyChanged("Bookmarks"); break;
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.friends:
                            OnPropertyChanged("Friends"); break;
                    }
                },
                true);

            cm.SelectedChannelChanged += (s, e) => 
            { 
                OnPropertyChanged("HasUsers");
                OnPropertyChanged("Moderators");
                OnPropertyChanged("HasBanned");
                OnPropertyChanged("Banned");
            };

        }
        #endregion
    }
}
