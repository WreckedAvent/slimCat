#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalTabViewModel.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libraries;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     On the channel bar (right-hand side) the 'users' tab, only it shows the entire list
    /// </summary>
    public class GlobalTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        public const string GlobalTabView = "GlobalTabView";

        #endregion

        #region Fields

        private readonly DeferredAction updateUserList;

        #endregion

        #region Constructors and Destructors

        public GlobalTabViewModel(IChatState chatState)
            : base(chatState)
        {
            Container.RegisterType<object, GlobalTabView>(GlobalTabView);
            GenderSettings = new GenderSettingsModel();

            SearchSettings.Updated += OnSearchSettingsUpdated;
            GenderSettings.Updated += OnSearchSettingsUpdated;

            Events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                {
                    var thisNotification = args as CharacterUpdateModel;
                    if (thisNotification == null)
                        return;

                    if (thisNotification.Arguments is CharacterListChangedEventArgs
                        || thisNotification.Arguments is LoginStateChangedEventArgs)
                        OnPropertyChanged("SortedUsers");
                });


            updateUserList = DeferredAction.Create(() => OnPropertyChanged("SortedUsers"));
        }

        #endregion

        #region Public Properties

        public GenderSettingsModel GenderSettings { get; }

        public string SortContentString => "Global";

        public IEnumerable<ICharacter> SortedUsers => CharacterManager.SortedCharacters.Where(MeetsFilter).OrderBy(RelationshipToUser).ThenBy(x => x.Name);

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character) => character.MeetsFilters(GenderSettings, SearchSettings, CharacterManager, null);

        private string RelationshipToUser(ICharacter character) => character.RelationshipToUser(CharacterManager, null);

        private void OnSearchSettingsUpdated(object sender, EventArgs e)
        {
            OnPropertyChanged("GenderSettings");
            OnPropertyChanged("SearchSettings");
            updateUserList.Defer(Constants.SearchDebounce);
        }

        #endregion
    }
}