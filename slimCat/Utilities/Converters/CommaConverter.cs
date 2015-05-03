#region Copyright

// <copyright file="CommaConverter.cs">
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

    using System.Linq;
    using System.Text;

    #endregion

    public class CommaConverter : OneWayMultiConverter
    {
        public override object Convert(object[] values, object parameter)
        {
            // This was changed to ensure that a series of strings do not end with a comma
            var stringCount = 0;
            var toReturn = new StringBuilder();
            string parsed;

            for (var i = 0; i < values.Count(); i++)
            {
                parsed = values[i] as string;

                if (string.IsNullOrEmpty(parsed))
                    continue;

                stringCount++;
                if (stringCount >= 2)
                    toReturn.Append(", ");

                toReturn.Append(parsed);
            }

            parsed = toReturn.ToString();
            return string.IsNullOrEmpty(parsed)
                ? ""
                : parsed;
        }
    }
}