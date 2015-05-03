#region Copyright

// <copyright file="BbCodePostConverter.cs">
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
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Models;

    #endregion

    /// <summary>
    ///     Converts active messages into textblock inlines.
    /// </summary>
    public sealed class BbCodePostConverter : BbCodeBaseConverter, IMultiValueConverter
    {
        #region Constructors

        public BbCodePostConverter(IChatModel chatModel, ICharacterManager characterManager, IThemeLocator locator)
            : base(chatModel, characterManager, locator)
        {
        }

        public BbCodePostConverter()
        {
        }

        #endregion

        #region Public Methods and Operators

        public object Convert(object[] values, Type types, object parameter, CultureInfo cultureInfo)
        {
            var inlines = new List<Inline>();
            if (values.Length == 3 && values[0] is string && values[1] is ICharacter && values[2] is MessageType)
            {
                inlines.Clear(); // simple insurance that there's no junk

                var text = (string) values[0]; // this is the beef of the message
                text = HttpUtility.HtmlDecode(text); // translate the HTML characters
                var user = (ICharacter) values[1]; // this is our poster's name
                var type = (MessageType) values[2]; // what kind of type our message is

                if (type == MessageType.Roll)
                    return Parse(text);

                // this creates the name link
                var nameLink = MakeUsernameLink(user);
                inlines.Add(nameLink); // name first

                if (text[0] == '/')
                {
                    var check = text.Substring(0, text.IndexOf(' ') + 1);
                    Func<string, string> nonCommandCommand;

                    if (CommandDefinitions.NonCommandCommands.TryGetValue(check, out nonCommandCommand))
                    {
                        var command = text[1];
                        text = nonCommandCommand(text);

                        if (command == 'm') // is an emote
                        {
                            inlines.Insert(0, new Run("*")); // push the name button to the second slot
                            inlines[1].FontStyle = FontStyles.Italic;
                            inlines.Add(new Italic(Parse(text)));
                            inlines.Add(new Run("*"));
                        }
                        else if (command == 'w') // is a warn
                        {
                            var toAdd = Parse(text);
                            toAdd.Foreground = Locator.Find<Brush>("ModeratorBrush");
                            toAdd.FontWeight = FontWeights.Medium;
                            inlines.Add(toAdd);
                        }
                        else if (command == 'p') // is a post
                            inlines.Add(Parse(text));

                        return inlines;
                    }

                    inlines.Add(new Run(" : "));
                    inlines.Add(Parse(text));
                    return inlines;
                }

                inlines.Add(new Run(" : "));
                inlines.Add(new Run(text));
            }
            else if (values.Length == 1 && values[0] is NotificationModel)
            {
                inlines.Clear();

                if (values[0] is CharacterUpdateModel)
                {
                    var notification = values[0] as CharacterUpdateModel;
                    var user = notification.TargetCharacter;
                    var text = HttpUtility.HtmlDecode(notification.Arguments.ToString());

                    var nameLink = MakeUsernameLink(user);

                    inlines.Add(nameLink);
                    inlines.Add(new Run(" "));
                    inlines.Add(Parse(text));
                }
            }
            else
                inlines.Clear();

            return inlines;
        }

        public object[] ConvertBack(object values, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}