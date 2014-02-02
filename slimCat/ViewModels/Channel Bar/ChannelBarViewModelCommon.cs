#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelBarViewModelCommon.cs">
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

namespace slimCat.ViewModels
{
    #region Usings

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;

    #endregion

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
        ///     Initializes a new instance of the <see cref="ChannelbarViewModelCommon" /> class.
        /// </summary>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="events">
        ///     The events.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        protected ChannelbarViewModelCommon(
            IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager lists)
            : base(contain, regman, events, cm, lists)
        {
            cm.SelectedChannelChanged += (s, e) => OnPropertyChanged("HasUsers");
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
                if (ChatModel.CurrentChannel == null)
                    return false;

                return (ChatModel.CurrentChannel.Type != ChannelType.PrivateMessage)
                       && ChatModel.CurrentChannel.Type != ChannelType.Utility;
            }
        }

        /// <summary>
        ///     Gets the search settings.
        /// </summary>
        public GenericSearchSettingsModel SearchSettings
        {
            get { return searchSettings; }
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