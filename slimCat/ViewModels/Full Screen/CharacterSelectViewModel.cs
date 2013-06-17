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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using slimCat;
using Views;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using lib;
using System.Windows.Input;

namespace ViewModels
{
    /// <summary>
    /// This View model only serves to allow a user to select a character. 
    /// Fires off 'CharacterSelectedLoginEvent' when the user has selected their character.
    /// Responds to the 'LoginCompletedEvent'
    /// </summary>
    public class CharacterSelectViewModel : ViewModelBase
    {
        #region Fields
        private IAccount _model; // the model to interact with

        private string _selchar; // character we have selected
        private bool _connecting = false;
        private string _relay = "Next, Select a Character ..."; // bound message used to relay information to the information

        public const string CharacterSelectViewName = "CharacterSelectView";
        #endregion

        #region Properties
        public string RelayMessage
        {
            get { return _relay; }
            set { _relay = value; OnPropertyChanged("RelayMessage"); }
        }

        public ObservableCollection<string> Characters { get { return _model.Characters; } }

        public string SelectedCharacter
        {
            get { return _selchar; }
            set{ _selchar = value; OnPropertyChanged("SelectedCharacter"); }
        }

        public bool IsConnecting
        {
            get { return _connecting; }
            set { _connecting = value; OnPropertyChanged("IsConnecting"); }
        }
        #endregion

        #region Constructors
        public CharacterSelectViewModel(IUnityContainer contain, IRegionManager regman, IEventAggregator events,
                                        IAccount acc, IChatModel cm) 
            : base(contain, regman, events, cm)
        {
            try
            {
                if (acc == null) throw new ArgumentNullException("acc");
                _model = acc;

                this._events.GetEvent<slimCat.LoginCompleteEvent>().Subscribe(handleLoginComplete, ThreadOption.UIThread, true);
            }

            catch (Exception ex)
            {
                ex.Source = "Character Select ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        public override void Initialize()
        {
            try
            {
                _container.RegisterType<object, CharacterSelectView>(CharacterSelectViewName);
            }

            catch (Exception ex)
            {
                ex.Source = "Character Select ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Commands
        RelayCommand _select;
        public ICommand SelectCharacterCommand
        {
            get
            {
                if (_select == null)
                {
                    _select = new RelayCommand(param => this.SendCharacterSelectEvent(),
                        param => this.CanSendCharacterSelectEvent());
                }
                return _select;
            }
        }

        private bool CanSendCharacterSelectEvent()
        {
            return !string.IsNullOrWhiteSpace(SelectedCharacter);
        }

        private void SendCharacterSelectEvent()
        {
            RelayMessage = "Awesome! Connecting to F-Chat ...";
            IsConnecting = true;
            this._events.GetEvent<CharacterSelectedLoginEvent>().Publish(SelectedCharacter);
        }
        #endregion

        #region Methods
        private void handleLoginComplete(bool should)
        {

            if (should)
            {
                RelayMessage = "Done! Now pick a character.";
                _region.RequestNavigate(Shell.MainRegion,
                    new Uri(CharacterSelectViewName, UriKind.Relative));
            }
        }
        #endregion
    }
}
