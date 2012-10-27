using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;

namespace ViewModels
{
    /// <summary>
    /// Contains some things the channelbar viewmodels have in common
    /// </summary>
    public class ChannelbarViewModelCommon : ViewModelBase
    {
        #region Fields
        private IChatModel _cm;
        private GenericSearchSettingsModel _searchSettings = new GenericSearchSettingsModel();

        public IChatModel Model { get { return _cm; } }
        public GenericSearchSettingsModel SearchSettings { get { return _searchSettings; } }
        #endregion

        #region Constructors
        public ChannelbarViewModelCommon(IChatModel mod, IUnityContainer contain, IRegionManager regman,
                                          IEventAggregator events)
            : base(contain, regman, events)
        {
            _cm = mod;
        }

        public override void Initialize() { }
        #endregion

        #region Commands
        private RelayCommand _join;
        public ICommand JoinChannelCommand
        {
            get
            {
                if (_join == null)
                    _join = new RelayCommand(RequestChannelJoinEvent, CanJoinChannel);
                return _join;
            }
        }

        private RelayCommand _priv;
        public ICommand RequestPMCommand
        {
            get
            {
                if (_priv == null)
                    _priv = new RelayCommand(RequestPMEvent, CanRequestPM);
                return _priv;
            }
        }

        protected void RequestChannelJoinEvent(object args)
        {
            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand("join", new List<string> { args as string })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
        }

        protected bool CanJoinChannel(object args)
        {
            return !Model
                .CurrentChannels
                .Any(param => param.ID.Equals((string)args, StringComparison.OrdinalIgnoreCase));
        }

        protected void RequestPMEvent(object args)
        {
            string TabName = (string) args;
            if (Model
                .CurrentPMs
                .Any(param => param.ID.Equals(TabName, StringComparison.OrdinalIgnoreCase)))
            {
                _events.GetEvent<RequestChangeTabEvent>().Publish(TabName);
                return;
            }

            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand("priv", new List<string>() { TabName })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
        }

        protected bool CanRequestPM(object args)
        {
            return (Model.SelectedCharacter.Name != args as string);
        }

        private RelayCommand _ign;
        public ICommand IgnoreCommand
        {
            get
            {
                if (_ign == null)
                    _ign = new RelayCommand(AddIgnoreEvent,
                        args =>
                        {
                            return !Model.Ignored.Contains(args as string)
                                && Model.SelectedCharacter.Name != args as string;
                        });
                return _ign;
            }
        }

        private RelayCommand _uign;
        public ICommand UnignoreCommand
        {
            get
            {
                if (_uign == null)
                    _uign = new RelayCommand(RemoveIgnoreEvent, args => { return Model.Ignored.Contains(args as string); });
                return _uign;
            }
        }

        private void AddIgnoreEvent(object args) { IgnoreEvent(args); }
        private void RemoveIgnoreEvent(object args) { IgnoreEvent(args, true); }

        private void IgnoreEvent(object args, bool remove = false)
        {
            string name = args as string;

            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand((remove ? "unignore" : "ignore"), new List<string> { name })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
        }
        #endregion
    }
}
