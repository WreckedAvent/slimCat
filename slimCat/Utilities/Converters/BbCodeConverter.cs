#region Copyright

// <copyright file="BbCodeConverter.cs">
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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Web;
    using System.Windows.Data;
    using System.Windows.Documents;
    using Models;
    using Services;

    #endregion

    /// <summary>
    ///     Converts history messages into document inlines.
    /// </summary>
    public sealed class BbCodeConverter : BbCodeBaseConverter, IValueConverter
    {
        #region Constructors

        public BbCodeConverter(IChatState chatState, IThemeLocator locator)
            : base(chatState, locator)
        {
        }

        public BbCodeConverter()
        {
        }

        #endregion

        #region Explicit Interface Methods
        IList<Inline> toReturn = new List<Inline>();  //reusing the same variable instead of making new ones each call,
                                                      //because it risks crashing from using too much memory otherwise
        object IValueConverter.Convert(object value, Type type, object parameter, CultureInfo cultureInfo)
        {
            if (value == null)
                return null;

            var text = value as string ?? value.ToString();

            text = HttpUtility.HtmlDecode(text);

            toReturn.Clear();
            toReturn.Add(Parse(text));

            return toReturn;
        }

        object IValueConverter.ConvertBack(object value, Type type, object parameter, CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}