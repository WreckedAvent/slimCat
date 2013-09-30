// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatWrapperViewModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   A specific viewmodel for the chat wrapper itself
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using System;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Utilities;
    using Slimcat.Views;

    /// <summary>
    ///     A specific viewmodel for the chat wrapper itself
    /// </summary>
    public class ChatWrapperViewModel : ViewModelBase
    {
        #region Constants

        /// <summary>
        ///     The chat wrapper view.
        /// </summary>
        public const string ChatWrapperView = "ChatWrapperView";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatWrapperViewModel"/> class.
        /// </summary>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        public ChatWrapperViewModel(
            IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                this.Events.GetEvent<CharacterSelectedLoginEvent>()
                    .Subscribe(this.HandleCurrentCharacter, ThreadOption.UIThread, true);
            }
            catch (Exception ex)
            {
                ex.Source = "Chat Wrapper ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
            try
            {
                this.Container.RegisterType<object, ChatWrapperView>(ChatWrapperView);
            }
            catch (Exception ex)
            {
                ex.Source = "Chat Wrapper ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        private void HandleCurrentCharacter(string chara)
        {
            this.RegionManager.RequestNavigate(
                Shell.MainRegion, new Uri(ChatWrapperView, UriKind.Relative), this.NavigationCompleted);
        }

        private void NavigationCompleted(NavigationResult result)
        {
            if (result.Result == true)
            {
                this.Events.GetEvent<ChatOnDisplayEvent>().Publish(null);
            }
        }

        #endregion
    }
}