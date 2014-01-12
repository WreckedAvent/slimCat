#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManageListsViewModel.cs">
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

namespace Slimcat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     The manage lists view model.
    /// </summary>
    public class ManageListsViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        /// <summary>
        ///     The manage lists tab view.
        /// </summary>
        public const string ManageListsTabView = "ManageListsTabView";

        #endregion

        #region Fields

        private readonly GenderSettingsModel genderSettings;

        private IList<ICharacter> bookmarks;

        private IList<ICharacter> friends;

        private IList<ICharacter> ignored;

        private IList<ICharacter> interested;

        private IList<ICharacter> notInterested;

        private IList<ICharacter> roomBans;

        private IList<ICharacter> roomMods;

        private bool showOffline = true;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ManageListsViewModel" /> class.
        /// </summary>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="eventagg">
        ///     The eventagg.
        /// </param>
        public ManageListsViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            Container.RegisterType<object, ManageListsTabView>(ManageListsTabView);

            genderSettings = new GenderSettingsModel();
            SearchSettings.ShowNotInterested = true;
            SearchSettings.ShowIgnored = true;

            SearchSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("SearchSettings");
                    UpdateBindings();
                };

            GenderSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("GenderSettings");
                    UpdateBindings();
                };

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
                            && thisChannelUpdate.Arguments is ChannelUpdateModel.ChannelTypeBannedListEventArgs)
                        {
                            OnPropertyChanged("HasBanned");
                            OnPropertyChanged("Banned");
                        }

                        var thisUpdate = args as CharacterUpdateModel;
                        if (thisUpdate == null)
                            return;

                        var thisArguments = thisUpdate.Arguments as CharacterUpdateModel.ListChangedEventArgs;
                        if (thisArguments == null)
                            return;

                        switch (thisArguments.ListArgument)
                        {
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.Interested:
                                OnPropertyChanged("Interested");
                                OnPropertyChanged("NotInterested");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.Ignored:
                                OnPropertyChanged("Ignored");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.NotInterested:
                                OnPropertyChanged("NotInterested");
                                OnPropertyChanged("Interested");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.Bookmarks:
                                OnPropertyChanged("Bookmarks");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.Friends:
                                OnPropertyChanged("Friends");
                                break;
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

        #region Public Properties

        /// <summary>
        ///     Gets the banned.
        /// </summary>
        public IEnumerable<ICharacter> Banned
        {
            get { return HasUsers ? Update(((GeneralChannelModel) ChatModel.CurrentChannel).Banned, roomBans) : null; }
        }

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IEnumerable<ICharacter> Bookmarks
        {
            get
            {
                bookmarks = Update(ChatModel.Bookmarks, bookmarks);
                return bookmarks;
            }
        }

        /// <summary>
        ///     Gets the friends.
        /// </summary>
        public IEnumerable<ICharacter> Friends
        {
            get
            {
                friends = Update(ChatModel.Friends, friends);
                return friends;
            }
        }

        /// <summary>
        ///     Gets the gender settings.
        /// </summary>
        public GenderSettingsModel GenderSettings
        {
            get { return genderSettings; }
        }

        /// <summary>
        ///     Gets a value indicating whether has banned.
        /// </summary>
        public bool HasBanned
        {
            get { return HasUsers && (((GeneralChannelModel) ChatModel.CurrentChannel).Banned.Count > 0); }
        }

        /// <summary>
        ///     Gets the ignored.
        /// </summary>
        public IEnumerable<ICharacter> Ignored
        {
            get
            {
                ignored = Update(ChatModel.Ignored, ignored);
                return ignored;
            }
        }

        /// <summary>
        ///     Gets the interested.
        /// </summary>
        public IEnumerable<ICharacter> Interested
        {
            get
            {
                interested = Update(ApplicationSettings.Interested, interested);
                return interested;
            }
        }

        /// <summary>
        ///     Gets the moderators.
        /// </summary>
        public IEnumerable<ICharacter> Moderators
        {
            get
            {
                return HasUsers ? Update(((GeneralChannelModel) ChatModel.CurrentChannel).Moderators, roomMods) : null;
            }
        }

        /// <summary>
        ///     Gets the not interested.
        /// </summary>
        public IEnumerable<ICharacter> NotInterested
        {
            get
            {
                notInterested = Update(ApplicationSettings.NotInterested, notInterested);
                return notInterested;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether show mods.
        /// </summary>
        public bool ShowMods
        {
            get { return ChatModel.CurrentChannel is GeneralChannelModel; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show offline.
        /// </summary>
        public bool ShowOffline
        {
            get { return showOffline; }

            set
            {
                showOffline = value;
                UpdateBindings();
            }
        }

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(
                GenderSettings, SearchSettings, ChatModel, ChatModel.CurrentChannel as GeneralChannelModel);
        }

        private IList<ICharacter> Update(ICollection<string> characterNames, IList<ICharacter> currentList)
        {
            if (characterNames == null)
                return currentList;

            if (currentList == null || currentList.Count != characterNames.Count)
            {
                currentList = characterNames
                    .Select(characterName => ChatModel.FindCharacter(characterName))
                    .Where(toAdd => toAdd.Status != StatusType.Offline || showOffline)
                    .Where(MeetsFilter).ToList();
            }

            return currentList;
        }

        private void UpdateBindings()
        {
            friends = new List<ICharacter>();
            OnPropertyChanged("Friends");

            bookmarks = new List<ICharacter>();
            OnPropertyChanged("Bookmarks");

            interested = new List<ICharacter>();
            OnPropertyChanged("Interested");

            notInterested = new List<ICharacter>();
            OnPropertyChanged("NotInterested");

            ignored = new List<ICharacter>();
            OnPropertyChanged("Ignored");

            roomMods = new List<ICharacter>();
            OnPropertyChanged("Moderators");

            roomBans = new List<ICharacter>();
            OnPropertyChanged("Banned");
        }

        #endregion
    }
}