#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManageListsViewModel.cs">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     The manage lists view model.
    /// </summary>
    public class ManageListsViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        public const string ManageListsTabView = "ManageListsTabView";

        #endregion

        #region Fields

        private readonly GenderSettingsModel genderSettings;

        private readonly DeferredAction updateLists;
        private readonly TimeSpan searchDebounce = TimeSpan.FromMilliseconds(250);

        private readonly IDictionary<ListKind, string> listKinds = new Dictionary<ListKind, string>
        {
            {ListKind.Banned, "Banned"},
            {ListKind.Bookmark, "Bookmarks"},
            {ListKind.Friend, "Friends"},
            {ListKind.Moderator, "Moderators"},
            {ListKind.Interested, "Interested"},
            {ListKind.NotInterested, "NotInterested"},
            {ListKind.Ignored, "Ignored"}
        };

        private bool showOffline;
        private bool hasNewSearchResults;

        private RelayCommand clearSearch;

        #endregion

        #region Constructors and Destructors

        public ManageListsViewModel(IChatState chatState)
            : base(chatState)
        {
            Container.RegisterType<object, ManageListsTabView>(ManageListsTabView);

            genderSettings = new GenderSettingsModel();
            SearchSettings.ShowNotInterested = true;
            SearchSettings.ShowIgnored = true;

            SearchSettings.Updated += OnSearchSettingsUpdated;
            GenderSettings.Updated += OnSearchSettingsUpdated;

            ChatModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals("OnlineFriends", StringComparison.OrdinalIgnoreCase))
                    OnPropertyChanged("Friends");
            };

            Events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                {
                    var thisChannelUpdate = args as ChannelUpdateModel;
                    if (thisChannelUpdate != null
                        && (thisChannelUpdate.Arguments is ChannelTypeBannedListEventArgs
                            || thisChannelUpdate.Arguments is ChannelDisciplineEventArgs))
                    {
                        OnPropertyChanged("HasBanned");
                        OnPropertyChanged("Banned");
                    }

                    var thisUpdate = args as CharacterUpdateModel;
                    if (thisUpdate == null)
                        return;

                    var name = thisUpdate.TargetCharacter.Name;

                    var joinLeaveArguments = thisUpdate.Arguments as JoinLeaveEventArgs;
                    if (joinLeaveArguments != null)
                    {
                        if (CharacterManager.IsOnList(name, ListKind.Moderator, false))
                            OnPropertyChanged("Moderators");
                        if (CharacterManager.IsOnList(name, ListKind.Banned, false))
                            OnPropertyChanged("Banned");
                        return;
                    }

                    var signInOutArguments = thisUpdate.Arguments as LoginStateChangedEventArgs;
                    if (signInOutArguments != null)
                    {
                        listKinds.Each(x =>
                        {
                            if (CharacterManager.IsOnList(name, x.Key, false))
                                OnPropertyChanged(x.Value);
                        });

                        return;
                    }

                    var thisArguments = thisUpdate.Arguments as CharacterListChangedEventArgs;
                    if (thisArguments == null)
                        return;

                    switch (thisArguments.ListArgument)
                    {
                        case ListKind.Interested:
                            OnPropertyChanged("Interested");
                            OnPropertyChanged("NotInterested");
                            break;
                        case ListKind.Ignored:
                            OnPropertyChanged("Ignored");
                            break;
                        case ListKind.NotInterested:
                            OnPropertyChanged("NotInterested");
                            OnPropertyChanged("Interested");
                            break;
                        case ListKind.Bookmark:
                            OnPropertyChanged("Bookmarks");
                            break;
                        case ListKind.Friend:
                            OnPropertyChanged("Friends");
                            break;
                        case ListKind.SearchResult:
                            OnPropertyChanged("SearchResults");
                            OnPropertyChanged("HasSearchResults");
                            break;
                    }
                },
                true);

            ChatModel.SelectedChannelChanged += (s, e) =>
            {
                OnPropertyChanged("HasUsers");
                OnPropertyChanged("Moderators");
                OnPropertyChanged("HasBanned");
                OnPropertyChanged("Banned");
            };

            Events.GetEvent<ChatSearchResultEvent>().Subscribe(_ =>
            {
                OnPropertyChanged("SearchResults");
                OnPropertyChanged("HasSearchResults");
                HasNewSearchResults = true;
            });

            updateLists = DeferredAction.Create(UpdateBindings);
        }

        #endregion

        #region Public Properties

        public IEnumerable<ICharacter> Banned
        {
            get { return GetChannelList(ListKind.Banned, false); }
        }

        public IEnumerable<ICharacter> Bookmarks
        {
            get { return GetGlobalList(ListKind.Bookmark); }
        }

        public IEnumerable<ICharacter> Friends
        {
            get { return GetGlobalList(ListKind.Friend); }
        }

        public IEnumerable<ICharacter> SearchResults
        {
            get { return GetGlobalList(ListKind.SearchResult); }
        }

        public GenderSettingsModel GenderSettings
        {
            get { return genderSettings; }
        }

        public bool HasBanned
        {
            get { return Banned.Any(); }
        }

        public bool HasSearchResults
        {
            get { return SearchResults.Any(); }
        }

        public bool HasNewSearchResults
        {
            get { return hasNewSearchResults; }
            set
            {
                hasNewSearchResults = value; 
                OnPropertyChanged("HasNewSearchResults");
            }
        }

        public IEnumerable<ICharacter> Ignored
        {
            get { return GetGlobalList(ListKind.Ignored); }
        }

        public IEnumerable<ICharacter> Interested
        {
            get { return GetGlobalList(ListKind.Interested); }
        }

        public IEnumerable<ICharacter> Moderators
        {
            get { return GetChannelList(ListKind.Moderator, !showOffline); }
        }

        public IEnumerable<ICharacter> NotInterested
        {
            get { return GetGlobalList(ListKind.NotInterested); }
        }

        public bool ShowMods
        {
            get { return ChatModel.CurrentChannel is GeneralChannelModel; }
        }

        public bool ShowOffline
        {
            get { return showOffline; }

            set
            {
                showOffline = value;
                UpdateBindings();
            }
        }

        public ICommand ClearSearchResultsCommand
        {
            get
            {
                return clearSearch ??
                       (clearSearch =
                           new RelayCommand(_ =>
                           {
                               CharacterManager.Set(new List<string>(), ListKind.SearchResult);
                               OnPropertyChanged("SearchResults");
                               OnPropertyChanged("HasSearchResults");
                           }));
            }
        }

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(
                GenderSettings, SearchSettings, CharacterManager, ChatModel.CurrentChannel as GeneralChannelModel);
        }

        private IEnumerable<ICharacter> GetList(ICharacterManager manager, ListKind listKind, bool onlineOnly = true)
        {
            return
                manager.GetCharacters(listKind, onlineOnly)
                    .Where(x => !onlineOnly || x.Status != StatusType.Offline)
                    .Where(MeetsFilter)
                    .OrderBy(x => x.Name);
        }

        private IEnumerable<ICharacter> GetGlobalList(ListKind listkind)
        {
            return GetList(CharacterManager, listkind, !showOffline);
        }

        private IEnumerable<ICharacter> GetChannelList(ListKind listKind, bool onlineOnly)
        {
            var channel = ChatModel.CurrentChannel as GeneralChannelModel;
            if (HasUsers && channel != null)
                return GetList(channel.CharacterManager, listKind, onlineOnly);

            return new List<ICharacter>();
        } 

        private void UpdateBindings()
        {
            OnPropertyChanged("Friends");
            OnPropertyChanged("Bookmarks");
            OnPropertyChanged("Interested");
            OnPropertyChanged("NotInterested");
            OnPropertyChanged("Ignored");
            OnPropertyChanged("Moderators");
            OnPropertyChanged("Banned");
            OnPropertyChanged("SearchResults");
        }

        private void OnSearchSettingsUpdated(object sender, EventArgs e)
        {
            OnPropertyChanged("SearchSettings");
            OnPropertyChanged("GenderSettings");
            updateLists.Defer(searchDebounce);
        }

        #endregion
    }
}