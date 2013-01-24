using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Windows.Input;

namespace ViewModels
{
    public abstract class ViewModelBase : SysProp, IModule, IDisposable
    {
        #region Fields
        protected IUnityContainer _container;
        protected IRegionManager _region;
        protected IEventAggregator _events;
        protected IChatModel _cm;
        private RightClickMenuViewModel _rcmvm = new RightClickMenuViewModel();
        #endregion

        #region Shared Constructors
        public ViewModelBase(IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
        {
            try
            {
                if (regman == null) throw new ArgumentNullException("contain");
                _container = contain;

                if (regman == null) throw new ArgumentNullException("regman");
                _region = regman;

                if (events == null) throw new ArgumentNullException("events");
                _events = events;

                if (cm == null) throw new ArgumentNullException("cm");
                _cm = cm;

                _cm.SelectedChannelChanged += OnSelectedChannelChanged;

                _events.GetEvent<NewUpdateEvent>().Subscribe(UpdateRightClickMenu);

            }

            catch (Exception ex)
            {
                ex.Source = "Generic ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        public abstract void Initialize();
        #endregion

        #region Global Commands
        private RelayCommand _link;
        public ICommand NavigateTo
        {
            get
            {
                if (_link == null)
                    _link = new RelayCommand(StartLinkInDefaultBrowser);
                return _link;
            }
        }

        protected void StartLinkInDefaultBrowser(object link)
        {
            string interpret = link as string;
            if (!interpret.Contains(".") || interpret.Contains(" "))
                interpret = "http://www.f-list.net/c/" + HttpUtility.UrlEncode(interpret);

            if (!String.IsNullOrEmpty(interpret))
                Process.Start(interpret);
        }

        #region Join Channel
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

        protected void RequestChannelJoinEvent(object args)
        {
            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand("join", new List<string> { args as string })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
        }

        protected bool CanJoinChannel(object args)
        {
            return !_cm
                .CurrentChannels
                .Any(param => param.ID.Equals((string)args, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Priv
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

        protected void RequestPMEvent(object args)
        {
            string TabName = (string)args;
            if (_cm
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
            return true;
        }
        #endregion

        #region Ignore
        private RelayCommand _ign;
        public ICommand IgnoreCommand
        {
            get
            {
                if (_ign == null)
                    _ign = new RelayCommand(AddIgnoreEvent, CanIgnore);
                return _ign;
            }
        }

        private RelayCommand _uign;
        public ICommand UnignoreCommand
        {
            get
            {
                if (_uign == null)
                    _uign = new RelayCommand(RemoveIgnoreEvent, CanUnIgnore);
                return _uign;
            }
        }

        protected void AddIgnoreEvent(object args) { IgnoreEvent(args); }
        protected void RemoveIgnoreEvent(object args) { IgnoreEvent(args, true); }

        protected bool CanIgnore(object args)
        {
            return (!_cm.Ignored.Contains(args as string, StringComparer.OrdinalIgnoreCase));
        }

        protected bool CanUnIgnore(object args)
        {
            return !CanIgnore(args);
        }

        protected void IgnoreEvent(object args, bool remove = false)
        {
            string name = args as string;

            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand((remove ? "unignore" : "ignore"), new List<string> { name })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
            updateRightClickMenu(_rcmvm.Target); // updates the ignore/unignore text
        }
        #endregion

        #region Interested In / Not
        protected RelayCommand _isInter;
        public ICommand InterestedCommand
        {
            get
            {
                if (_isInter == null)
                    _isInter = new RelayCommand(IsInterestedEvent);
                return _isInter;
            }
        }

        protected RelayCommand _isNInter;
        public ICommand NotInterestedCommand
        {
            get
            {
                if (_isNInter == null)
                    _isNInter = new RelayCommand(IsUninterestedEvent);
                return _isNInter;
            }
        }

        protected void IsInterestedEvent(object args) { InterestedEvent(args); }
        protected void IsUninterestedEvent(object args) { InterestedEvent(args, false); }

        protected void InterestedEvent(object args, bool interestedIn = true)
        {
            if (interestedIn)
                _events.GetEvent<UserCommandEvent>().Publish(
                    CommandDefinitions.CreateCommand("interesting", new [] { args as string})
                        .toDictionary());
            else
                _events.GetEvent<UserCommandEvent>().Publish(
                    CommandDefinitions.CreateCommand("notinteresting", new[] { args as string })
                        .toDictionary());
        }
        #endregion

        #region Kick/Ban
        protected RelayCommand _kick;
        public ICommand KickCommand
        {
            get
            {
                if (_kick == null)
                    _kick = new RelayCommand(KickEvent, param => HasPermissions);
                return _kick;
            }
        }

        protected RelayCommand _ban;
        public ICommand BanCommand
        {
            get
            {
                if (_ban == null)
                    _ban = new RelayCommand(BanEvent, param => HasPermissions);
                return _ban;
            }
        }

        protected void KickEvent(object args) { KickOrBanEvent(args, false); }
        protected void BanEvent(object args) { KickOrBanEvent(args, true); }

        protected void KickOrBanEvent(object args, bool isBan)
        {
            string name = args as string;

            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand((isBan ? "ban" : "kick"), new[] { name }, CM.SelectedChannel.ID)
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
        }
        #endregion

        #region RightClick Menu
        private RelayCommand _openMenu;
        public ICommand OpenRightClickMenuCommand
        {
            get
            {
                if (_openMenu == null)
                    _openMenu = new RelayCommand(
                        args =>
                        {
                            var newTarget = CM.FindCharacter(args as string);
                            updateRightClickMenu(newTarget);
                        });
                return _openMenu;
            }
        }

        internal void updateRightClickMenu(ICharacter NewTarget)
        {
            string name = NewTarget.Name;
            _rcmvm.SetNewTarget(NewTarget,
                                CanIgnore(name),
                                CanUnIgnore(name));
            _rcmvm.IsOpen = true;
            OnPropertyChanged("RightClickMenuViewModel");
        }

        private RelayCommand _invertButton;
        public ICommand InvertButtonCommand
        {
            get
            {
                if (_invertButton == null)
                    _invertButton = new RelayCommand(InvertButton);
                return _invertButton;
            }
        }
        #endregion
        #endregion

        #region Methods
        protected virtual void OnSelectedChannelChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("HasPermissions");
            _rcmvm.IsOpen = false;
        }

        private void UpdateRightClickMenu(NotificationModel argument)
        {
            if (!_rcmvm.IsOpen) return;

            var updateKind = argument as CharacterUpdateModel;
            if (updateKind == null) return;

            if (_rcmvm.Target == null) return;

            if (updateKind.TargetCharacter.Name == _rcmvm.Target.Name)
                updateRightClickMenu(_rcmvm.Target);

            OnPropertyChanged("RightClickMenuViewModel");
        }

        internal virtual void InvertButton(object arguments) { }
        #endregion

        #region Properties
        /// <summary>
        /// CM is the general reference to the ChatModel, which is central to anything which needs to interact with session data
        /// </summary>
        public IChatModel CM
        {
            get { return _cm; }
        }

        /// <summary>
        /// Returns true if the current user has moderator permissions
        /// </summary>
        public bool HasPermissions
        {
            get
            {
                if (_cm.SelectedCharacter == null) return false;

                bool isLocalMod = false;
                if (_cm.SelectedChannel is GeneralChannelModel)
                    isLocalMod = (_cm.SelectedChannel as GeneralChannelModel).Moderators.Contains(_cm.SelectedCharacter.Name);
                return isLocalMod || _cm.Mods.Contains(_cm.SelectedCharacter.Name);
            }
        }

        public RightClickMenuViewModel RightClickMenuViewModel { get { return _rcmvm; } }
        #endregion

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _cm.SelectedChannelChanged -= OnSelectedChannelChanged;
                _events.GetEvent<NewUpdateEvent>().Unsubscribe(UpdateRightClickMenu);
                _container = null;
                _region = null;
                _cm = null;
                _events = null;
                _rcmvm.Dispose();
                _rcmvm = null;
            }
        }
    }
}
