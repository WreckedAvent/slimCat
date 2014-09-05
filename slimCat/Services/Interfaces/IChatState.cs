#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IChatState.cs">
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

namespace slimCat.Services
{
    #region Usings

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;

    #endregion

    public interface IChatState
    {
        IChatConnection ChatConnection { get; set; }

        IUnityContainer Container { get; set; }

        IRegionManager RegionManager { get; set; }

        IEventAggregator EventAggregator { get; set; }

        IChatModel ChatModel { get; set; }

        IAccount Account { get; set; }

        ICharacterManager CharacterManager { get; set; }
    }

    public class ChatState : IChatState
    {
        public ChatState(
            IUnityContainer container,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IChatModel chatModel,
            ICharacterManager characterManager,
            IChatConnection chatConnection,
            IAccount account)
        {
            ChatConnection = chatConnection;
            Container = container;
            RegionManager = regionManager;
            EventAggregator = eventAggregator;
            ChatModel = chatModel;
            CharacterManager = characterManager;
            Account = account;
        }

        public IChatConnection ChatConnection { get; set; }
        public IUnityContainer Container { get; set; }
        public IRegionManager RegionManager { get; set; }
        public IEventAggregator EventAggregator { get; set; }
        public IChatModel ChatModel { get; set; }
        public IAccount Account { get; set; }
        public ICharacterManager CharacterManager { get; set; }
    }
}