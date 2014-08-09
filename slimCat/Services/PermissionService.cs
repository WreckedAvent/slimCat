#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PermissionService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using Models;

    #endregion

    public class PermissionService : IPermissionService
    {
        private readonly IChatModel cm;
        private readonly ICharacterManager manager;

        public PermissionService(IChatModel cm, ICharacterManager manager)
        {
            this.cm = cm;
            this.manager = manager;
        }

        private GeneralChannelModel CurrentChannel
        {
            get { return cm.CurrentChannel as GeneralChannelModel; }
        }

        public bool IsModerator(string name)
        {
            if (IsAdmin(name)) return true;

            return CurrentChannel != null
                   && CurrentChannel.CharacterManager.IsOnList(name, ListKind.Moderator, false);
        }

        public bool IsChannelModerator(string name)
        {
            return CurrentChannel != null
                   && CurrentChannel.CharacterManager.IsOnList(name, ListKind.Moderator, false);
        }

        public bool IsAdmin(string name)
        {
            return manager.IsOnList(name, ListKind.Moderator, false);
        }
    }
}