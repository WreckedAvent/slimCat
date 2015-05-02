#region Copyright

// <copyright file="ChannelViewModelBase.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
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

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Services;
    using Utilities;

    #endregion

    /// <summary>
    ///     This holds most of the logic for channel view models. Changing behaviors between channels should be done by
    ///     overriding methods.
    /// </summary>
    public abstract class ChannelViewModelBase : ViewModelBase
    {
        #region Constructors and Destructors

        protected ChannelViewModelBase(IChatState chatState)
            : base(chatState)
        {
            Events.GetEvent<ErrorEvent>().Subscribe(UpdateError);

            PropertyChanged += OnThisPropertyChanged;

            if (errorRemoveTimer != null)
                return;

            errorRemoveTimer = new Timer(5000);
            errorRemoveTimer.Elapsed += (s, e) => Error = null;

            errorRemoveTimer.AutoReset = false;

            saveMessageTimer = new Timer(10000) {AutoReset = false};

            entryBoxRowHeight = new GridLength(1, GridUnitType.Auto);
            headerRowHeight = new GridLength(1, GridUnitType.Auto);


            Events.GetEvent<ConnectionClosedEvent>().Subscribe(OnDisconnect, ThreadOption.PublisherThread, true);
        }

        #endregion

        public string TabComplete(string character)
        {
            if (string.IsNullOrWhiteSpace(Message)) return null;

            var lastSpaceIdx = string.IsNullOrWhiteSpace(character) ? Message.LastIndexOf(' ') : tabCompleteIdx;
            var lastWord = Message.Substring(lastSpaceIdx == -1 ? 0 : lastSpaceIdx + 1);

            if (string.IsNullOrWhiteSpace(lastWord)) return null;

            IEnumerable<ICharacter> characters = CharacterManager.Characters.OrderBy(x => x.Name).ThenBy(x => x.Name);

            if (!string.IsNullOrWhiteSpace(character))
                characters = characters
                    .SkipWhile(x => !string.Equals(x.Name, character, StringComparison.CurrentCultureIgnoreCase))
                    .Skip(1);
            else
            {
                tabCompleteStartString = lastWord;
                tabCompleteIdx = lastSpaceIdx;
            }

            var match = characters.FirstOrDefault(x => x.Name.StartsWith(tabCompleteStartString, true, null));

            if (match == null) return null;

            Message = Message.Replace(lastWord, match.Name);

            return match.Name;
        }

        #region Static Fields

        private static string error;

        private static Timer errorRemoveTimer;

        private static Timer saveMessageTimer;

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

        private int tabCompleteIdx;

        private string tabCompleteStartString;

        #endregion

        #region Public Properties

        public ChannelSettingsModel ChannelSettings => model.Settings;

        public GridLength HeaderRowHeight
        {
            get { return headerRowHeight; }
            set
            {
                headerRowHeight = value;
                OnPropertyChanged();
            }
        }

        public ICommand ClearErrorCommand => clear ?? (clear = new RelayCommand(_ => Error = null));

        public ICommand ClearLogCommand => clearLog
                                           ?? (clearLog = new RelayCommand(args => Events.GetEvent<UserCommandEvent>()
                                               .Publish(
                                                   CommandDefinitions.CreateCommand("clear", null, Model.Id)
                                                       .ToDictionary())));

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
                OnPropertyChanged();
            }
        }

        public string Error
        {
            get { return error; }

            set
            {
                error = value;
                OnPropertyChanged();
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
                OnPropertyChanged();

                if (string.IsNullOrEmpty(message))
                    ChannelSettings.LastMessage = null;
                else if (saveMessageTimer.Enabled == false
                         && message.Length > 30)
                {
                    ChannelSettings.LastMessage = message;
                    saveMessageTimer.Start();
                }
            }
        }

        public string LastMessage { get; set; }

        public bool ShowPreview
        {
            get { return showPreview; }
            set
            {
                showPreview = value;
                OnPropertyChanged();
            }
        }

        public ChannelModel Model
        {
            get { return model; }

            set
            {
                model = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateDownCommand => navDown
                                               ??
                                               (navDown = new RelayCommand(_ => RequestNavigateDirectionalEvent(false)))
            ;

        public ICommand NavigateUpCommand
            => navUp ?? (navUp = new RelayCommand(_ => RequestNavigateDirectionalEvent(true)));

        public ICommand OpenLogCommand => openLog ?? (openLog = new RelayCommand(OnOpenLogEvent));

        public ICommand OpenLogFolderCommand
            => openLogFolder ?? (openLogFolder = new RelayCommand(OnOpenLogFolderEvent));

        public ICommand SendMessageCommand => sendText ?? (sendText = new RelayCommand(_ => ParseAndSend()));

        public virtual string EntryTextBoxIcon => "pack://application:,,,/icons/send_chat.png";

        public virtual string EntryTextBoxLabel => "Chat here ...";

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

        private void OnDisconnect(bool isIntentional)
        {
            if (isIntentional) return;
            ChannelSettings.LastMessage = Message;
        }

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                PropertyChanged -= OnThisPropertyChanged;
                Model.PropertyChanged -= OnModelPropertyChanged;
                Events.GetEvent<ConnectionClosedEvent>().Unsubscribe(OnDisconnect);
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

            if (CommandParser.HasNonCommand(Message))
            {
                if (Message.StartsWith("/my "))
                    Message = "/me 's " + Message.Substring("/my ".Length);
                else if (Message.StartsWith("/me's "))
                    Message = "/me 's " + Message.Substring("/me's ".Length);

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