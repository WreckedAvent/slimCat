#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterSelectViewModel.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//  
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     This View model only serves to allow a user to select a character.
    ///     Fires off 'CharacterSelectedLoginEvent' when the user has selected their character.
    ///     Responds to the 'LoginCompletedEvent'
    /// </summary>
    public class CharacterSelectViewModel : ViewModelBase
    {
        #region Constants

        internal const string CharacterSelectViewName = "CharacterSelectView";

        #endregion

        #region Fields

        private readonly IAccount model;

        private bool isConnecting;

        private string relayMessage = "Next, Select a Character ...";

        private RelayCommand @select;

        private ICharacter selectedCharacter = new CharacterModel();
        private RelayCommand switchAccount;

        #endregion

        #region Constructors and Destructors

        public CharacterSelectViewModel(IChatState chatState)
            : base(chatState)
        {
            try
            {
                model = chatState.Account;

                Events.GetEvent<LoginCompleteEvent>()
                    .Subscribe(HandleLoginComplete, ThreadOption.UIThread, true);

                LoggingSection = "character select vm";
            }
            catch (Exception ex)
            {
                ex.Source = "Character Select ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        public IEnumerable<ICharacter> Characters
        {
            get
            {
                return model.Characters.Select(
                    c =>
                    {
                        var temp = new CharacterModel {Name = c};
                        temp.GetAvatar();
                        return temp;
                    });
            }
        }

        public bool IsConnecting
        {
            get { return isConnecting; }

            set
            {
                isConnecting = value;
                OnPropertyChanged("IsConnecting");
            }
        }

        public string RelayMessage
        {
            get { return relayMessage; }

            set
            {
                relayMessage = value;
                OnPropertyChanged("RelayMessage");
            }
        }

        public ICommand SelectCharacterCommand
        {
            get
            {
                return @select
                       ?? (@select =
                           new RelayCommand(
                               param => SendCharacterSelectEvent(), param => CanSendCharacterSelectEvent()));
            }
        }

        public ICommand SwitchAccountCommand
        {
            get
            {
                return switchAccount ??
                       (switchAccount =
                           new RelayCommand(
                               _ =>
                                   RegionManager.RequestNavigate(Shell.MainRegion,
                                       new Uri(LoginViewModel.LoginViewName, UriKind.Relative))));
            }
        }

        public ICharacter CurrentCharacter
        {
            get { return selectedCharacter; }

            set
            {
                selectedCharacter = value;
                OnPropertyChanged("CurrentCharacter");
            }
        }

        #endregion

        #region Public Methods and Operators

        public override void Initialize()
        {
            try
            {
                Container.RegisterType<object, CharacterSelectView>(CharacterSelectViewName);
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
            return CurrentCharacter != null && !string.IsNullOrWhiteSpace(CurrentCharacter.Name);
        }

        private void SendCharacterSelectEvent()
        {
            IsConnecting = true;
            Events.GetEvent<CharacterSelectedLoginEvent>().Publish(CurrentCharacter.Name);
            Log("Character {0} selected".FormatWith(CurrentCharacter.Name));
        }

        private void HandleLoginComplete(bool should)
        {
            if (!should)
                return;

            RelayMessage = "Done! Now pick a character.";
            RegionManager.RequestNavigate(Shell.MainRegion, new Uri(CharacterSelectViewName, UriKind.Relative));
            OnPropertyChanged("Characters");
            Log("Requesting character select view");
        }

        #endregion
    }
}