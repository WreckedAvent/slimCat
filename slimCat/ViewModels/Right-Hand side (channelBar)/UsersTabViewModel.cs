// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UsersTabViewModel.cs" company="Justin Kadrovach">
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
//   On the channel bar (right-hand side) the 'users' tab, only it shows only the users in the current channel
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;
    using Utilities;
    using Views;

    /// <summary>
    ///     On the channel bar (right-hand side) the 'users' tab, only it shows only the users in the current channel
    /// </summary>
    public class UsersTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        /// <summary>
        ///     The users tab view.
        /// </summary>
        public const string UsersTabView = "UsersTabView";

        #endregion

        #region Fields

        private readonly GenderSettingsModel genderSettings;

        private readonly Timer updateTick = new Timer(3000);

        private GeneralChannelModel currentChan;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersTabViewModel"/> class.
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
        public UsersTabViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            this.Container.RegisterType<object, UsersTabView>(UsersTabView);
            this.genderSettings = new GenderSettingsModel();

            this.SearchSettings.Updated += (s, e) =>
                {
                    this.OnPropertyChanged("SortedUsers");
                    this.OnPropertyChanged("SearchSettings");
                };

            this.GenderSettings.Updated += (s, e) =>
                {
                    this.OnPropertyChanged("GenderSettings");
                    this.OnPropertyChanged("SortedUsers");
                };

            this.ChatModel.SelectedChannelChanged += (s, e) =>
                {
                    this.currentChan = null;
                    this.OnPropertyChanged("SortContentString");
                    this.OnPropertyChanged("SortedUsers");
                };

            this.Events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                    {
                        var thisNotification = args as CharacterUpdateModel;
                        if (thisNotification == null)
                        {
                            return;
                        }

                        if (thisNotification.Arguments is CharacterUpdateModel.PromoteDemoteEventArgs)
                        {
                            this.OnPropertyChanged("HasPermissions");
                        }
                    });

            this.updateTick.Elapsed += this.OnChannelListUpdated;
            this.updateTick.Start();
        }

        #endregion

        #region Public Properties

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
        ///     Gets the selected chan.
        /// </summary>
        public GeneralChannelModel SelectedChan
        {
            get
            {
                return this.currentChan ?? this.ChatModel.CurrentChannel as GeneralChannelModel;
            }
        }

        /// <summary>
        ///     Gets the sort content string.
        /// </summary>
        public string SortContentString
        {
            get
            {
                return this.HasUsers ? this.SelectedChan.Title : null;
            }
        }

        /// <summary>
        ///     Gets the sorted users.
        /// </summary>
        public IEnumerable<ICharacter> SortedUsers
        {
            get
            {
                if (this.HasUsers)
                {
                    lock (this.SelectedChan.Users)
                    {
                        return
                            this.SelectedChan.Users.Where(this.MeetsFilter)
                                .OrderBy(this.RelationshipToUser)
                                .ThenBy(x => x.Name);
                    }
                }
                return null;
            }
        }

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(
                this.GenderSettings, this.SearchSettings, this.ChatModel, this.ChatModel.CurrentChannel as GeneralChannelModel);
        }

        private void OnChannelListUpdated(object sender, EventArgs e)
        {
            if (this.SelectedChan != null)
            {
                this.OnPropertyChanged("SortedUsers");
            }
        }

        private string RelationshipToUser(ICharacter character)
        {
            return character.RelationshipToUser(this.ChatModel, this.ChatModel.CurrentChannel as GeneralChannelModel);
        }

        #endregion
    }
}