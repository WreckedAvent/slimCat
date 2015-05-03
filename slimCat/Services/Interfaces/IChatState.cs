#region Copyright

// <copyright file="IChatState.cs">
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

    /// <summary>
    ///     Represents all useful chat state, such as our account and current channels.
    /// </summary>
    public interface IChatState
    {
        IHandleChatConnection Connection { get; set; }
        IUnityContainer Container { get; set; }
        IRegionManager RegionManager { get; set; }
        IEventAggregator EventAggregator { get; set; }
        IChatModel ChatModel { get; set; }
        IAccount Account { get; set; }
        ICharacterManager CharacterManager { get; set; }
    }
}