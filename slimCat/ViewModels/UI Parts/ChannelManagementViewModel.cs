#region Copyright

// <copyright file="ChannelManagementViewModel.cs">
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
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Utilities;

    #endregion

    /// <summary>
    ///     The channel management view model.
    /// </summary>
    public class ChannelManagementViewModel : SysProp, IDisposable
    {
        #region Constructors and Destructors

        public ChannelManagementViewModel(IEventAggregator eventagg, GeneralChannelModel model)
        {
            this.model = model;
            description = this.model.Description;
            events = eventagg;
            this.model.PropertyChanged += UpdateDescription;

            modeTypes = new[] {"Ads", "chat", "both"};
        }

        #endregion

        #region Explicit Interface Methods

        void IDisposable.Dispose() => Dispose(true);

        #endregion

        #region Fields

        /// <summary>
        ///     The mode types.
        /// </summary>
        private readonly string[] modeTypes;

        private string description;

        private IEventAggregator events;

        private bool isOpen;

        private GeneralChannelModel model;

        private RelayCommand open;

        private RelayCommand toggleType;

        #endregion

        #region Public Properties

        public bool IsManaging
        {
            get { return isOpen; }

            set
            {
                if (isOpen == value) return;

                Logging.LogLine((value ? "Opening" : "Closing") + " modal", "channel manage vm");

                isOpen = value;
                OnPropertyChanged();

                if (!value && Description != model.Description)
                    UpdateDescription();
            }
        }

        public string Description
        {
            get { return description; }

            set
            {
                description = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<string> ModeTypes => modeTypes;

        public ICommand OpenChannelManagementCommand
            => open ?? (open = new RelayCommand(args => IsManaging = !IsManaging));

        public string RoomModeString
        {
            get
            {
                if (model.Mode == ChannelMode.Ads)
                    return "allows only Ads.";

                return model.Mode == ChannelMode.Chat ? "allows only chatting." : "allows Ads and chatting.";
            }
        }

        public ChannelMode RoomModeType
        {
            get { return model.Mode; }

            set
            {
                if (model.Mode == value)
                    return;

                model.Mode = value;
                OnRoomModeChanged(null);
            }
        }

        public string RoomTypeString
        {
            get
            {
                if (model.Type == ChannelType.InviteOnly)
                    return "Closed Private Channel";

                return model.Type == ChannelType.Private ? "Open Private Channel" : "Public Channel";
            }
        }

        public string ToggleRoomToolTip
        {
            get
            {
                if (model.Type == ChannelType.InviteOnly)
                {
                    return
                        "The room is currently closed and requires an invite to join. Click to declare the room open, which will allow anyone to join it.";
                }

                return model.Type == ChannelType.Private
                    ? "The room is currently open and does not require an invite to join. Click to declare the room closed, which will only allow those with an invite to join it."
                    : "The room is currently a public room and cannot be closed.";
            }
        }

        public ICommand ToggleRoomTypeCommand => toggleType
                                                 ?? (toggleType = new RelayCommand(OnToggleRoomType, CanToggleRoomType))
            ;

        public string ToggleRoomTypeString
        {
            get
            {
                if (model.Type == ChannelType.InviteOnly)
                    return "Open this channel";

                return model.Type == ChannelType.Private ? "Close this channel" : "Cannot close channel";
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
            {
                model.PropertyChanged -= UpdateDescription;
                events = null;
                model = null;
            }

            base.Dispose(isManaged);
        }

        private bool CanToggleRoomType(object args) => model.Type != ChannelType.Public;

        private void OnRoomModeChanged(object args)
        {
            events.GetEvent<UserCommandEvent>()
                .Publish(
                    CommandDefinitions.CreateCommand("setmode", new[] {model.Mode.ToString()}, model.Id).ToDictionary());
        }

        private void OnToggleRoomType(object args)
        {
            events.GetEvent<UserCommandEvent>()
                .Publish(model.Type == ChannelType.InviteOnly
                    ? CommandDefinitions.CreateCommand("openroom", new List<string>(), model.Id).ToDictionary()
                    : CommandDefinitions.CreateCommand("closeroom", null, model.Id).ToDictionary());
        }

        private void UpdateDescription(object sender = null, PropertyChangedEventArgs e = null)
        {
            if (e != null)
            {
                // if its our property changed sending this
                switch (e.PropertyName)
                {
                    case "Description":
                        description = model.Description;
                        OnPropertyChanged("Description");
                        break;
                    case "Type":
                        OnPropertyChanged("ToggleRoomTip");
                        OnPropertyChanged("ToggleRoomTypeString");
                        OnPropertyChanged("RoomTypeString");
                        break;
                    case "Mode":
                        OnPropertyChanged("RoomModeString");
                        break;
                }
            }
            else
            {
                // if its us updating it
                events.GetEvent<UserCommandEvent>()
                    .Publish(
                        CommandDefinitions.CreateCommand("setdescription", new[] {description}, model.Id).ToDictionary());
            }
        }

        #endregion
    }
}