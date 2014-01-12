// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelTabViewModel.cs" company="Justin Kadrovach">
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
//   The channels tab view model.
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

    using Models;
    using Views;

    /// <summary>
    ///     The channels tab view model.
    /// </summary>
    public class ChannelsTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        /// <summary>
        ///     The channels tab view.
        /// </summary>
        public const string ChannelsTabView = "ChannelsTabView";

        #endregion

        #region Fields

        /// <summary>
        ///     The show only alphabet.
        /// </summary>
        public bool ShowOnlyAlphabet;

        private bool showPrivate = true;

        private bool showPublic = true;

        private bool sortByName;

        private int thresh;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelsTabViewModel"/> class.
        /// </summary>
        /// <param name="cm">
        /// The cm.
        /// </param>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="reggman">
        /// The reggman.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        public ChannelsTabViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager reggman, IEventAggregator events)
            : base(contain, reggman, events, cm)
        {
            this.Container.RegisterType<object, ChannelTabView>(ChannelsTabView);

            this.SearchSettings.Updated += (s, e) =>
                {
                    this.OnPropertyChanged("SearchSettings");
                    this.OnPropertyChanged("SortedChannels");
                };
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets a value indicating whether show private rooms.
        /// </summary>
        public bool ShowPrivateRooms
        {
            get
            {
                return this.showPrivate;
            }

            set
            {
                if (this.showPrivate == value)
                {
                    return;
                }

                this.showPrivate = value;
                this.OnPropertyChanged("SortedChannels");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show public rooms.
        /// </summary>
        public bool ShowPublicRooms
        {
            get
            {
                return this.showPublic;
            }

            set
            {
                if (this.showPublic == value)
                {
                    return;
                }

                this.showPublic = value;
                this.OnPropertyChanged("SortedChannels");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether sort by name.
        /// </summary>
        public bool SortByName
        {
            get
            {
                return this.sortByName;
            }

            set
            {
                if (this.sortByName == value)
                {
                    return;
                }

                this.sortByName = value;
                this.OnPropertyChanged("SortedChannels");
            }
        }

        /// <summary>
        ///     Gets the sorted channels.
        /// </summary>
        public IEnumerable<GeneralChannelModel> SortedChannels
        {
            get
            {
                Func<GeneralChannelModel, bool> containsSearchString =
                    channel =>
                    channel.Id.ToLower().Contains(this.SearchSettings.SearchString)
                    || channel.Title.ToLower().Contains(this.SearchSettings.SearchString);

                Func<GeneralChannelModel, bool> meetsThreshold = channel => channel.UserCount >= this.Threshold;

                Func<GeneralChannelModel, bool> meetsTypeFilter =
                    channel =>
                    ((channel.Type == ChannelType.Public) && this.showPublic)
                    || ((channel.Type == ChannelType.Private) && this.showPrivate);


                Func<GeneralChannelModel, bool> meetsFilter =
                    channel => containsSearchString(channel) && meetsTypeFilter(channel) && meetsThreshold(channel);

                return this.SortByName 
                    ? this.ChatModel.AllChannels.Where(meetsFilter).OrderBy(channel => channel.Title) 
                    : this.ChatModel.AllChannels.Where(meetsFilter).OrderByDescending(channel => channel.UserCount);
            }
        }

        /// <summary>
        ///     Gets or sets the threshold.
        /// </summary>
        public int Threshold
        {
            get
            {
                return this.thresh;
            }

            set
            {
                if (this.thresh == value || value <= 0 || value >= 1000)
                {
                    return;
                }

                this.thresh = value;
                this.OnPropertyChanged("SortedChannels");
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
        }

        #endregion
    }
}