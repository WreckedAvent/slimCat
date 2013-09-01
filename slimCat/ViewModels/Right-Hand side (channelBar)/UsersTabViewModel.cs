/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Views;
using System.ComponentModel;

namespace ViewModels
{
    /// <summary>
    /// On the channel bar (right-hand side) the 'users' tab, only it shows only the users in the current channel
    /// </summary>
    public class UsersTabViewModel : ChannelbarViewModelCommon
    {
        #region Fields
        private GenderSettingsModel _genderSettings;
        public const string UsersTabView = "UsersTabView";
        private int _listCacheCount = -1;
        private GeneralChannelModel _currentChan;
        #endregion

        #region Properties
        public GenderSettingsModel GenderSettings { get { return _genderSettings; } }
        public GeneralChannelModel SelectedChan { get { return _currentChan ?? _cm.SelectedChannel as GeneralChannelModel; } }

        public IEnumerable<ICharacter> SortedUsers
        {
            get
            {
                if (HasUsers)
                    return SelectedChan.Users
                                .Where(MeetsFilter)
                                .OrderBy(RelationshipToUser)
                                .ThenBy(x => x.Name);
                else
                    return null;
            }
        }

        public string SortContentString
        {
            get
            {
                if (HasUsers)
                    return SelectedChan.Title;
                else return null;
            }
        }
        #endregion

        #region filter functions
        private bool MeetsFilter(ICharacter character)
        {
            return character.MeetsFilters(GenderSettings, SearchSettings, CM, CM.SelectedChannel as GeneralChannelModel);
        }

        private string RelationshipToUser(ICharacter character)
        {
            return character.RelationshipToUser(CM, CM.SelectedChannel as GeneralChannelModel);
        }
        #endregion

        #region Constructors
        public UsersTabViewModel(IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            :base(contain, regman, eventagg, cm)
        {
            _container.RegisterType<object, UsersTabView>(UsersTabView);
            _genderSettings = new GenderSettingsModel();

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

            CM.SelectedChannelChanged += (s, e) =>
                {
                    SelectedChan.PropertyChanged -= OnChannelListUpdated;
                    _currentChan = null;

                    OnPropertyChanged("SortContentString");
                    OnPropertyChanged("SortedUsers");

                    SelectedChan.PropertyChanged += OnChannelListUpdated;
                };

            _events.GetEvent<slimCat.NewUpdateEvent>().Subscribe(
                args =>
                {
                    var thisNotification = args as CharacterUpdateModel;

                    if (thisNotification.Arguments is CharacterUpdateModel.PromoteDemoteEventArgs)
                        OnPropertyChanged("HasPermissions");
                });
        }
        #endregion

        #region methods
        private void OnChannelListUpdated(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Users"))
            {
                OnPropertyChanged("SortedUsers");
            }
        }
        #endregion
    }
}
