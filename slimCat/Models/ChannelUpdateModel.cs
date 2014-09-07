#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelUpdateModel.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Windows.Documents;
    using Services;
    using Utilities;
    using ViewModels;
    using Views;

    #endregion

    /// <summary>
    ///     Used to represent an update about a channel
    /// </summary>
    public class ChannelUpdateModel : NotificationModel
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelUpdateModel" /> class.
        /// </summary>
        public ChannelUpdateModel(ChannelModel model, ChannelUpdateEventArgs e)
        {
            TargetChannel = model;
            Arguments = e;
            Arguments.Model = this;
        }

        public ChannelUpdateModel()
        {
        }

        #endregion

        #region Public Properties

        public ChannelUpdateEventArgs Arguments { get; private set; }

        public ChannelModel TargetChannel { get; set; }

        public override Block View
        {
            get { return new ChannelUpdateView {DataContext = this}; }
        }

        public override void Navigate(IChatState chatState)
        {
            Arguments.NavigateTo(chatState);
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastManager)
        {
            if (ApplicationSettings.DisallowNotificationsWhenDnd && chatState.ChatModel.CurrentCharacter.Status == StatusType.Busy)
                return;

            Arguments.DisplayNewToast(chatState, toastManager);
        }

        #endregion

        #region Public Methods and Operators

        public override string ToString()
        {
            return Arguments.ToString();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            TargetChannel = null;
            Arguments = null;
        }

        #endregion
    }

    /// <summary>
    ///     Represents arguments which have a channel as their direct object
    /// </summary>
    public abstract class ChannelUpdateEventArgs : EventArgs
    {
        public ChannelUpdateModel Model { get; set; }

        public virtual void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            if (!Model.TargetChannel.Settings.AlertAboutUpdates) return;

            SetToastData(toastsManager.Toast);
            toastsManager.AddNotification(Model);
            toastsManager.ShowToast();
        }

        public virtual void NavigateTo(IChatState chatState)
        {
            chatState.EventAggregator.SendUserCommand("join", new[] {Model.TargetChannel.Id});

            NotificationService.ShowWindow();
        }

        internal virtual void SetToastData(ToastNotificationsViewModel toast)
        {
            toast.Title = Model.TargetChannel.Title;
            toast.Content = ToString();
            toast.Navigator = Model;
        }

        internal string GetChannelBbCode()
        {
            return "[session={1}]{0}[/session]".FormatWith(Model.TargetChannel.Id, Model.TargetChannel.Title);
        }

        internal string WrapInUser(string user)
        {
            return "[user]{0}[/user]".FormatWith(user);
        }
    }
}