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
