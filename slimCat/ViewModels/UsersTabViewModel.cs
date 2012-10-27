using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Views;

namespace ViewModels
{
    /// <summary>
    /// On the channel bar (right-hand side) the 'users' tab
    /// </summary>
    public class UsersTabViewModel : ChannelbarViewModelCommon
    {
        #region Fields
        private GenderSettingsModel _genderSettings;
        public const string UsersTabView = "UsersTabView";
        #endregion

        #region Properties
        public GenderSettingsModel GenderSettings { get { return _genderSettings; } }
        public GeneralChannelModel SelectedChan { get { return CM.SelectedChannel as GeneralChannelModel; } }

        public bool HasUsers
        {
            get
            {
                if (CM.SelectedChannel == null) return false;

                return ((CM.SelectedChannel.Type != ChannelType.pm)
                && (CM.SelectedChannel.DisplayNumber > 0));
            }
        }

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
                    return CM.OnlineCharacters
                        .Where(MeetsFilter)
                        .OrderBy(RelationshipToUser)
                        .ThenBy(x => x.Name);
            }
        }

        public string SortContentString
        {
            get
            {
                if (HasUsers)
                    return SelectedChan.Title;
                else return "Global";
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
                    OnPropertyChanged("SortContentString");
                    OnPropertyChanged("SortedUsers");
                };
        }
        #endregion
    }
}
