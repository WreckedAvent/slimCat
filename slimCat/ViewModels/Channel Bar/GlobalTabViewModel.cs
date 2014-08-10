#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalTabViewModel.cs">
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

        private readonly GenderSettingsModel genderSettings;

        #endregion

        #region Constructors and Destructors

        public GlobalTabViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg,
            ICharacterManager manager)
            : base(contain, regman, eventagg, cm, manager)
        {
            Container.RegisterType<object, GlobalTabView>(GlobalTabView);
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

            Events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                    {
                        var thisNotification = args as CharacterUpdateModel;
                        if (thisNotification == null)
                            return;

                        if (thisNotification.Arguments is CharacterUpdateModel.ListChangedEventArgs
                            || thisNotification.Arguments is CharacterUpdateModel.LoginStateChangedEventArgs)
                            OnPropertyChanged("SortedUsers");
                    });
        }

        #endregion

        #region Public Properties

        public GenderSettingsModel GenderSettings
        {
            get { return genderSettings; }
        }

        public string SortContentString
        {
            get { return "Global"; }
        }

        public IEnumerable<ICharacter> SortedUsers
        {
            get
            {
                return
                    CharacterManager.SortedCharacters.Where(MeetsFilter).OrderBy(RelationshipToUser).ThenBy(x => x.Name);
            }
        }

        #endregion

        #region Methods

        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(GenderSettings, SearchSettings, CharacterManager, null);
        }

        private string RelationshipToUser(ICharacter character)
        {
            return character.RelationshipToUser(CharacterManager, null);
        }

        #endregion
    }
}