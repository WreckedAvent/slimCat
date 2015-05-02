#region Copyright

// <copyright file="PermissionService.cs">
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

    using Models;

    #endregion

    /// <summary>
    ///     This is used to determine the permissions of a given character.
    /// </summary>
    public class PermissionService : IGetPermissions
    {
        private readonly IChatModel cm;

        private readonly ICharacterManager manager;

        public PermissionService(IChatModel cm, ICharacterManager manager)
        {
            this.cm = cm;
            this.manager = manager;
        }

        private GeneralChannelModel CurrentChannel => cm.CurrentChannel as GeneralChannelModel;

        public bool IsModerator(string name)
        {
            if (IsAdmin(name)) return true;

            return CurrentChannel != null
                   && CurrentChannel.CharacterManager.IsOnList(name, ListKind.Moderator, false);
        }

        public bool IsChannelModerator(string name) => CurrentChannel != null
                                                       && CurrentChannel.CharacterManager.IsOnList(name, ListKind.Moderator, false);

        public bool IsAdmin(string name) => manager.IsOnList(name, ListKind.Moderator, false);
    }
}