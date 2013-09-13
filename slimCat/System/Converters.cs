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

namespace System
{
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

        /// <summary>
        /// The convert.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="targetType">
        /// The target type.
        /// </param>
        /// <param name="paramater">
        /// The paramater.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object paramater, CultureInfo culture)
        {
            var v = (bool)value;
            return !v;
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
    ///     If true, return a bright color
    /// </summary>
    public class BoolColorConverter : IValueConverter
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
            var v = (bool)value;
            if (v)
            {
                return Application.Current.FindResource("HighlightBrush") as SolidColorBrush;
            }

            return Application.Current.FindResource("BrightBackgroundBrush") as SolidColorBrush;
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
    ///     This is a primary converter which pulls some WPFy magic to convert BBCode
    /// </summary>
    public sealed class BBCodePostConverter : IMultiValueConverter
    {
        #region Public Methods and Operators

        /// <summary>
        /// The convert.
        /// </summary>
        /// <param name="values">
        /// The values.
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

                if (type == MessageType.roll)
                {
                    inlines.Add(HelperConverter.ParseBBCode(text, true));
                    return inlines;
                }

                // this creates the name link
                Inline nameLink = HelperConverter.MakeUsernameLink(user, true);

                inlines.Add(nameLink); // name first

                

                if (text.Length > "/me".Length)
                {
                    if (text.StartsWith("/me"))
                    {
                        // if the post is a /me "command"
                        text = text.Substring("/me".Length);
                        inlines.Insert(0, new Run("*")); // push the name button to the second slot
                        inlines.Add(new Italic(HelperConverter.ParseBBCode(text, true)));
                        inlines.Add(new Run("*"));
                        return inlines;
                    }
                    else if (text.StartsWith("/post"))
                    {
                        // or a post "command"
                        text = text.Substring("/post ".Length);

                        inlines.Insert(0, HelperConverter.ParseBBCode(text, true));
                        inlines.Insert(1, new Run(" ~"));
                        return inlines;
                    }
                    else if (text.StartsWith("/warn"))
                    {
                        // or a warn "command"
                        text = text.Substring("/warn ".Length);
                        inlines.Add(new Run(" warns, "));
                        Inline toAdd = HelperConverter.ParseBBCode(text, true);

                        toAdd.Foreground = (Brush)Application.Current.FindResource("HighlightBrush");
                        toAdd.FontWeight = FontWeights.ExtraBold;
                        inlines.Add(toAdd);

                        return inlines;
                    }

                    inlines.Add(new Run(": "));
                    inlines.Add(HelperConverter.ParseBBCode(text, true));
                    return inlines;
                }

                

                #region Fallback addition

                inlines.Add(new Run(": "));
                inlines.Add(new Run(text));

                #endregion
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
                    inlines.Add(HelperConverter.ParseBBCode(text, true));
                }
            }
            else
            {
                inlines.Clear();
            }

            return inlines;
        }

        /// <summary>
        /// The convert back.
        /// </summary>
        /// <param name="values">
        /// The values.
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
        /// The <see cref="object[]"/>.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public object[] ConvertBack(object values, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///     The bb flow converter.
    /// </summary>
    public sealed class BBFlowConverter : IValueConverter
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
                inlines.Add(new Italic(HelperConverter.ParseBBCode(text, true)));
                inlines.Add(new Run("*"));
            }
            else if (text.StartsWith("/post"))
            {
                // or a post "command"
                text = text.Substring("/post ".Length);

                inlines.Insert(0, HelperConverter.ParseBBCode(text, true));
                inlines.Insert(1, new Run(" ~"));
            }
            else if (text.StartsWith("/warn"))
            {
                // or a warn "command"
                text = text.Substring("/warn ".Length);
                inlines.Add(new Run(" warns, "));
                Inline toAdd = HelperConverter.ParseBBCode(text, true);

                toAdd.Foreground = (Brush)Application.Current.FindResource("HighlightBrush");
                toAdd.FontWeight = FontWeights.ExtraBold;
                inlines.Add(toAdd);
            }
            else
            {
                inlines.Add(new Run(": "));
                inlines.Add(HelperConverter.ParseBBCode(text, true));
            }

            return inlines;
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
    ///     This is a secondary converter for BBCode when there is no 'post', such as  room description
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
            toReturn.Add(HelperConverter.ParseBBCode(text, false));

            return toReturn;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///     Colors the gender icons based
    /// </summary>
    public sealed class GenderColorConverter : IValueConverter
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
    ///     Creates gender icons from gender data type
    /// </summary>
    public sealed class GenderImageConverter : IValueConverter
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
    ///     Converts notify level into strings
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
    ///     Converts Interested-only data into strings
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

            if (v)
            {
                return "only for people of interest.";
            }
            else
            {
                return "for everyone.";
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
    ///     Turns a channel type into an image source
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
                case ChannelType.pm:
                    uri = new Uri("pack://application:,,,/icons/chat.png");
                    break;
                case ChannelType.closed:
                    uri = new Uri("pack://application:,,,/icons/private_closed.png");
                    break;
                case ChannelType.priv:
                    uri = new Uri("pack://application:,,,/icons/private_open.png");
                    break;
                case ChannelType.pub:
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

        private static readonly IList<string> BBType = new List<string>
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

        private static readonly IList<string> SpecialBBCases = new List<string> { "url", "channel", "user", "icon", };

        // determines if a string has a (valid) opening BBCode tag
        private static readonly string[] validStartTerms = new[] { "http://", "https://", "ftp://" };

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
        public static Inline ParseBBCode(string text, bool useTunnelingStyles)
        {
            return bbcodeToInline(PreProcessBBCode(text), useTunnelingStyles);
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
        public static string PreProcessBBCode(string text)
        {
            if (containsUrl(text))
            {
                // #region Find start and end of url
                // int start;
                IEnumerable<Tuple<string, string>> matches = from word in text.Split(' ')
                                                             // explode them into an array
                                                             where startsWithValidTerm(word)
                                                             select
                                                                 new Tuple<string, string>(
                                                                 word, markUpUrlWithBBCode(word));

                // put the match terms into a useful tuple
                string toReturn = text;
                foreach (var toReplace in matches)
                {
                    toReturn = toReturn.Replace(toReplace.Item1, toReplace.Item2); // replace each match
                }

                return toReturn; // return the replacements

                /*
                var match = text.Split(' ').FirstOrDefault(word => validStartTerms.Any(term => word.StartsWith(term)));

                if (match == null)
                    return text;
                else
                    start = text.IndexOf(match);

                int end = text.IndexOf(text.Skip(start).FirstOrDefault(Char.IsWhiteSpace), start); // find the first space

                if (end == -1) // if the string doesn't contain a space, assume the entire thing is part of the url
                    end = text.Length;
                end -= start;

                string fullurl = text.Substring(start, end); // this should be our entire url
                string final = null;
                #endregion

                #region mark it up
                if (start == 0 || Char.IsWhiteSpace(text[start - 1]))
                {
                    string toShow = getUrlDisplay(fullurl);
                    string markedupurl = "[url=" + fullurl + "]" + toShow + "[/url]"; // mark the bitch up

                    final = text.Replace(fullurl, markedupurl); // then replace it in our string
                    end = final.IndexOf(' ', start); // we just changed our string so we need to find our new end.
                    if (end == -1) end = final.Length;
                }
                #endregion

                #region recursively proess the rest of the string
                string sub;
                string rest;

                if (final != null) // if we wrapped our link with BBcode
                {
                    sub = final.Substring(0, end); // what we've already processed
                    rest = final.Substring(end); // what is left to process
                }

                else
                {
                    sub = text.Substring(0, start) + fullurl;
                    rest = text.Substring(end+start);
                }

                if (!String.IsNullOrWhiteSpace(rest)) // if there's something after our url, then process it recursively
                    return sub + PreProcessBBCode(rest);
                #endregion

                return sub; // otherwise we have our pre-processed url
                */
            }
            else
            {
                return text; // if there's no url in it, we don't have a link to mark up
            }
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
            int minute = time.Minute;
            string minuteFix = minute.ToString().Insert(0, minute < 10 ? "0" : string.Empty);

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
        private static Inline bbcodeToInline(string x, bool useTunnelingStyles)
        {
            if (!hasOpenTag(x))
            {
                return new Run(x);
            }
            else
            {
                var toReturn = new Span();

                string startType = findStartType(x);
                int startIndex = x.IndexOf("[" + startType + "]");

                if (startIndex > 0)
                {
                    toReturn.Inlines.Add(new Run(x.Substring(0, startIndex)));
                }

                if (startType.StartsWith("session"))
                {
                    // hack-in for session to work
                    string rough = x.Substring(startIndex);
                    int firstBrace = rough.IndexOf(']');
                    int endInd = rough.IndexOf("[/session]");

                    if (firstBrace != -1 || endInd != -1)
                    {
                        string channel = rough.Substring(firstBrace + 1, endInd - firstBrace - 1);
                        string title = rough.Substring("[session=".Length, firstBrace - "[session=".Length);

                        if (!title.Contains("ADH-"))
                        {
                            x = x.Replace(channel, title);
                            x = x.Replace("[session=" + title + "]", "[session=" + channel + "]");
                            startType = findStartType(x);
                        }
                    }
                }

                string roughString = x.Substring(startIndex);
                roughString = roughString.Remove(0, roughString.IndexOf(']') + 1);

                string endType = findEndType(roughString);
                int endIndex = roughString.IndexOf("[/" + endType + "]");
                int endLength = ("[/" + endType + "]").Length;

                // for BBCode with arguments, we must do this
                if (SpecialBBCases.Any(bbcase => startType.Equals(bbcase)))
                {
                    startType += "=";

                    string content;
                    if (endIndex != -1)
                    {
                        content = roughString.Substring(0, endIndex);
                    }
                    else
                    {
                        content = roughString;
                    }

                    startType += content;
                }

                if (startType == "noparse")
                {
                    endIndex = roughString.IndexOf("[/noparse]");
                    string restofString = roughString.Substring(endIndex + "[/noparse]".Length);
                    string skipthis = roughString.Substring(0, endIndex);

                    toReturn.Inlines.Add(new Run(skipthis));
                    toReturn.Inlines.Add(bbcodeToInline(restofString, useTunnelingStyles));
                }
                else if (endType == "n" || endIndex == -1)
                {
                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType, useTunnelingStyles));
                }
                else if (endType != startType)
                {
                    string properEnd = "[/" + stripAfterType(startType) + "]";
                    if (roughString.Contains(properEnd))
                    {
                        int properIndex = roughString.IndexOf(properEnd);
                        string toMarkUp = roughString.Substring(0, properIndex);
                        string restOfString = roughString.Substring(properIndex + properEnd.Length);

                        toReturn.Inlines.Add(
                            typeToInline(bbcodeToInline(toMarkUp, useTunnelingStyles), startType, useTunnelingStyles));

                        toReturn.Inlines.Add(bbcodeToInline(restOfString, useTunnelingStyles));
                    }
                    else
                    {
                        toReturn.Inlines.Add(
                            typeToInline(bbcodeToInline(roughString, useTunnelingStyles), startType, useTunnelingStyles));
                    }
                }
                else if (endIndex + endLength == roughString.Length)
                {
                    roughString = roughString.Remove(endIndex, endLength);
                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType, useTunnelingStyles));
                }
                else
                {
                    string restOfString = roughString.Substring(endIndex + endLength);

                    roughString = roughString.Substring(0, endIndex);

                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType, useTunnelingStyles));

                    toReturn.Inlines.Add(bbcodeToInline(restOfString, useTunnelingStyles));
                }

                return toReturn;
            }
        }

        private static bool containsUrl(string args)
        {
            string match = args.Split(' ').FirstOrDefault(word => startsWithValidTerm(word));

            // see if it starts with something useful
            if (match == null)
            {
                return false;
            }
            else
            {
                string starter = validStartTerms.First(term => match.StartsWith(term));
                return args.Trim().Length > starter.Length;
            }
        }

        private static string findEndType(string x)
        {
            int root = x.IndexOf('[');
            if (root == -1 || x.Length < 4)
            {
                return "n";
            }

            int end = x.IndexOf(']', root);
            if (end == -1)
            {
                return "n";
            }

            if (x[root + 1] != '/')
            {
                return findEndType(x.Substring(end + 1));
            }

            string type = x.Substring(root + 2, end - (root + 2));

            if (BBType.Any(bbtype => type.Equals(bbtype)))
            {
                return type;
            }
            else
            {
                return findEndType(x.Substring(end + 1));
            }
        }

        private static string findStartType(string x)
        {
            int root = x.IndexOf('[');
            if (root == -1 || x.Length < 3)
            {
                return "n";
            }

            int end = x.IndexOf(']', root);
            if (end == -1)
            {
                return "n";
            }

            string type = x.Substring(root + 1, end - root - 1);

            if (BBType.Any(bbtype => type.StartsWith(bbtype)))
            {
                return type;
            }
            else
            {
                return findStartType(x.Substring(end + 1));
            }
        }

        private static string getUrlDisplay(string args)
        {
            // forgot about the wonderful principle of KISS. This works better and is way more simple
            string stripped;
            string match = validStartTerms.FirstOrDefault(term => args.StartsWith(term));
            stripped = args.Substring(match.Length); // remove the starting term

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

        private static bool hasCloseTag(string x)
        {
            int root = x.IndexOf("[/");
            if (root == -1 || x.Length < 4 || x[root + 1] != '/')
            {
                return false;
            }

            int end = x.IndexOf("]", root);
            if (end == -1)
            {
                return false;
            }

            string type = x.Substring(root + 1, end - root - 1);

            if (!BBType.Any(bbtype => bbtype.Equals(type)))
            {
                return hasCloseTag(x.Substring(end + 1));
            }
            else
            {
                return true;
            }
        }

        private static bool hasOpenTag(string x)
        {
            int root = x.IndexOf('[');
            if (root == -1 || x.Length < 3)
            {
                return false;
            }

            int end = x.IndexOf(']', root);
            if (end == -1)
            {
                return false;
            }

            if (x[root + 1] == '/')
            {
                hasOpenTag(x.Substring(end));
            }

            string type = x.Substring(root + 1, end - root - 1);

            if (!BBType.Any(bbtype => type.StartsWith(bbtype)))
            {
                return hasOpenTag(x.Substring(end + 1));
            }
            else
            {
                return true;
            }
        }

        private static string markUpUrlWithBBCode(string args)
        {
            string toShow = getUrlDisplay(args);
            return "[url=" + args + "]" + toShow + "[/url]"; // mark the bitch up
        }

        private static string removeFirstEndType(string x, string type)
        {
            string endtype = "[/" + type + "]";
            int firstOccur = x.IndexOf(endtype);

            string interestedin = x.Substring(0, firstOccur);
            string rest = x.Substring(firstOccur + endtype.Length);

            return interestedin + rest;
        }

        private static bool startsWithValidTerm(string text)
        {
            return validStartTerms.Any(term => text.StartsWith(term));
        }

        private static string stripAfterType(string z)
        {
            if (z.Contains('='))
            {
                return z.Substring(0, z.IndexOf('='));
            }
            else
            {
                return z;
            }
        }

        private static string stripBeforeType(string z)
        {
            if (z.Contains('='))
            {
                string type = z.Substring(0, z.IndexOf('='));
                return z.Substring(z.IndexOf('=') + 1, z.Length - (type.Length + 1));
            }
            else
            {
                return z;
            }
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
        private static Inline typeToInline(Inline x, string y, bool useTunnelingStyles)
        {
            if (y == "b")
            {
                return new Bold(x);
            }
            else if (y == "u")
            {
                return new Span(x) { TextDecorations = TextDecorations.Underline };
            }
            else if (y == "i")
            {
                return new Italic(x);
            }
            else if (y == "s")
            {
                return new Span(x) { TextDecorations = TextDecorations.Strikethrough };
            }
            else if (y.StartsWith("channel") || y.StartsWith("session"))
            {
                string channel = stripBeforeType(y);

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
            else if (y.StartsWith("url"))
            {
                string url = stripBeforeType(y);

                var toReturn = new Hyperlink(x) { CommandParameter = url, ToolTip = url };

                if (useTunnelingStyles)
                {
                    toReturn.Style = (Style)Application.Current.FindResource("TunnelingHyperlink");
                }

                return toReturn;
            }
            else if (y.StartsWith("user") || y.StartsWith("icon"))
            {
                string target = stripBeforeType(y);

                return MakeUsernameLink(new CharacterModel { Name = target, Gender = Gender.None }, useTunnelingStyles);
            }
            else if (y == "sub")
            {
                return new Span(x) { BaselineAlignment = BaselineAlignment.Subscript, FontSize = 10 };
            }
            else if (y == "sup")
            {
                return new Span(x) { BaselineAlignment = BaselineAlignment.Top, FontSize = 10 };
            }
            else if (y == "small")
            {
                return new Span(x) { FontSize = 9 };
            }
            else if (y == "big")
            {
                return new Span(x) { FontSize = 16 };
            }
            else
            {
                return new Span(x);
            }
        }

        #endregion
    }
}