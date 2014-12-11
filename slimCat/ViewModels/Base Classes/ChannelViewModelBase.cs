#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelViewModelBase.cs">
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
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;
    using Libraries;
    using Models;
    using Services;
    using Utilities;
    using System.Windows;

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

        private GridLength headerRowHeight;

        private RelayCommand clear;

        private RelayCommand clearLog;

        private GridLength entryBoxRowHeight;

        private RelayCommand linebreak;

        private string message = string.Empty;

        private ChannelModel model;

        private RelayCommand navDown;

        private RelayCommand navUp;

        private RelayCommand openLog;

        private RelayCommand openLogFolder;

        private RelayCommand sendText;

        private RelayCommand togglePreview;

        private bool showPreview;

        #endregion

        #region Constructors and Destructors

        protected ChannelViewModelBase(IChatState chatState)
            : base(chatState)
        {
            Events.GetEvent<ErrorEvent>().Subscribe(UpdateError);

            PropertyChanged += OnThisPropertyChanged;

            if (errorRemoveTimer != null)
                return;

            errorRemoveTimer = new Timer(5000);
            errorRemoveTimer.Elapsed += (s, e) => { Error = null; };

            errorRemoveTimer.AutoReset = false;

            entryBoxRowHeight = new GridLength(1, GridUnitType.Auto);
            headerRowHeight = new GridLength(1, GridUnitType.Auto);
        }

        #endregion

        #region Public Properties

        public ChannelSettingsModel ChannelSettings
        {
            get { return model.Settings; }
        }

        public GridLength HeaderRowHeight
        {
            get { return headerRowHeight; }
            set
            {
                headerRowHeight = value;
                OnPropertyChanged("HeaderRowHeight");
            }
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
                                       .Publish(CommandDefinitions.CreateCommand("clear", null, Model.Id).ToDictionary())));
            }
        }

        public ICommand TogglePreviewCommand
        {
            get { return togglePreview ?? (togglePreview = new RelayCommand(_ => ShowPreview = !ShowPreview)); }
        }

        public GridLength EntryBoxRowHeight
        {
            get { return entryBoxRowHeight; }
            set
            {
                entryBoxRowHeight = value;
                OnPropertyChanged("EntryBoxRowHeight");
            }
        }

        public string Error
        {
            get { return error; }

            set
            {
                error = value;
                OnPropertyChanged("Error");
            }
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

        public string LastMessage { get; set; }

        public bool ShowPreview
        {
            get { return showPreview; }
            set
            {
                showPreview = value;
                OnPropertyChanged("ShowPreview");
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
                       ?? (navDown = new RelayCommand(_ => RequestNavigateDirectionalEvent(false)));
            }
        }

        public ICommand NavigateUpCommand
        {
            get { return navUp ?? (navUp = new RelayCommand(_ => RequestNavigateDirectionalEvent(true))); }
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
            get { return sendText ?? (sendText = new RelayCommand(_ => ParseAndSend())); }
        }

        public virtual string EntryTextBoxIcon
        {
            get { return "pack://application:,,,/icons/send_chat.png"; }
        }

        public virtual string EntryTextBoxLabel
        {
            get { return "Chat here ..."; }
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
            Events.SendUserCommand(isFolder ? "openlogfolder" : "openlog", null, Model.Id);
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

            LastMessage = Message;
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
                    NavigateStub(getTop, false);
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

            Message = Message.Trim();

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
                else
                    SendCommand(messageToCommand.ToDictionary());
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