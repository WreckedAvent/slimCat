#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelTabViewModel.cs">
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

    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Views;

    #endregion

    /// <summary>
    ///     The channels tab view model.
    /// </summary>
    public class ChannelsTabViewModel : ChannelbarViewModelCommon
    {

        #region Constants

        public const string ChannelsTabView = "ChannelsTabView";

        #endregion

        #region Fields

        public bool ShowOnlyAlphabet;

        private bool showPrivate = true;

        private bool showPublic = true;

        private bool sortByName;

        private int thresh = 5;

        private readonly IChannelListUpdater updater;

        #endregion

        #region Constructors and Destructors

        public ChannelsTabViewModel(IChatState chatState, IChannelListUpdater updater)
            : base(chatState)
        {
            this.updater = updater;
            Container.RegisterType<object, ChannelTabView>(ChannelsTabView);

            SearchSettings.Updated += Update;

            ChatModel.AllChannels.CollectionChanged += Update;
        }

        #endregion

        #region Public Properties

        public bool ShowPrivateRooms
        {
            get { return showPrivate; }

            set
            {
                if (showPrivate == value)
                    return;

                showPrivate = value;
                OnPropertyChanged("SortedChannels");
            }
        }

        public bool ShowPublicRooms
        {
            get { return showPublic; }

            set
            {
                if (showPublic == value)
                    return;

                showPublic = value;
                OnPropertyChanged("SortedChannels");
            }
        }

        public bool SortByName
        {
            get { return sortByName; }

            set
            {
                if (sortByName == value)
                    return;

                sortByName = value;
                OnPropertyChanged("SortedChannels");
            }
        }

        public void UpdateChannels()
        {    
            updater.UpdateChannels();
        }

        public IEnumerable<GeneralChannelModel> SortedChannels
        {
            get
            {
                Func<GeneralChannelModel, bool> containsSearchString =
                    channel =>
                        channel.Id.ToLower().Contains(SearchSettings.SearchString)
                        || channel.Title.ToLower().Contains(SearchSettings.SearchString);

                Func<GeneralChannelModel, bool> meetsThreshold = channel => channel.UserCount >= Threshold;

                Func<GeneralChannelModel, bool> meetsTypeFilter =
                    channel =>
                        ((channel.Type == ChannelType.Public) && showPublic)
                        || ((channel.Type == ChannelType.Private) && showPrivate);


                Func<GeneralChannelModel, bool> meetsFilter =
                    channel => containsSearchString(channel) && meetsTypeFilter(channel) && meetsThreshold(channel);

                return SortByName
                    ? ChatModel.AllChannels.Where(meetsFilter).OrderBy(channel => channel.Title)
                    : ChatModel.AllChannels.Where(meetsFilter).OrderByDescending(channel => channel.UserCount);
            }
        }

        public int Threshold
        {
            get { return thresh; }

            set
            {
                if (thresh == value || value <= 0 || value >= 1000)
                    return;

                thresh = value;
                OnPropertyChanged("SortedChannels");
            }
        }

        #endregion

        private void Update(object sender, EventArgs e)
        {
            OnPropertyChanged("SearchSettings");
            OnPropertyChanged("SortedChannels");
        }

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                ChatModel.AllChannels.CollectionChanged -= Update;
                SearchSettings.Updated -= Update;

            }
            base.Dispose(isManaged);
        }
    }
}