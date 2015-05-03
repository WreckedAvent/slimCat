#region Copyright

// <copyright file="CharacterStatusOpacityConverter.cs">
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

    public sealed class CharacterStatusOpacityConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
            => !(value is StatusType)
                ? 1.0
                : (((StatusType) value) == StatusType.Offline ? 0.4 : 1);
    }
}