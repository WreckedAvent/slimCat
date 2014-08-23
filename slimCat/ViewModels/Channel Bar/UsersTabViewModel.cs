#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UsersTabViewModel.cs">
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
    using System.Collections.Generic;
    using System.Linq;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     On the channel bar (right-hand side) the 'users' tab, only it shows only the users in the current channel
    /// </summary>
    public class UsersTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        public const string UsersTabView = "UsersTabView";

        #endregion

        #region Fields

        private readonly GenderSettingsModel genderSettings;

        private GeneralChannelModel currentChan;

        #endregion

        #region Constructors and Destructors

        public UsersTabViewModel(IChatState chatState)
            : base(chatState)
        {
            Container.RegisterType<object, UsersTabView>(UsersTabView);
            genderSettings = new GenderSettingsModel();

            SearchSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("SortedUsers");
                    OnPropertyChanged("SearchSettings");
                };

            GenderSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("GenderSettings");
                    OnPropertyChanged("SortedUsers");
                };

            ChatModel.SelectedChannelChanged += (s, e) =>
                {
                    currentChan = null;
                    OnPropertyChanged("SortContentString");
                    OnPropertyChanged("SortedUsers");
                };

            Events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                    {
                        var thisNotification = args as CharacterUpdateModel;
                        if (thisNotification == null)
                            return;

                        if (thisNotification.Arguments is PromoteDemoteEventArgs)
                            OnPropertyChanged("HasPermissions");


                        else if (thisNotification.Arguments is JoinLeaveEventArgs
                                 || thisNotification.Arguments is CharacterListChangedEventArgs)
                            OnPropertyChanged("SortedUsers");
                    });
        }

        #endregion

        #region Public Properties

        public GenderSettingsModel GenderSettings
        {
            get { return genderSettings; }
        }

        public GeneralChannelModel SelectedChan
        {
            get { return currentChan ?? ChatModel.CurrentChannel as GeneralChannelModel; }
        }

        public string SortContentString
        {
            get { return HasUsers ? SelectedChan.Title : null; }
        }

        public IEnumerable<ICharacter> SortedUsers
        {
            get
            {
                var channel = ChatModel.CurrentChannel as GeneralChannelModel;
                if (HasUsers && channel != null)
                {
                    return
                        channel.CharacterManager.SortedCharacters.Where(MeetsFilter)
                            .OrderBy(RelationshipToUser)
                            .ThenBy(x => x.Name);
                }
                return null;
            }
        }

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(
                GenderSettings, SearchSettings, CharacterManager, ChatModel.CurrentChannel as GeneralChannelModel);
        }

        private string RelationshipToUser(ICharacter character)
        {
            return character.RelationshipToUser(CharacterManager, ChatModel.CurrentChannel as GeneralChannelModel);
        }

        #endregion
    }
}