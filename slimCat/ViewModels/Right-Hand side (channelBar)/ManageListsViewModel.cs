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

namespace ViewModels
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using slimCat;

    using Views;

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

        private readonly GenderSettingsModel _genderSettings;

        private IList<ICharacter> _bookmarks;

        private IList<ICharacter> _friends;

        private IList<ICharacter> _ignored;

        private IList<ICharacter> _interested;

        private IList<ICharacter> _notInterested;

        private IList<ICharacter> _roomBans;

        private IList<ICharacter> _roomMods;

        private bool _showOffline = true;

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
            this._container.RegisterType<object, ManageListsTabView>(ManageListsTabView);

            this._genderSettings = new GenderSettingsModel();
            this.SearchSettings.ShowNotInterested = true;
            this.SearchSettings.ShowIgnored = true;

            this.SearchSettings.Updated += (s, e) =>
                {
                    this.OnPropertyChanged("SearchSettings");
                    this.updateBindings();
                };

            this.GenderSettings.Updated += (s, e) =>
                {
                    this.OnPropertyChanged("GenderSettings");
                    this.updateBindings();
                };

            this._events.GetEvent<NewUpdateEvent>().Subscribe(
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
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.interested:
                                this.OnPropertyChanged("Interested");
                                this.OnPropertyChanged("NotInterested");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.ignored:
                                this.OnPropertyChanged("Ignored");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.notinterested:
                                this.OnPropertyChanged("NotInterested");
                                this.OnPropertyChanged("Interested");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.bookmarks:
                                this.OnPropertyChanged("Bookmarks");
                                break;
                            case CharacterUpdateModel.ListChangedEventArgs.ListType.friends:
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
                if (this.HasUsers)
                {
                    return this.update(((GeneralChannelModel)this.CM.SelectedChannel).Banned, this._roomBans);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IList<ICharacter> Bookmarks
        {
            get
            {
                this._bookmarks = this.update(this._cm.Bookmarks, this._bookmarks);
                return this._bookmarks;
            }
        }

        /// <summary>
        ///     Gets the friends.
        /// </summary>
        public IList<ICharacter> Friends
        {
            get
            {
                this._friends = this.update(this._cm.Friends, this._friends);
                return this._friends;
            }
        }

        /// <summary>
        ///     Gets the gender settings.
        /// </summary>
        public GenderSettingsModel GenderSettings
        {
            get
            {
                return this._genderSettings;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has banned.
        /// </summary>
        public bool HasBanned
        {
            get
            {
                return this.HasUsers && (((GeneralChannelModel)this.CM.SelectedChannel).Banned.Count > 0);
            }
        }

        /// <summary>
        ///     Gets the ignored.
        /// </summary>
        public IList<ICharacter> Ignored
        {
            get
            {
                this._ignored = this.update(this.CM.Ignored, this._ignored);
                return this._ignored;
            }
        }

        /// <summary>
        ///     Gets the interested.
        /// </summary>
        public IList<ICharacter> Interested
        {
            get
            {
                this._interested = this.update(ApplicationSettings.Interested, this._interested);
                return this._interested;
            }
        }

        /// <summary>
        ///     Gets the moderators.
        /// </summary>
        public IList<ICharacter> Moderators
        {
            get
            {
                if (this.HasUsers)
                {
                    return this.update(((GeneralChannelModel)this.CM.SelectedChannel).Moderators, this._roomMods);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///     Gets the not interested.
        /// </summary>
        public IList<ICharacter> NotInterested
        {
            get
            {
                this._notInterested = this.update(ApplicationSettings.NotInterested, this._notInterested);
                return this._notInterested;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether show mods.
        /// </summary>
        public bool ShowMods
        {
            get
            {
                return this._cm.SelectedChannel is GeneralChannelModel;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show offline.
        /// </summary>
        public bool ShowOffline
        {
            get
            {
                return this._showOffline;
            }

            set
            {
                this._showOffline = value;
                this.updateBindings();
            }
        }

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(
                this.GenderSettings, this.SearchSettings, this.CM, this.CM.SelectedChannel as GeneralChannelModel);
        }

        private IList<ICharacter> update(IList<string> CharacterNames, IList<ICharacter> CurrentList)
        {
            if (CharacterNames == null)
            {
                return CurrentList;
            }

            if (CurrentList == null || CurrentList.Count != CharacterNames.Count)
            {
                CurrentList = new List<ICharacter>();
                foreach (string characterName in CharacterNames)
                {
                    ICharacter toAdd = this.CM.FindCharacter(characterName);

                    if (toAdd.Status == StatusType.offline && !this._showOffline)
                    {
                        continue;
                    }

                    if (this.MeetsFilter(toAdd))
                    {
                        CurrentList.Add(toAdd);
                    }
                }
            }

            return CurrentList;
        }

        private void updateBindings()
        {
            this._friends = new List<ICharacter>();
            this.OnPropertyChanged("Friends");

            this._bookmarks = new List<ICharacter>();
            this.OnPropertyChanged("Bookmarks");

            this._interested = new List<ICharacter>();
            this.OnPropertyChanged("Interested");

            this._notInterested = new List<ICharacter>();
            this.OnPropertyChanged("NotInterested");

            this._ignored = new List<ICharacter>();
            this.OnPropertyChanged("Ignored");

            this._roomMods = new List<ICharacter>();
            this.OnPropertyChanged("Moderators");

            this._roomBans = new List<ICharacter>();
            this.OnPropertyChanged("Banned");
        }

        #endregion
    }
}