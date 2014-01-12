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

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Libraries;
    using Models;
    using Utilities;
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
        internal const string CharacterSelectViewName = "CharacterSelectView";

        #endregion

        #region Fields

        private readonly IAccount model;

        private bool isConnecting;

        private string relayMessage = "Next, Select a Character ...";

        private ICharacter selectedCharacter = new CharacterModel();

        private RelayCommand @select;

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
                this.model = acc.ThrowIfNull("acc");

                this.Events.GetEvent<LoginCompleteEvent>()
                    .Subscribe(this.HandleLoginComplete, ThreadOption.UIThread, true);
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
        public IEnumerable<ICharacter> Characters
        {
            get
            {
                return this.model.Characters.Select(
                    c =>
                        {
                            var temp = new CharacterModel { Name = c };
                            temp.GetAvatar();
                            return temp;
                        });
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is connecting.
        /// </summary>
        public bool IsConnecting
        {
            get
            {
                return this.isConnecting;
            }

            set
            {
                this.isConnecting = value;
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
                return this.relayMessage;
            }

            set
            {
                this.relayMessage = value;
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
                return this.@select
                       ?? (this.@select =
                           new RelayCommand(
                               param => this.SendCharacterSelectEvent(), param => this.CanSendCharacterSelectEvent()));
            }
        }

        /// <summary>
        ///     Gets or sets the selected character.
        /// </summary>
        public ICharacter CurrentCharacter
        {
            get
            {
                return this.selectedCharacter;
            }

            set
            {
                this.selectedCharacter = value;
                this.OnPropertyChanged("CurrentCharacter");
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
                this.Container.RegisterType<object, CharacterSelectView>(CharacterSelectViewName);
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
            return !string.IsNullOrWhiteSpace(this.CurrentCharacter.Name);
        }

        private void SendCharacterSelectEvent()
        {
            this.RelayMessage = "Awesome! Connecting to F-Chat ...";
            this.IsConnecting = true;
            this.Events.GetEvent<CharacterSelectedLoginEvent>().Publish(this.CurrentCharacter.Name);
        }

        private void HandleLoginComplete(bool should)
        {
            if (!should)
            {
                return;
            }

            this.RelayMessage = "Done! Now pick a character.";
            this.RegionManager.RequestNavigate(Shell.MainRegion, new Uri(CharacterSelectViewName, UriKind.Relative));
        }

        #endregion
    }
}