#region Copyright

// <copyright file="PermissionToTextConverter.cs">
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

    using Models;
    using Utilities;

    #endregion

    public class PermissionToTextConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            var level = (CommandModel.PermissionLevel) value;

            if (level == CommandModel.PermissionLevel.User) return "User commands";
            if (level == CommandModel.PermissionLevel.Moderator) return "Moderator commands";
            if (level == CommandModel.PermissionLevel.GlobalMod) return "Global moderator commands";
            if (level == CommandModel.PermissionLevel.Admin) return "Admin commands";
            return string.Empty;
        }
    }
}