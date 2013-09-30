// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManageListsViewModel.cs" company="Justin Kadrovach">
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
//   The manage lists view model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Utilities;
    using Slimcat.Views;

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
        /// Initializes a new instance of the <see cref="ManageListsViewModel"/> class.
        /// </summary>
        /// <param name="cm">
        /// The cm.
        /// </param>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="eventagg">
        /// The eventagg.
        /// </param>
        public ManageListsViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            this.Container.RegisterType<object, ManageListsTabView>(ManageListsTabView);

            this.genderSettings = new GenderSettingsModel();
            this.SearchSettings.ShowNotInterested = true;
            this.SearchSettings.ShowIgnored = true;

            this.SearchSettings.Updated += (s, e) =>
                {
                    this.OnPropertyChanged("SearchSettings");
                    this.UpdateBindings();
                };

            this.GenderSettings.Updated += (s, e) =>
                {
                    this.OnPropertyChanged("GenderSettings");
                    this.UpdateBindings();
                };

            this.ChatModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName.Equals("OnlineFriends", StringComparison.OrdinalIgnoreCase))
                    {
                        this.OnPropertyChanged("Friends");
                    }
                };

            this.Events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                    {
                        var thisChannelUpdate = args as ChannelUpdateModel;
                        if (thisChannelUpdate != null
                            && thisChannelUpdate.Arguments is ChannelUpdateModel.ChannelTypeBannedListEventArgs)
                        {
                            this.OnPropertyChanged("HasBanned");
                            this.OnPropertyChanged("Banned");
                        }

                        var thisUpdate = args as CharacterUpdateModel;
                        if (thisUpdate == null)
                        {
                            return;
                        }

                        var thisArguments = thisUpdate.Arguments as CharacterUpdateModel.ListChangedEventArgs;
                        if (thisArguments == null)
                        {
                            return;
                        }

                        switch (thisArguments.ListArgument)
                        {
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.Interested:
                                this.OnPropertyChanged("Interested");
                                this.OnPropertyChanged("NotInterested");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.Ignored:
                                this.OnPropertyChanged("Ignored");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.NotInterested:
                                this.OnPropertyChanged("NotInterested");
                                this.OnPropertyChanged("Interested");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.Bookmarks:
                                this.OnPropertyChanged("Bookmarks");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.Friends:
                                this.OnPropertyChanged("Friends");
                                break;
                        }
                    }, 
                true);

            cm.SelectedChannelChanged += (s, e) =>
                {
                    this.OnPropertyChanged("HasUsers");
                    this.OnPropertyChanged("Moderators");
                    this.OnPropertyChanged("HasBanned");
                    this.OnPropertyChanged("Banned");
                };
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the banned.
        /// </summary>
        public IList<ICharacter> Banned
        {
            get
            {
                return this.HasUsers ? this.Update(((GeneralChannelModel)this.ChatModel.CurrentChannel).Banned, this.roomBans) : null;
            }
        }

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IList<ICharacter> Bookmarks
        {
            get
            {
                this.bookmarks = this.Update(this.ChatModel.Bookmarks, this.bookmarks);
                return this.bookmarks;
            }
        }

        /// <summary>
        ///     Gets the friends.
        /// </summary>
        public IList<ICharacter> Friends
        {
            get
            {
                this.friends = this.Update(this.ChatModel.Friends, this.friends);
                return this.friends;
            }
        }

        /// <summary>
        ///     Gets the gender settings.
        /// </summary>
        public GenderSettingsModel GenderSettings
        {
            get
            {
                return this.genderSettings;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has banned.
        /// </summary>
        public bool HasBanned
        {
            get
            {
                return this.HasUsers && (((GeneralChannelModel)this.ChatModel.CurrentChannel).Banned.Count > 0);
            }
        }

        /// <summary>
        ///     Gets the ignored.
        /// </summary>
        public IList<ICharacter> Ignored
        {
            get
            {
                this.ignored = this.Update(this.ChatModel.Ignored, this.ignored);
                return this.ignored;
            }
        }

        /// <summary>
        ///     Gets the interested.
        /// </summary>
        public IList<ICharacter> Interested
        {
            get
            {
                this.interested = this.Update(ApplicationSettings.Interested, this.interested);
                return this.interested;
            }
        }

        /// <summary>
        ///     Gets the moderators.
        /// </summary>
        public IList<ICharacter> Moderators
        {
            get
            {
                return this.HasUsers ? this.Update(((GeneralChannelModel)this.ChatModel.CurrentChannel).Moderators, this.roomMods) : null;
            }
        }

        /// <summary>
        ///     Gets the not interested.
        /// </summary>
        public IList<ICharacter> NotInterested
        {
            get
            {
                this.notInterested = this.Update(ApplicationSettings.NotInterested, this.notInterested);
                return this.notInterested;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether show mods.
        /// </summary>
        public bool ShowMods
        {
            get
            {
                return this.ChatModel.CurrentChannel is GeneralChannelModel;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show offline.
        /// </summary>
        public bool ShowOffline
        {
            get
            {
                return this.showOffline;
            }

            set
            {
                this.showOffline = value;
                this.UpdateBindings();
            }
        }

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(
                this.GenderSettings, this.SearchSettings, this.ChatModel, this.ChatModel.CurrentChannel as GeneralChannelModel);
        }

        private IList<ICharacter> Update(ICollection<string> characterNames, IList<ICharacter> currentList)
        {
            if (characterNames == null)
            {
                return currentList;
            }

            if (currentList == null || currentList.Count != characterNames.Count)
            {
                currentList = characterNames
                    .Select(characterName => this.ChatModel.FindCharacter(characterName))
                    .Where(toAdd => toAdd.Status != StatusType.Offline || this.showOffline)
                    .Where(this.MeetsFilter).ToList();
            }

            return currentList;
        }

        private void UpdateBindings()
        {
            this.friends = new List<ICharacter>();
            this.OnPropertyChanged("Friends");

            this.bookmarks = new List<ICharacter>();
            this.OnPropertyChanged("Bookmarks");

            this.interested = new List<ICharacter>();
            this.OnPropertyChanged("Interested");

            this.notInterested = new List<ICharacter>();
            this.OnPropertyChanged("NotInterested");

            this.ignored = new List<ICharacter>();
            this.OnPropertyChanged("Ignored");

            this.roomMods = new List<ICharacter>();
            this.OnPropertyChanged("Moderators");

            this.roomBans = new List<ICharacter>();
            this.OnPropertyChanged("Banned");
        }

        #endregion
    }
}