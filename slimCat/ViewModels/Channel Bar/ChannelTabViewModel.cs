#region Copyright

// <copyright file="ChannelTabViewModel.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
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
    ///     The channels tab view model.
    /// </summary>
    public class ChannelsTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        public const string ChannelsTabView = "ChannelsTabView";

        #endregion

        #region Constructors and Destructors

        public ChannelsTabViewModel(IChatState chatState, IUpdateChannelLists updater)
            : base(chatState)
        {
            this.updater = updater;
            Container.RegisterType<object, ChannelTabView>(ChannelsTabView);

            SearchSettings.Updated += Update;

            ChatModel.AllChannels.CollectionChanged += Update;
            updateChannelList = DeferredAction.Create(() => OnPropertyChanged("SortedChannels"));
        }

        #endregion

        public bool IsCreatingNewChannel
        {
            get { return isCreatingNewChannel; }
            set
            {
                isCreatingNewChannel = value;
                OnPropertyChanged();
            }
        }

        public ICommand ToggleIsCreatingNewChannelCommand => toggleIsCreatingNewChannel ??
                                                             (toggleIsCreatingNewChannel =
                                                                 new RelayCommand(
                                                                     _ => IsCreatingNewChannel = !IsCreatingNewChannel))
            ;

        public ICommand CreateNewChannelCommand
            => createNewChannel ?? (createNewChannel = new RelayCommand(CreateNewChannelEvent));

        public string NewChannelName
        {
            get { return newChannelName; }
            set
            {
                newChannelName = value;
                OnPropertyChanged();
            }
        }

        private void Update(object sender, EventArgs e)
        {
            OnPropertyChanged("SearchSettings");
            updateChannelList.Defer(TimeSpan.FromSeconds(0.25));
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

        public void CreateNewChannelEvent(object args)
        {
            IsCreatingNewChannel = false;
            Events.SendUserCommand("makeroom", new[] {NewChannelName});
            NewChannelName = string.Empty;
        }

        #region Fields

        private readonly DeferredAction updateChannelList;
        private readonly IUpdateChannelLists updater;
        public bool ShowOnlyAlphabet;

        private RelayCommand createNewChannel;

        private bool isCreatingNewChannel;

        private string newChannelName;

        private bool showPrivate = true;

        private bool showPublic = true;

        private bool sortByName;

        private RelayCommand toggleIsCreatingNewChannel;

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
            get { return ApplicationSettings.ChannelDisplayThreshold; }

            set
            {
                if (ApplicationSettings.ChannelDisplayThreshold == value || value < 0 || value >= 1000)
                    return;

                ApplicationSettings.ChannelDisplayThreshold = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                updateChannelList.Defer(TimeSpan.FromSeconds(1));
            }
        }

        public void UpdateChannels()
        {
            updater.UpdateChannels();
        }

        #endregion
    }
}