#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatWrapperViewModel.cs">
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

namespace Slimcat.ViewModels
{
    #region Usings

    using System;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     A specific viewmodel for the chat wrapper itself
    /// </summary>
    public class ChatWrapperViewModel : ViewModelBase
    {
        #region Constants

        /// <summary>
        ///     The chat wrapper view.
        /// </summary>
        private const string ChatWrapperView = "ChatWrapperView";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChatWrapperViewModel" /> class.
        /// </summary>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="events">
        ///     The events.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        public ChatWrapperViewModel(
            IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager lists)
            : base(contain, regman, events, cm, lists)
        {
            try
            {
                Events.GetEvent<CharacterSelectedLoginEvent>()
                    .Subscribe(HandleCurrentCharacter, ThreadOption.UIThread, true);
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
                Container.RegisterType<object, ChatWrapperView>(ChatWrapperView);
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
            RegionManager.RequestNavigate(
                Shell.MainRegion, new Uri(ChatWrapperView, UriKind.Relative), NavigationCompleted);
        }

        private void NavigationCompleted(NavigationResult result)
        {
            if (result.Result == true)
                Events.GetEvent<ChatOnDisplayEvent>().Publish(null);
        }

        #endregion
    }
}