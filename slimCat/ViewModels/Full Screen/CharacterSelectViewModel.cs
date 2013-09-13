// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterSelectViewModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   This View model only serves to allow a user to select a character.
//   Fires off 'CharacterSelectedLoginEvent' when the user has selected their character.
//   Responds to the 'LoginCompletedEvent'
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using slimCat;

    using Views;

    /// <summary>
    ///     This View model only serves to allow a user to select a character.
    ///     Fires off 'CharacterSelectedLoginEvent' when the user has selected their character.
    ///     Responds to the 'LoginCompletedEvent'
    /// </summary>
    public class CharacterSelectViewModel : ViewModelBase
    {
        #region Constants

        /// <summary>
        ///     The character select view name.
        /// </summary>
        public const string CharacterSelectViewName = "CharacterSelectView";

        #endregion

        #region Fields

        private readonly IAccount _model; // the model to interact with

        private bool _connecting;

        private string _relay = "Next, Select a Character ...";

        // bound message used to relay information to the information
        private string _selchar; // character we have selected

        private RelayCommand _select;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterSelectViewModel"/> class.
        /// </summary>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="acc">
        /// The acc.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        public CharacterSelectViewModel(
            IUnityContainer contain, IRegionManager regman, IEventAggregator events, IAccount acc, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                this._model = acc.ThrowIfNull("acc");

                this._events.GetEvent<LoginCompleteEvent>()
                    .Subscribe(this.handleLoginComplete, ThreadOption.UIThread, true);
            }
            catch (Exception ex)
            {
                ex.Source = "Character Select ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the characters.
        /// </summary>
        public ObservableCollection<string> Characters
        {
            get
            {
                return this._model.Characters;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is connecting.
        /// </summary>
        public bool IsConnecting
        {
            get
            {
                return this._connecting;
            }

            set
            {
                this._connecting = value;
                this.OnPropertyChanged("IsConnecting");
            }
        }

        /// <summary>
        ///     Gets or sets the relay message.
        /// </summary>
        public string RelayMessage
        {
            get
            {
                return this._relay;
            }

            set
            {
                this._relay = value;
                this.OnPropertyChanged("RelayMessage");
            }
        }

        /// <summary>
        ///     Gets the select character command.
        /// </summary>
        public ICommand SelectCharacterCommand
        {
            get
            {
                if (this._select == null)
                {
                    this._select = new RelayCommand(
                        param => this.SendCharacterSelectEvent(), param => this.CanSendCharacterSelectEvent());
                }

                return this._select;
            }
        }

        /// <summary>
        ///     Gets or sets the selected character.
        /// </summary>
        public string SelectedCharacter
        {
            get
            {
                return this._selchar;
            }

            set
            {
                this._selchar = value;
                this.OnPropertyChanged("SelectedCharacter");
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
            try
            {
                this._container.RegisterType<object, CharacterSelectView>(CharacterSelectViewName);
            }
            catch (Exception ex)
            {
                ex.Source = "Character Select ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        private bool CanSendCharacterSelectEvent()
        {
            return !string.IsNullOrWhiteSpace(this.SelectedCharacter);
        }

        private void SendCharacterSelectEvent()
        {
            this.RelayMessage = "Awesome! Connecting to F-Chat ...";
            this.IsConnecting = true;
            this._events.GetEvent<CharacterSelectedLoginEvent>().Publish(this.SelectedCharacter);
        }

        private void handleLoginComplete(bool should)
        {
            if (should)
            {
                this.RelayMessage = "Done! Now pick a character.";
                this._region.RequestNavigate(Shell.MainRegion, new Uri(CharacterSelectViewName, UriKind.Relative));
            }
        }

        #endregion
    }
}