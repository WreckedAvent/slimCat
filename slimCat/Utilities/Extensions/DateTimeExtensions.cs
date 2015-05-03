#region Copyright

// <copyright file="DateTimeExtensions.cs">
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

    using System;
    using System.Text;
    using Models;

    #endregion

    public static class DateTimeExtensions
    {
        /// <summary>
        ///     Converts a <see cref="System.DateTimeOffset" /> to a rough time in the future.
        /// </summary>
        /// <returns>A string in the "hours h minutes m seconds s" format.</returns>
        public static string DateTimeInFutureToRough(this DateTimeOffset futureTime)
        {
            var temp = new StringBuilder();
            var rough = futureTime - DateTimeOffset.Now;

            if (rough.Days > 0)
                temp.Append(rough.Days + "d ");

            if (rough.Hours > 0)
                temp.Append(rough.Hours + "h ");

            if (rough.Minutes > 0)
                temp.Append(rough.Minutes + "m ");

            if (rough.Seconds > 0)
                temp.Append(rough.Seconds + "s ");

            if (temp.Length < 2)
                temp.Append("0s ");

            return temp.ToString();
        }

        /// <summary>
        ///     Converts a <see cref="System.DateTimeOffset" /> to a rough time in the past.
        /// </summary>
        /// <returns>A string in the "hours h minutes m seconds s ago" format.</returns>
        public static string DateTimeToRough(this DateTimeOffset original, bool returnSeconds = false,
            bool appendAgo = true)
        {
            var temp = new StringBuilder();
            var rough = DateTimeOffset.Now - original;
            var tolerance = returnSeconds ? 1 : 60;

            if (rough.TotalSeconds < tolerance)
                return "<1s ";

            if (rough.Days > 0)
                temp.Append(rough.Days + "d ");

            if (rough.Hours > 0)
                temp.Append(rough.Hours + "h ");

            if (rough.Minutes > 0)
                temp.Append(rough.Minutes + "m ");

            if (returnSeconds)
            {
                if (rough.Seconds > 0)
                    temp.Append(rough.Seconds + "s ");
            }

            if (appendAgo)
                temp.Append("ago");

            return temp.ToString();
        }

        /// <summary>
        ///     Turns a <see cref="System.DateTimeOffset" /> to a timestamp.
        /// </summary>
        /// <returns>A string in the format [hours:minutes]</returns>
        public static string ToTimeStamp(this DateTimeOffset time)
        {
            if (time.AddDays(1) < DateTime.Now)
            {
                return "[" + time.ToString("d") + "]";
            }

            return time.ToString(ApplicationSettings.UseMilitaryTime ? "[HH:mm]" : "[hh:mm tt]");
        }
    }
}