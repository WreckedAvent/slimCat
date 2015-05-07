#region Copyright

// <copyright file="CharacterUpdateModel.cs">
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
    ///     Used to represent an update about a character
    /// </summary>
    public partial class CharacterUpdateModel : NotificationModel
    {
        #region Public Methods and Operators

        public override string ToString()
        {
            return TargetCharacter.Name + " " + Arguments;
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            TargetCharacter = null;
            Arguments = null;
        }

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterUpdateModel" /> class.
        /// </summary>
        public CharacterUpdateModel(ICharacter target, CharacterUpdateEventArgs e)
        {
            TargetCharacter = target;
            Arguments = e;
            Arguments.Model = this;
        }

        public CharacterUpdateModel()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the arguments.
        /// </summary>
        public CharacterUpdateEventArgs Arguments { get; private set; }

        /// <summary>
        ///     Gets the target character.
        /// </summary>
        public ICharacter TargetCharacter { get; private set; }

        public override Block View => new CharacterUpdateView {DataContext = this};

        public override void Navigate(IChatState chatState)
        {
            Arguments.NavigateTo(chatState);
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            Arguments.DisplayNewToast(chatState, toastsManager);
        }

        #endregion
    }

    /// <summary>
    ///     Represents updates which have a character as their direct object
    /// </summary>
    public abstract class CharacterUpdateEventArgs : EventArgs
    {
        public CharacterUpdateModel Model { get; set; }
        public abstract void DisplayNewToast(IChatState chatState, IManageToasts toastsManager);

        public virtual void NavigateTo(IChatState chatState)
        {
            chatState.EventAggregator.SendUserCommand("priv", new[] {Model.TargetCharacter.Name});

            NotificationService.ShowWindow();
        }

        internal virtual void SetToastData(ToastNotificationsViewModel toast)
        {
            var name = ApplicationSettings.ShowNamesInToasts ? Model.TargetCharacter.Name : "A user";
            toast.Title = name;
            toast.Content = (ApplicationSettings.ShowAvatarsInToasts ? "" : name + " ") + ToString();
            toast.TargetCharacter = Model.TargetCharacter;
            Model.TargetCharacter.GetAvatar();
            toast.Navigator = Model;
        }

        internal void DoNormalToast(IManageToasts toastManager)
        {
            SetToastData(toastManager.Toast);
            toastManager.AddNotification(Model);
            toastManager.ShowToast();
        }

        internal void DoLoudToast(IManageToasts toastManager)
        {
            DoNormalToast(toastManager);
            toastManager.PlaySound();
            toastManager.FlashWindow();
        }
    }

    public abstract class CharacterUpdateInChannelEventArgs : CharacterUpdateEventArgs
    {
        public string TargetChannel { get; set; }
        public string TargetChannelId { get; set; }

        internal override void SetToastData(ToastNotificationsViewModel toast)
        {
            base.SetToastData(toast);
            toast.Title = $"{Model.TargetCharacter.Name} #{TargetChannel}";
        }

        public override void NavigateTo(IChatState chatState)
        {
            chatState.EventAggregator.GetEvent<RequestChangeTabEvent>().Publish(TargetChannelId);

            NotificationService.ShowWindow();
        }

        internal void DoToast(ChannelSettingPair setting, IManageToasts toastManager, IChatState chatState)
        {
            if (setting.OnlyForInteresting && !chatState.IsInteresting(Model.TargetCharacter.Name)) return;

            toastManager.NotifyWithSettings(Model, setting.NotifyLevel);

            if (setting.NotifyLevel >= ChannelSettingsModel.NotifyLevel.NotificationAndToast)
                SetToastData(toastManager.Toast);
        }
    }
}