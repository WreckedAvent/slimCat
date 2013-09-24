// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalTabViewModel.cs" company="Justin Kadrovach">
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
//   On the channel bar (right-hand side) the 'users' tab, only it shows the entire list
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

    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Utilities;
    using Slimcat.Views;

    /// <summary>
    ///     On the channel bar (right-hand side) the 'users' tab, only it shows the entire list
    /// </summary>
    public class GlobalTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        /// <summary>
        ///     The global tab view.
        /// </summary>
        public const string GlobalTabView = "GlobalTabView";

        #endregion

        #region Fields

        private readonly GenderSettingsModel genderSettings;

        private readonly Timer updateTick = new Timer(5000);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalTabViewModel"/> class.
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
        public GlobalTabViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            this.Container.RegisterType<object, GlobalTabView>(GlobalTabView);
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

            this.Events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                    {
                        var thisNotification = args as CharacterUpdateModel;
                        if (thisNotification == null)
                        {
                            return;
                        }

                        var thisArgument = thisNotification.Arguments as CharacterUpdateModel.ListChangedEventArgs;
                        if (thisArgument != null)
                        {
                            this.OnPropertyChanged("SortedUsers");
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
                return this.ChatModel.CurrentChannel as GeneralChannelModel;
            }
        }

        /// <summary>
        ///     Gets the sort content string.
        /// </summary>
        public string SortContentString
        {
            get
            {
                return "Global";
            }
        }

        /// <summary>
        ///     Gets the sorted users.
        /// </summary>
        public IEnumerable<ICharacter> SortedUsers
        {
            get
            {
                lock (this.ChatModel.OnlineCharacters)
                {
                    return
                        this.ChatModel.OnlineCharacters.Where(this.MeetsFilter)
                            .OrderBy(this.RelationshipToUser)
                            .ThenBy(x => x.Name);
                }
            }
        }

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(this.GenderSettings, this.SearchSettings, this.ChatModel, null);
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
            return character.RelationshipToUser(this.ChatModel, null);
        }

        #endregion
    }
}