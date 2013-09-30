// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelBarViewModelCommon.cs" company="Justin Kadrovach">
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
//   Contains some things the channelbar viewmodels have in common
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    /// <summary>
    ///     Contains some things the channelbar viewmodels have in common
    /// </summary>
    public class ChannelbarViewModelCommon : ViewModelBase
    {
        #region Fields

        private readonly GenericSearchSettingsModel searchSettings = new GenericSearchSettingsModel();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelbarViewModelCommon"/> class.
        /// </summary>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        protected ChannelbarViewModelCommon(
            IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            cm.SelectedChannelChanged += (s, e) => this.OnPropertyChanged("HasUsers");
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether has users.
        /// </summary>
        public bool HasUsers
        {
            get
            {
                if (this.ChatModel.CurrentChannel == null)
                {
                    return false;
                }

                return (this.ChatModel.CurrentChannel.Type != ChannelType.PrivateMessage)
                       && this.ChatModel.CurrentChannel.Type != ChannelType.Utility;
            }
        }

        /// <summary>
        ///     Gets the search settings.
        /// </summary>
        public GenericSearchSettingsModel SearchSettings
        {
            get
            {
                return this.searchSettings;
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