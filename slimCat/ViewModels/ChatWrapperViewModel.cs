using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using slimCat;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Views;

namespace ViewModels
{
    /// <summary>
    /// A specific viewmodel for the chat wrapper itself
    /// </summary>
    public class ChatWrapperViewModel : ViewModelBase
    {
        #region Fields
        public const string ChatWrapperView = "ChatWrapperView";
        #endregion

        #region Constructors
        public ChatWrapperViewModel(IUnityContainer contain, IRegionManager regman, IEventAggregator events,
                                        IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                this._events.GetEvent<slimCat.CharacterSelectedLoginEvent>().Subscribe(handleSelectedCharacter, ThreadOption.UIThread, true);
            }

            catch (Exception ex)
            {
                ex.Source = "Chat Wrapper ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        public override void Initialize()
        {
            try
            {
                _container.RegisterType<object, ChatWrapperView>(ChatWrapperView);
            }
            catch (Exception ex)
            {
                ex.Source = "Chat Wrapper ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        private void handleSelectedCharacter(string chara)
        {
            _region.RequestNavigate(Shell.MainRegion,
                new Uri(ChatWrapperView, UriKind.Relative), navigationCompleted);
        }

        private void navigationCompleted(NavigationResult result)
        {
            if (result.Result == true)
                _events.GetEvent<ChatOnDisplayEvent>().Publish(null);
        }
        #endregion
    }
}
