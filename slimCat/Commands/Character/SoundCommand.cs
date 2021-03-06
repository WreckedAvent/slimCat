﻿#region Copyright

// <copyright file="SoundCommand.cs">
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

    using System.Collections.Generic;
    using Models;

    #endregion

    public partial class UserCommandService
    {
        private void OnSoundOnRequested(IDictionary<string, object> command)
        {
            if (ApplicationSettings.AllowSound) return;

            iconService.ToggleSound();
            SettingsService.SaveApplicationSettingsToXml(cm.CurrentCharacter.Name);
        }

        private void OnSoundOffRequested(IDictionary<string, object> command)
        {
            if (!ApplicationSettings.AllowSound) return;
            iconService.ToggleSound();
            SettingsService.SaveApplicationSettingsToXml(cm.CurrentCharacter.Name);
        }
    }
}