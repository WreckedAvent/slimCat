#region Copyright

// <copyright file="ChatState.cs">
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

namespace slimCat.Services
{
    #region Usings

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;

    #endregion

    public class ChatState : IChatState
    {
        public ChatState(
            IUnityContainer container,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IChatModel chatModel,
            ICharacterManager characterManager,
            IHandleChatConnection connection,
            IAccount account)
        {
            Connection = connection;
            Container = container;
            RegionManager = regionManager;
            EventAggregator = eventAggregator;
            ChatModel = chatModel;
            CharacterManager = characterManager;
            Account = account;
        }

        public IHandleChatConnection Connection { get; set; }
        public IUnityContainer Container { get; set; }
        public IRegionManager RegionManager { get; set; }
        public IEventAggregator EventAggregator { get; set; }
        public IChatModel ChatModel { get; set; }
        public IAccount Account { get; set; }
        public ICharacterManager CharacterManager { get; set; }
    }
}