// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Converters.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   Returns the opposite boolean value
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Web;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using Models;

    /// <summary>
    ///     Returns the opposite boolean value
    /// </summary>
    public class OppositeBoolConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object paramater, CultureInfo culture)
        {
            var v = (bool)value;
            return !v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///     If true, return a bright color
    /// </summary>
    public class BoolColorConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool)value;
            if (v)
            {
                return Application.Current.FindResource("HighlightBrush") as SolidColorBrush;
            }

            return Application.Current.FindResource("BrightBackgroundBrush") as SolidColorBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Converts active messages into textblock inlines.
    /// </summary>
    public sealed class BBCodePostConverter : IMultiValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var inlines = new List<Inline>();
            if (values.Length == 3 && values[0] is string && values[1] is ICharacter && values[2] is MessageType)
            {
                // avoids null reference
                inlines.Clear(); // simple insurance that there's no junk

                var text = (string)values[0]; // this is the beef of the message
                text = HttpUtility.HtmlDecode(text); // translate the HTML characters
                var user = (ICharacter)values[1]; // this is our poster's name
                var type = (MessageType)values[2]; // what kind of type our message is

                if (type == MessageType.Roll)
                {
                    inlines.Add(HelperConverter.ParseBbCode(text, true));
                    return inlines;
                }

                // this creates the name link
                var nameLink = HelperConverter.MakeUsernameLink(user, true);

                inlines.Add(nameLink); // name first

                

                if (text.Length > "/me".Length)
                {
                    if (text.StartsWith("/me"))
                    {
                        // if the post is a /me "command"
                        text = text.Substring("/me".Length);
                        inlines.Insert(0, new Run("*")); // push the name button to the second slot
                        inlines.Add(new Italic(HelperConverter.ParseBbCode(text, true)));
                        inlines.Add(new Run("*"));
                        return inlines;
                    }

                    if (text.StartsWith("/post"))
                    {
                        // or a post "command"
                        text = text.Substring("/post ".Length);

                        inlines.Insert(0, HelperConverter.ParseBbCode(text, true));
                        inlines.Insert(1, new Run(" ~"));
                        return inlines;
                    }

                    if (text.StartsWith("/warn"))
                    {
                        // or a warn "command"
                        text = text.Substring("/warn ".Length);
                        inlines.Add(new Run(" warns, "));
                        var toAdd = HelperConverter.ParseBbCode(text, true);

                        toAdd.Foreground = (Brush)Application.Current.FindResource("HighlightBrush");
                        toAdd.FontWeight = FontWeights.ExtraBold;
                        inlines.Add(toAdd);

                        return inlines;
                    }

                    inlines.Add(new Run(": "));
                    inlines.Add(HelperConverter.ParseBbCode(text, true));
                    return inlines;
                }

                inlines.Add(new Run(": "));
                inlines.Add(new Run(text));
            }
            else if (values.Length == 1 && values[0] is NotificationModel)
            {
                inlines.Clear();

                if (values[0] is CharacterUpdateModel)
                {
                    var notification = values[0] as CharacterUpdateModel;
                    ICharacter user = notification.TargetCharacter;
                    string text = HttpUtility.HtmlDecode(notification.Arguments.ToString());

                    Inline nameLink = HelperConverter.MakeUsernameLink(user, true);

                    inlines.Add(nameLink);
                    inlines.Add(new Run(" "));
                    inlines.Add(HelperConverter.ParseBbCode(text, true));
                }
            }
            else
            {
                inlines.Clear();
            }

            return inlines;
        }


        public object[] ConvertBack(object values, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Converts active messages into flow document inlines.
    /// </summary>
    public sealed class BBFlowConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inlines = new List<Inline>();

            if (value == null)
            {
                return null;
            }

            var text = (string)value; // this is the beef of the message
            text = HttpUtility.HtmlDecode(text); // translate the HTML characters

            if (text.StartsWith("/me"))
            {
                // if the post is a /me "command"
                text = text.Substring("/me".Length);
                inlines.Add(new Italic(HelperConverter.ParseBbCode(text, true)));
                inlines.Add(new Run("*"));
            }
            else if (text.StartsWith("/post"))
            {
                // or a post "command"
                text = text.Substring("/post ".Length);

                inlines.Insert(0, HelperConverter.ParseBbCode(text, true));
                inlines.Insert(1, new Run(" ~"));
            }
            else if (text.StartsWith("/warn"))
            {
                // or a warn "command"
                text = text.Substring("/warn ".Length);
                inlines.Add(new Run(" warns, "));
                Inline toAdd = HelperConverter.ParseBbCode(text, true);

                toAdd.Foreground = (Brush)Application.Current.FindResource("HighlightBrush");
                toAdd.FontWeight = FontWeights.ExtraBold;
                inlines.Add(toAdd);
            }
            else
            {
                inlines.Add(new Run(": "));
                inlines.Add(HelperConverter.ParseBbCode(text, true));
            }

            return inlines;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Converts history messages into flow document inlines.
    /// </summary>
    public sealed class BBFlowHistoryConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inlines = new List<Inline>();

            if (value == null)
            {
                return null;
            }

            var text = (string)value; // this is the beef of the message
            text = HttpUtility.HtmlDecode(text); // translate the HTML characters

            inlines.Add(HelperConverter.ParseBbCode(text, true));

            return inlines;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///  Converts history messages into textblock inlines.
    /// </summary>
    public sealed class BBCodeConverter : IValueConverter
    {
        #region Explicit Interface Methods

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var text = value as string;

            if (text == null)
            {
                return null;
            }

            text = HttpUtility.HtmlDecode(text);

            IList<Inline> toReturn = new List<Inline>();
            toReturn.Add(HelperConverter.ParseBbCode(text, false));

            return toReturn;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Converts gender string into gender color.
    /// </summary>
    public sealed class GenderColorConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var gender = (Gender)value;

                switch (gender)
                {
                    case Gender.Herm_F:
                        return
                            new LinearGradientBrush(
                                new GradientStopCollection(
                                    new List<GradientStop>
                                        {
                                            new GradientStop(
                                                (Color)
                                                Application.Current.FindResource("ForegroundColor"), 
                                                0.0), 
                                            new GradientStop(
                                                (Color)
                                                Application.Current.FindResource("ForegroundColor"), 
                                                0.5), 
                                            new GradientStop(
                                                (Color)
                                                Application.Current.FindResource("HighlightColor"), 
                                                0.5), 
                                            new GradientStop(
                                                (Color)
                                                Application.Current.FindResource("HighlightColor"), 
                                                1.0)
                                        }), 
                                0);
                    case Gender.Herm_M:
                        return
                            new LinearGradientBrush(
                                new GradientStopCollection(
                                    new List<GradientStop>
                                        {
                                            new GradientStop(
                                                (Color)
                                                Application.Current.FindResource("ForegroundColor"), 
                                                0.0), 
                                            new GradientStop(
                                                (Color)
                                                Application.Current.FindResource("ForegroundColor"), 
                                                0.5), 
                                            new GradientStop(
                                                (Color)
                                                Application.Current.FindResource("HighlightColor"), 
                                                0.5), 
                                            new GradientStop(
                                                (Color)
                                                Application.Current.FindResource("HighlightColor"), 
                                                1.0)
                                        }));

                    case Gender.Cuntboy:
                    case Gender.Shemale:
                        return Application.Current.FindResource("ForegroundBrush");
                    default:
                        return Application.Current.FindResource("HighlightBrush");
                }
            }
            catch
            {
                return new SolidColorBrush();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Converts gender string into a gender image.
    /// </summary>
    public sealed class GenderImageConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var gender = (Gender)value;
                Uri uri;

                switch (gender)
                {
                    case Gender.Herm_M:
                    case Gender.Cuntboy:
                    case Gender.Male:
                        uri = new Uri("pack://application:,,,/icons/male.png");
                        break;
                    case Gender.Herm_F:
                    case Gender.Female:
                    case Gender.Shemale:
                        uri = new Uri("pack://application:,,,/icons/female.png");
                        break;
                    case Gender.Transgender:
                        uri = new Uri("pack://application:,,,/icons/transgender.png");
                        break;
                    default:
                        uri = new Uri("pack://application:,,,/icons/none.png");
                        break;
                }

                return new BitmapImage(uri);
            }
            catch
            {
                return new BitmapImage();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Converts notify level into strings.
    /// </summary>
    public sealed class NotifyLevelConverter : IValueConverter
    {
        #region Public Methods and Operators

        /// <summary>
        /// The convert.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var toParse = (int)value;
            var notificationType = parameter as string;
            string verboseNotificationKind = "• A notification";

            if (notificationType != null && notificationType.Equals("flash"))
            {
                verboseNotificationKind = "• A tab flash";
            }

            switch ((ChannelSettingsModel.NotifyLevel)toParse)
            {
                case ChannelSettingsModel.NotifyLevel.NotificationOnly:
                    return verboseNotificationKind;
                case ChannelSettingsModel.NotifyLevel.NotificationAndToast:
                    return verboseNotificationKind + "\n• A toast";
                case ChannelSettingsModel.NotifyLevel.NotificationAndSound:
                    return verboseNotificationKind + "\n• A toast with sound\n• 5 Window Flashes";

                default:
                    return "Nothing!";
            }
        }

        /// <summary>
        /// The convert back.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Converts Interested-only data into strings
    /// </summary>
    public sealed class InterestedOnlyBoolConverter : IValueConverter
    {
        #region Public Methods and Operators

        /// <summary>
        /// The convert.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
            {
                return null;
            }

            var v = (bool)value;

            return v ? "only for people of interest." : "for everyone.";
        }

        /// <summary>
        /// The convert back.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// Converts a channel type enum into a channel type image representation.
    /// </summary>
    public sealed class ChannelTypeToImageConverter : IValueConverter
    {
        #region Public Methods and Operators

        /// <summary>
        /// The convert.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var args = (ChannelType)value;
            var uri = new Uri("pack://application:,,,/icons/chat.png");
            switch (args)
            {
                case ChannelType.PrivateMessage:
                    uri = new Uri("pack://application:,,,/icons/chat.png");
                    break;
                case ChannelType.InviteOnly:
                    uri = new Uri("pack://application:,,,/icons/private_closed.png");
                    break;
                case ChannelType.Private:
                    uri = new Uri("pack://application:,,,/icons/private_open.png");
                    break;
                case ChannelType.Public:
                    uri = new Uri("pack://application:,,,/icons/public.png");
                    break;
            }

            return new BitmapImage(uri);
        }

        /// <summary>
        /// The convert back.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///     Various conversion methods
    /// </summary>
    public static class HelperConverter
    {
        #region Static Fields

        private static readonly IList<string> BbType = new List<string>
                                                           {
                                                               "b", 
                                                               "s", 
                                                               "u", 
                                                               "i", 
                                                               "url", 
                                                               "color", 
                                                               "channel", 
                                                               "session", 
                                                               "user", 
                                                               "noparse", 
                                                               "icon", 
                                                               "sub", 
                                                               "sup", 
                                                               "small", 
                                                               "big"
                                                           };

        private static readonly IList<string> SpecialBbCases = new List<string> { "url", "channel", "user", "icon", };

        // determines if a string has a (valid) opening BBCode tag
        private static readonly string[] ValidStartTerms = new[] { "http://", "https://", "ftp://" };

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Like above, but works for dates in the time
        /// </summary>
        /// <param name="futureTime">
        /// The future Time.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string DateTimeInFutureToRough(DateTimeOffset futureTime)
        {
            var temp = new StringBuilder();
            TimeSpan rough = futureTime - DateTimeOffset.Now;

            if (rough.Days > 0)
            {
                temp.Append(rough.Days + "d ");
            }

            if (rough.Hours > 0)
            {
                temp.Append(rough.Hours + "h ");
            }

            if (rough.Minutes > 0)
            {
                temp.Append(rough.Minutes + "m ");
            }

            if (rough.Seconds > 0)
            {
                temp.Append(rough.Seconds + "s ");
            }

            if (temp.Length < 2)
            {
                temp.Append("0s ");
            }

            return temp.ToString();
        }

        /// <summary>
        /// Converts a datetimeoffset to a "x h x m x s ago" format
        /// </summary>
        /// <param name="original">
        /// The original.
        /// </param>
        /// <param name="returnSeconds">
        /// The return Seconds.
        /// </param>
        /// <param name="appendAgo">
        /// The append Ago.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string DateTimeToRough(DateTimeOffset original, bool returnSeconds = false, bool appendAgo = true)
        {
            var temp = new StringBuilder();
            TimeSpan rough = DateTimeOffset.Now - original;
            int tolerance = returnSeconds ? 1 : 60;

            if (rough.TotalSeconds < tolerance)
            {
                return "<1s ";
            }

            if (rough.Days > 0)
            {
                temp.Append(rough.Days + "d ");
            }

            if (rough.Hours > 0)
            {
                temp.Append(rough.Hours + "h ");
            }

            if (rough.Minutes > 0)
            {
                temp.Append(rough.Minutes + "m ");
            }

            if (returnSeconds)
            {
                if (rough.Seconds > 0)
                {
                    temp.Append(rough.Seconds + "s ");
                }
            }

            if (appendAgo)
            {
                temp.Append("ago");
            }

            return temp.ToString();
        }

        /// <summary>
        /// Used for shitty shit channel names with spaces
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string EscapeSpaces(string text)
        {
            return text.Replace(" ", "___");
        }

        /// <summary>
        /// Used to make a username 'button'
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="useTunnelingStyles">
        /// The use Tunneling Styles.
        /// </param>
        /// <returns>
        /// The <see cref="Inline"/>.
        /// </returns>
        public static Inline MakeUsernameLink(ICharacter target, bool useTunnelingStyles)
        {
            var toReturn =
                new InlineUIContainer(
                    new ContentControl
                        {
                            ContentTemplate =
                                (DataTemplate)
                                Application.Current.FindResource(
                                    useTunnelingStyles ? "TunnelingUsernameTemplate" : "UsernameTemplate"), 
                            Content = target
                        }) {
                              BaselineAlignment = BaselineAlignment.TextBottom 
                           };

            return toReturn;
        }

        /// <summary>
        /// The heart of BBCode converstion, turns a string of text into an inline
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="useTunnelingStyles">
        /// The use Tunneling Styles.
        /// </param>
        /// <returns>
        /// The <see cref="Inline"/>.
        /// </returns>
        public static Inline ParseBbCode(string text, bool useTunnelingStyles)
        {
            return BbcodeToInline(PreProcessBbCode(text), useTunnelingStyles);
        }

        /// <summary>
        /// Right now, all this does is warp a url tag around links.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string PreProcessBbCode(string text)
        {
            if (!ContainsUrl(text))
            {
                return text; // if there's no url in it, we don't have a link to mark up
            }

            var matches = from word in text.Split(' ')
                          where StartsWithValidTerm(word)
                          select new Tuple<string, string>(word, MarkUpUrlWithBbCode(word));

            return matches.Aggregate(text, (current, toReplace) => current.Replace(toReplace.Item1, toReplace.Item2)); 
        }

        /// <summary>
        /// Turns a datetime to a timestamp (hh:mm)
        /// </summary>
        /// <param name="time">
        /// The time.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string ToTimeStamp(this DateTimeOffset time)
        {
            var minute = time.Minute;
            var minuteFix = minute.ToString(CultureInfo.InvariantCulture).Insert(0, minute < 10 ? "0" : string.Empty);

            return "[" + time.Hour + ":" + minuteFix + "]";
        }

        /// <summary>
        /// Converts a POSIX/UNIX timecode to a datetime
        /// </summary>
        /// <param name="time">
        /// The time.
        /// </param>
        /// <returns>
        /// The <see cref="DateTimeOffset"/>.
        /// </returns>
        public static DateTimeOffset UnixTimeToDateTime(long time)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return new DateTimeOffset(epoch.AddSeconds(time));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts a marked-up BBCode string to a flowdocument inline
        /// </summary>
        /// <param name="x">
        /// string to convert
        /// </param>
        /// <param name="useTunnelingStyles">
        /// if the styles used need to tunnel up the visual tree
        /// </param>
        /// <returns>
        /// The <see cref="Inline"/>.
        /// </returns>
        private static Inline BbcodeToInline(string x, bool useTunnelingStyles)
        {
            if (!HasOpenTag(x))
            {
                return new Run(x);
            }

            var toReturn = new Span();

            var startType = FindStartType(x);
            var startIndex = x.IndexOf("[" + startType + "]", StringComparison.Ordinal);

            if (startIndex > 0)
            {
                toReturn.Inlines.Add(new Run(x.Substring(0, startIndex)));
            }

            if (startType.StartsWith("session"))
            {
                // hack-in for session to work
                var rough = x.Substring(startIndex);
                var firstBrace = rough.IndexOf(']');
                var endInd = rough.IndexOf("[/session]", StringComparison.Ordinal);

                if (firstBrace != -1 || endInd != -1)
                {
                    var channel = rough.Substring(firstBrace + 1, endInd - firstBrace - 1);
                    var title = rough.Substring("[session=".Length, firstBrace - "[session=".Length);

                    if (!title.Contains("ADH-"))
                    {
                        x = x.Replace(channel, title);
                        x = x.Replace("[session=" + title + "]", "[session=" + channel + "]");
                        startType = FindStartType(x);
                    }
                }
            }

            string roughString = x.Substring(startIndex);
            roughString = roughString.Remove(0, roughString.IndexOf(']') + 1);

            string endType = FindEndType(roughString);
            int endIndex = roughString.IndexOf("[/" + endType + "]", StringComparison.Ordinal);
            int endLength = ("[/" + endType + "]").Length;

            // for BBCode with arguments, we must do this
            if (SpecialBbCases.Any(bbcase => startType.Equals(bbcase)))
            {
                startType += "=";

                string content = endIndex != -1 ? roughString.Substring(0, endIndex) : roughString;

                startType += content;
            }

            if (startType == "noparse")
            {
                endIndex = roughString.IndexOf("[/noparse]", StringComparison.Ordinal);
                string restofString = roughString.Substring(endIndex + "[/noparse]".Length);
                string skipthis = roughString.Substring(0, endIndex);

                toReturn.Inlines.Add(new Run(skipthis));
                toReturn.Inlines.Add(BbcodeToInline(restofString, useTunnelingStyles));
            }
            else if (endType == "n" || endIndex == -1)
            {
                toReturn.Inlines.Add(TypeToInline(new Run(roughString), startType, useTunnelingStyles));
            }
            else if (endType != startType)
            {
                var properEnd = "[/" + StripAfterType(startType) + "]";
                if (roughString.Contains(properEnd))
                {
                    var properIndex = roughString.IndexOf(properEnd, StringComparison.Ordinal);
                    var toMarkUp = roughString.Substring(0, properIndex);
                    var restOfString = roughString.Substring(properIndex + properEnd.Length);

                    toReturn.Inlines.Add(
                        TypeToInline(BbcodeToInline(toMarkUp, useTunnelingStyles), startType, useTunnelingStyles));

                    toReturn.Inlines.Add(BbcodeToInline(restOfString, useTunnelingStyles));
                }
                else
                {
                    toReturn.Inlines.Add(
                        TypeToInline(BbcodeToInline(roughString, useTunnelingStyles), startType, useTunnelingStyles));
                }
            }
            else if (endIndex + endLength == roughString.Length)
            {
                roughString = roughString.Remove(endIndex, endLength);
                toReturn.Inlines.Add(TypeToInline(new Run(roughString), startType, useTunnelingStyles));
            }
            else
            {
                string restOfString = roughString.Substring(endIndex + endLength);

                roughString = roughString.Substring(0, endIndex);

                toReturn.Inlines.Add(TypeToInline(new Run(roughString), startType, useTunnelingStyles));

                toReturn.Inlines.Add(BbcodeToInline(restOfString, useTunnelingStyles));
            }

            return toReturn;
        }

        private static bool ContainsUrl(string args)
        {
            string match = args.Split(' ').FirstOrDefault(StartsWithValidTerm);

            // see if it starts with something useful
            if (match == null)
            {
                return false;
            }

            var starter = ValidStartTerms.First(match.StartsWith);
            return args.Trim().Length > starter.Length;
        }

        private static string FindEndType(string x)
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 4)
            {
                return "n";
            }

            var end = x.IndexOf(']', root);
            if (end == -1)
            {
                return "n";
            }

            if (x[root + 1] != '/')
            {
                return FindEndType(x.Substring(end + 1));
            }

            var type = x.Substring(root + 2, end - (root + 2));

            return BbType.Any(type.Equals) ? type : FindEndType(x.Substring(end + 1));
        }

        private static string FindStartType(string x)
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 3)
            {
                return "n";
            }

            var end = x.IndexOf(']', root);
            if (end == -1)
            {
                return "n";
            }

            var type = x.Substring(root + 1, end - root - 1);

            return BbType.Any(type.StartsWith) ? type : FindStartType(x.Substring(end + 1));
        }

        private static string GetUrlDisplay(string args)
        {
            var match = ValidStartTerms.FirstOrDefault(args.StartsWith);
            var stripped = args.Substring(match.Length);

            if (stripped.Contains('/'))
            {
                // remove anything after the slash
                stripped = stripped.Substring(0, stripped.IndexOf('/'));
            }

            if (stripped.StartsWith("www."))
            {
                // remove the www.
                stripped = stripped.Substring("www.".Length);
            }

            return stripped;
        }

        private static bool HasCloseTag(string x)
        {
            var root = x.IndexOf("[/", StringComparison.Ordinal);
            if (root == -1 || x.Length < 4 || x[root + 1] != '/')
            {
                return false;
            }

            var end = x.IndexOf(']', root);
            if (end == -1)
            {
                return false;
            }

            var type = x.Substring(root + 1, end - root - 1);

            return BbType.Any(bbtype => bbtype.Equals(type)) || HasCloseTag(x.Substring(end + 1));
        }

        private static bool HasOpenTag(string x)
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 3)
            {
                return false;
            }

            var end = x.IndexOf(']', root);
            if (end == -1)
            {
                return false;
            }

            if (x[root + 1] == '/')
            {
                HasOpenTag(x.Substring(end));
            }

            var type = x.Substring(root + 1, end - root - 1);

            return BbType.Any(type.StartsWith) || HasOpenTag(x.Substring(end + 1));
        }

        private static string MarkUpUrlWithBbCode(string args)
        {
            string toShow = GetUrlDisplay(args);
            return "[url=" + args + "]" + toShow + "[/url]"; // mark the bitch up
        }

        private static string RemoveFirstEndType(string x, string type)
        {
            var endtype = "[/" + type + "]";
            var firstOccur = x.IndexOf(endtype, StringComparison.Ordinal);

            var interestedin = x.Substring(0, firstOccur);
            var rest = x.Substring(firstOccur + endtype.Length);

            return interestedin + rest;
        }

        private static bool StartsWithValidTerm(string text)
        {
            return ValidStartTerms.Any(text.StartsWith);
        }

        private static string StripAfterType(string z)
        {
            return z.Contains('=') ? z.Substring(0, z.IndexOf('=')) : z;
        }

        private static string StripBeforeType(string z)
        {
            if (z.Contains('='))
            {
                var type = z.Substring(0, z.IndexOf('='));
                return z.Substring(z.IndexOf('=') + 1, z.Length - (type.Length + 1));
            }

            return z;
        }

        /// <summary>
        /// Turns wraps x with an flowdocument inline matching y
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <param name="useTunnelingStyles">
        /// The use Tunneling Styles.
        /// </param>
        /// <returns>
        /// The <see cref="Inline"/>.
        /// </returns>
        private static Inline TypeToInline(Inline x, string y, bool useTunnelingStyles)
        {
            switch (y)
            {
                case "b":
                    return new Bold(x);
                case "u":
                    return new Span(x) { TextDecorations = TextDecorations.Underline };
                case "i":
                    return new Italic(x);
                case "s":
                    return new Span(x) { TextDecorations = TextDecorations.Strikethrough };
                case "sub":
                    return new Span(x) { BaselineAlignment = BaselineAlignment.Subscript, FontSize = 10 };
                case "sup":
                    return new Span(x) { BaselineAlignment = BaselineAlignment.Top, FontSize = 10 };
                case "small":
                    return new Span(x) { FontSize = 9 };
                case "big":
                    return new Span(x) { FontSize = 16 };
            }

            if (y.StartsWith("url"))
            {
                var url = StripBeforeType(y);

                var toReturn = new Hyperlink(x) { CommandParameter = url, ToolTip = url };

                if (useTunnelingStyles)
                {
                    toReturn.Style = (Style)Application.Current.FindResource("TunnelingHyperlink");
                }

                return toReturn;
            }

            if (y.StartsWith("user") || y.StartsWith("icon"))
            {
                var target = StripBeforeType(y);

                return MakeUsernameLink(new CharacterModel { Name = target, Gender = Gender.None }, useTunnelingStyles);
            }

            if (y.StartsWith("channel") || y.StartsWith("session"))
            {
                var channel = StripBeforeType(y);

                return
                    new InlineUIContainer(
                        new Button
                            {
                                Content = x, 
                                Style = (Style)Application.Current.FindResource("ChannelInterfaceButton"), 
                                CommandParameter = channel
                            }) {
                                  BaselineAlignment = BaselineAlignment.TextBottom 
                               };
            }

            return new Span(x);
        }

        #endregion
    }
}