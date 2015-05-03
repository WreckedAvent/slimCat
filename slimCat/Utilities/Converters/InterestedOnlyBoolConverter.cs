#region Copyright

// <copyright file="InterestedOnlyBoolConverter.cs">
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
    /// <summary>
    ///     Converts notification Interested-only settings into descriptive strings.
    /// </summary>
    public sealed class InterestedOnlyBoolConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            if (!(value is bool))
                return null;

            var v = (bool) value;

            return v ? "only for people of interest." : "for everyone.";
        }
    }
}