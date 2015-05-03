#region Copyright

// <copyright file="NotifyLevelConverter.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using Models;

    #endregion

    /// <summary>
    ///     Converts notification notify level into descriptive strings.
    /// </summary>
    public sealed class NotifyLevelConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            var toParse = (int) value;
            var notificationType = parameter as string;
            var verboseNotificationKind = "• A notification";

            if (notificationType != null && notificationType.Equals("flash"))
                verboseNotificationKind = "• A tab flash";

            var parsed = (ChannelSettingsModel.NotifyLevel) toParse;
            if (parsed >= ChannelSettingsModel.NotifyLevel.NotificationAndToast &&
                ApplicationSettings.ShowNotificationsGlobal)
                verboseNotificationKind += "\n• A toast";

            if (parsed >= ChannelSettingsModel.NotifyLevel.NotificationAndSound)
            {
                if (ApplicationSettings.AllowSound)
                {
                    if (ApplicationSettings.ShowNotificationsGlobal)
                        verboseNotificationKind += " with sound";
                    else
                        verboseNotificationKind += "\n• An audible alert";
                }

                verboseNotificationKind += "\n• 5 Window Flashes";
            }

            if (parsed == ChannelSettingsModel.NotifyLevel.NoNotification)
                return "Nothing!";

            return verboseNotificationKind;
        }
    }
}