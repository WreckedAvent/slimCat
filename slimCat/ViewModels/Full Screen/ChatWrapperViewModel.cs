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
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
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
