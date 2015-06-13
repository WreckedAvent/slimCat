#region Copyright

// <copyright file="BbFlowConverter.cs">
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
    using System.Text;
    using System.Web;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Models;
    using Services;

    #endregion

    /// <summary>
    ///     Converts active messages into flow document inlines.
    /// </summary>
    public sealed class BbFlowConverter : BbCodeBaseConverter, IValueConverter
    {
        private readonly IGetPermissions permissions;

        #region Constructors

        public BbFlowConverter(IChatState chatState, IThemeLocator locator,
            IGetPermissions permissions)
            : base(chatState, locator)
        {
            this.permissions = permissions;
        }

        public BbFlowConverter()
        {
        }

        #endregion

        #region Public Methods and Operators

        public object Convert(object value, Type type, object parameter, CultureInfo cultureInfo)
        {
            var inlines = new List<Inline>();

            if (value == null)
                return null;

            var message = value as IMessage; // this is the beef of the message
            var text = message == null ? value as string : message.Message;

            if (string.IsNullOrEmpty(text))
                return null;

            // show more logic
            if (message != null)
            {
                // we don't want to collapse only one small sentence
                const int wiggleRoom = 75;

                if (message.Type == MessageType.Ad &&
                    text.Length > (ApplicationSettings.ShowMoreInAdsLength + wiggleRoom))
                {
                    // try to find a nice sentence to break after
                    var start = ApplicationSettings.ShowMoreInAdsLength;
                    do
                    {
                        if (char.IsPunctuation(text[start]) && char.IsWhiteSpace(text[start + 1]))
                            break;
                        start--;
                    } while (start != 0);

                    // if we didn't find one, just aggressively break at our point
                    if (start == 0)
                    {
                        start = ApplicationSettings.ShowMoreInAdsLength;
                        do
                        {
                            if (char.IsWhiteSpace(text[start]))
                                break;
                            start--;
                        } while (start != 0);
                    }

                    if (start != 0)
                    {
                        var sb = new StringBuilder(text);
                        sb.Insert(start + 1, "[collapse=Read More]");
                        sb.Append("[/collapse]");
                        text = sb.ToString();
                    }
                }
            }


            text = HttpUtility.HtmlDecode(text); // translate the HTML characters

            if (text[0] == '/')
            {
                var check = text.Substring(0, text.IndexOf(' ') + 1);
                var command = ' ';
                Func<string, string> nonCommandCommand;

                if (CommandDefinitions.NonCommandCommands.TryGetValue(check, out nonCommandCommand))
                {
                    command = text[1];
                    text = nonCommandCommand(text);
                }

                if (command == 'w' && permissions.IsModerator(message.Poster.Name))
                {
                    // a warn "command" gets a different appearance
                    var toAdd = Parse(text);
                    toAdd.Foreground = Locator.Find<Brush>("ModeratorBrush");
                    toAdd.FontWeight = FontWeights.Medium;
                    inlines.Add(toAdd);
                }
                else if (command == 'm') // is an emote
                    inlines.Add(new Italic(Parse(text)));
                else
                {
                    inlines.Add(new Run(" : "));
                    inlines.Add(Parse(text));
                }

                return inlines;
            }
            inlines.Add(new Run(": "));
            inlines.Add(Parse(text));
            return inlines;
        }

        public object ConvertBack(object value, Type type, object parameter, CultureInfo cultureInfo)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}