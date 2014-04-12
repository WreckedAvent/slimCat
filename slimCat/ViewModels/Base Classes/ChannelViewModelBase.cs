#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelViewModelBase.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Utilities;

    #endregion

    /// <summary>
    ///     This holds most of the logic for channel view models. Changing behaviors between channels should be done by
    ///     overriding methods.
    /// </summary>
    public abstract class ChannelViewModelBase : ViewModelBase
    {
        #region Static Fields

        private static string error;

        private static Timer errorRemoveTimer;

        #endregion

        #region Fields

        private RelayCommand clear;

        private RelayCommand clearLog;

        private RelayCommand linebreak;

        private string message = string.Empty;

        private ChannelModel model;

        private RelayCommand navDown;

        private RelayCommand navUp;

        private RelayCommand openLog;

        private RelayCommand openLogFolder;

        private RelayCommand sendText;

        #endregion

        #region Constructors and Destructors

        protected ChannelViewModelBase(
            IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager manager)
            : base(contain, regman, events, cm, manager)
        {
            Events.GetEvent<ErrorEvent>().Subscribe(UpdateError);

            PropertyChanged += OnThisPropertyChanged;

            if (errorRemoveTimer != null)
                return;

            errorRemoveTimer = new Timer(5000);
            errorRemoveTimer.Elapsed += (s, e) => { Error = null; };

            errorRemoveTimer.AutoReset = false;
        }

        #endregion

        #region Public Properties

        public ChannelSettingsModel ChannelSettings
        {
            get { return model.Settings; }
        }

        public ICommand ClearErrorCommand
        {
            get { return clear ?? (clear = new RelayCommand(delegate { Error = null; })); }
        }

        public ICommand ClearLogCommand
        {
            get
            {
                return clearLog
                       ?? (clearLog =
                           new RelayCommand(
                               args =>
                                   Events.GetEvent<UserCommandEvent>()
                                       .Publish(CommandDefinitions.CreateCommand("clear").ToDictionary())));
            }
        }

        public string Error
        {
            get { return error; }

            set
            {
                error = value;
                OnPropertyChanged("Error");
                OnPropertyChanged("HasError");
            }
        }

        public bool HasError
        {
            get { return !string.IsNullOrWhiteSpace(Error); }
        }

        public ICommand InsertLineBreakCommand
        {
            get { return linebreak ?? (linebreak = new RelayCommand(args => Message = Message + '\n')); }
        }

        public string Message
        {
            get { return message; }

            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }

        public ChannelModel Model
        {
            get { return model; }

            set
            {
                model = value;
                OnPropertyChanged("Model");
            }
        }

        public ICommand NavigateDownCommand
        {
            get
            {
                return navDown
                       ?? (navDown = new RelayCommand(args => RequestNavigateDirectionalEvent(false)));
            }
        }

        public ICommand NavigateUpCommand
        {
            get { return navUp ?? (navUp = new RelayCommand(args => RequestNavigateDirectionalEvent(true))); }
        }

        public ICommand OpenLogCommand
        {
            get { return openLog ?? (openLog = new RelayCommand(OnOpenLogEvent)); }
        }

        public ICommand OpenLogFolderCommand
        {
            get { return openLogFolder ?? (openLogFolder = new RelayCommand(OnOpenLogFolderEvent)); }
        }

        public ICommand SendMessageCommand
        {
            get { return sendText ?? (sendText = new RelayCommand(param => ParseAndSend())); }
        }

        #endregion

        #region Public Methods and Operators

        public override void Initialize()
        {
        }

        public void OnOpenLogEvent(object args)
        {
            OpenLogEvent(args, false);
        }

        public void OnOpenLogFolderEvent(object args)
        {
            OpenLogEvent(args, true);
        }

        public void OpenLogEvent(object args, bool isFolder)
        {
            Events.SendUserCommand(isFolder ? "openlogfolder" : "openlog");
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                PropertyChanged -= OnThisPropertyChanged;
                Model.PropertyChanged -= OnModelPropertyChanged;
                model = null;
            }

            base.Dispose(isManaged);
        }

        protected virtual void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        protected virtual void OnThisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        protected void SendCommand(IDictionary<string, object> command)
        {
            Error = null;

            Message = null;
            Events.GetEvent<UserCommandEvent>().Publish(command);
        }


        protected abstract void SendMessage();

        protected void UpdateError(string newError)
        {
            if (errorRemoveTimer == null) return;

            errorRemoveTimer.Stop();

            Error = newError;
            errorRemoveTimer.Start();
        }

        private void NavigateStub(bool getTop, bool fromPms)
        {
            if (fromPms)
            {
                var collection = ChatModel.CurrentPms;
                if (!collection.Any())
                {
                    NavigateStub(false, false);
                    return;
                }

                var target = (getTop ? collection.First() : collection.Last()).Id;
                RequestPmEvent(target);
            }
            else
            {
                var collection = ChatModel.CurrentChannels;
                var target = (getTop ? collection.First() : collection.Last()).Id;
                RequestChannelJoinEvent(target);
            }
        }

        private void ParseAndSend()
        {
            if (Message == null)
                return;

            if (CommandParser.HasNonCommand(Message))
            {
                SendMessage();
                return;
            }

            try
            {
                var messageToCommand = new CommandParser(Message, model.Id);

                if (!messageToCommand.HasCommand)
                    SendMessage();
                else if ((messageToCommand.RequiresMod && !HasPermissions)
                         || (messageToCommand.Type.Equals("warn") && !HasPermissions))
                {
                    UpdateError(
                        string.Format("I'm sorry Dave, I can't let you do the {0} command.", messageToCommand.Type));
                }
                else if (messageToCommand.IsValid)
                    SendCommand(messageToCommand.ToDictionary());
                else
                    UpdateError(string.Format("I don't know the {0} command.", messageToCommand.Type));
            }
            catch (ArgumentException ex)
            {
                UpdateError(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                UpdateError(ex.Message);
            }
        }

        private void RequestNavigateDirectionalEvent(bool isUp)
        {
            if (ChatModel.CurrentChannel is PmChannelModel)
            {
                var index = ChatModel.CurrentPms.IndexOf(ChatModel.CurrentChannel as PmChannelModel);
                if (index == 0 && isUp)
                {
                    NavigateStub(false, false);
                    return;
                }

                if (index + 1 == ChatModel.CurrentPms.Count() && !isUp)
                {
                    NavigateStub(true, false);
                    return;
                }

                index += isUp ? -1 : 1;
                RequestPmEvent(ChatModel.CurrentPms[index].Id);
            }
            else
            {
                var index = ChatModel.CurrentChannels.IndexOf(ChatModel.CurrentChannel as GeneralChannelModel);
                if (index == 0 && isUp)
                {
                    NavigateStub(false, true);
                    return;
                }

                if (index + 1 == ChatModel.CurrentChannels.Count() && !isUp)
                {
                    NavigateStub(true, true);
                    return;
                }

                index += isUp ? -1 : 1;
                RequestChannelJoinEvent(ChatModel.CurrentChannels[index].Id);
            }
        }

        #endregion
    }
}