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
    public class ChannelsTabViewModel : ChannelbarViewModelCommon
    {
        #region Fields
        public const string ChannelsTabView = "ChannelsTabView";

        private int _thresh = 0;
        private bool _showPublic = true;
        private bool _showPrivate = true;
        private bool _sortByName = false;
        #endregion

        #region Properties
        public IEnumerable<GeneralChannelModel> SortedChannels
        {
            get
            {
                Func<GeneralChannelModel, bool> ContainsSearchString = new Func<GeneralChannelModel, bool>
                (channel => channel.ID.ToLower().Contains(SearchSettings.SearchString) 
                    || channel.Title.ToLower().Contains(SearchSettings.SearchString));

                Func<GeneralChannelModel, bool> MeetsThreshold = new Func<GeneralChannelModel, bool>
                (channel => channel.UserCount >= Threshold);

                Func<GeneralChannelModel, bool> MeetsTypeFilter = new Func<GeneralChannelModel,bool>
                (channel => ((channel.Type == ChannelType.pub) && _showPublic) || ((channel.Type == ChannelType.priv) && _showPrivate));

                Func<GeneralChannelModel, bool> MeetsFilter = new Func<GeneralChannelModel, bool>
                (channel => ContainsSearchString(channel) && MeetsTypeFilter(channel) && MeetsThreshold(channel));

                if (SortByName)
                    return CM.AllChannels.Where(MeetsFilter).OrderBy(channel => channel.Title);
                else
                    return CM.AllChannels.Where(MeetsFilter).OrderByDescending(channel => channel.UserCount);
            }
        }

        #region UI Binding for filter
        public int Threshold
        {
            get { return _thresh; }
            set
            {
                if (_thresh != value && value > 0 && value < 1000)
                {
                    _thresh = value;
                    OnPropertyChanged("SortedChannels");
                }
            }
        }

        public bool ShowPublicRooms
        {
            get { return _showPublic; }
            set
            {
                if (_showPublic != value)
                {
                    _showPublic = value;
                    OnPropertyChanged("SortedChannels");
                }
            }
        }

        public bool ShowPrivateRooms
        {
            get { return _showPrivate; }
            set
            {
                if (_showPrivate != value)
                {
                    _showPrivate = value;
                    OnPropertyChanged("SortedChannels");
                }
            }
        }

        public bool SortByName
        {
            get { return _sortByName; }
            set
            {
                if (_sortByName != value)
                {
                    _sortByName = value;
                    OnPropertyChanged("SortedChannels");
                }
            }
        }

        public bool ShowOnlyAlphabet;
        #endregion
        #endregion

        #region Constructors
        public ChannelsTabViewModel(IChatModel cm, IUnityContainer contain, IRegionManager reggman, IEventAggregator events)
            :base(contain, reggman, events, cm)
        {
            _container.RegisterType<object, ChannelTabView>(ChannelsTabView);

            SearchSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("SearchSettings");
                    OnPropertyChanged("SortedChannels");
                };
        }

        public override void Initialize() { }
        #endregion
    }
}
