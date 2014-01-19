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

namespace Slimcat.ViewModels
{
    #region Usings

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

    #endregion

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
        ///     Initializes a new instance of the <see cref="GlobalTabViewModel" /> class.
        /// </summary>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="eventagg">
        ///     The eventagg.
        /// </param>
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

                        var thisArgument = thisNotification.Arguments as CharacterUpdateModel.ListChangedEventArgs;
                        if (thisArgument != null)
                            OnPropertyChanged("SortedUsers");
                    });

            updateTick.Elapsed += OnChannelListUpdated;
            updateTick.Start();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the gender settings.
        /// </summary>
        public GenderSettingsModel GenderSettings
        {
            get { return genderSettings; }
        }

        /// <summary>
        ///     Gets the selected chan.
        /// </summary>
        public GeneralChannelModel SelectedChan
        {
            get { return ChatModel.CurrentChannel as GeneralChannelModel; }
        }

        /// <summary>
        ///     Gets the sort content string.
        /// </summary>
        public string SortContentString
        {
            get { return "Global"; }
        }

        /// <summary>
        ///     Gets the sorted users.
        /// </summary>
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

        private void OnChannelListUpdated(object sender, EventArgs e)
        {
            if (SelectedChan != null)
                OnPropertyChanged("SortedUsers");
        }

        private string RelationshipToUser(ICharacter character)
        {
            return character.RelationshipToUser(CharacterManager, null);
        }

        #endregion
    }
}