using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Views;

namespace ViewModels
{
    public class ManageListsViewModel : ChannelbarViewModelCommon
    {
        #region Fields
        public const string ManageListsTabView = "ManageListsTabView";
        IList<ICharacter> _friends;
        IList<ICharacter> _bookmarks;
        IList<ICharacter> _interested;
        IList<ICharacter> _notInterested;
        IList<ICharacter> _ignored;

        IList<ICharacter> _roomMods;
        #endregion

        #region Properties
        public bool ShowMods { get { return _cm.SelectedChannel is GeneralChannelModel; } }

        public IList<ICharacter> Friends { get { _friends = update(_cm.Friends, _friends); return _friends; } }
        public IList<ICharacter> Bookmarks { get { _bookmarks = update(_cm.Bookmarks, _bookmarks); return _bookmarks; } }
        public IList<ICharacter> Interested { get { _interested = update(ApplicationSettings.Interested, _interested); return _interested; } }
        public IList<ICharacter> NotInterested { get { _notInterested = update(ApplicationSettings.NotInterested, _notInterested); return _notInterested; } }
        public IList<ICharacter> Ignored { get { _ignored = update(CM.Ignored, _ignored); return _ignored; } }

        public IList<ICharacter> Moderaters { get { return update(((GeneralChannelModel)CM.SelectedChannel).Moderators, _roomMods); } }
        #endregion

        #region Methods
        private IList<ICharacter> update(IList<string> CharacterNames, IList<ICharacter> CurrentList)
        {
            if (CurrentList == null || CurrentList.Count != CharacterNames.Count)
            {
                CurrentList = new List<ICharacter>();
                foreach (var characterName in CharacterNames)
                    CurrentList.Add(CM.FindCharacter(characterName));
            }

            return CurrentList;
        }
        #endregion

        #region Constructor
        public ManageListsViewModel(IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            _container.RegisterType<object, ManageListsTabView>(ManageListsTabView);
            _events.GetEvent<NewUpdateEvent>().Subscribe(
                args =>
                {
                    var thisUpdate = args as CharacterUpdateModel;
                    if (thisUpdate == null) return;

                    var thisArguments = thisUpdate.Arguments as CharacterUpdateModel.ListChangedEventArgs;
                    if (thisArguments == null) return;

                    switch (thisArguments.ListArgument)
                    {
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.interested:
                            OnPropertyChanged("Interested"); OnPropertyChanged("NotInterested"); break;
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.ignored:
                            OnPropertyChanged("Ignored"); break;
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.notinterested:
                            OnPropertyChanged("NotInterested"); OnPropertyChanged("Interested"); break;
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.bookmarks:
                            OnPropertyChanged("Bookmarks"); break;
                        case CharacterUpdateModel.ListChangedEventArgs.ListType.friends:
                            OnPropertyChanged("Friends"); break;
                    }
                },
                true);
        }
        #endregion
    }
}
