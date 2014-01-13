#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Converters.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace Slimcat.Utilities
{
    #region Usings

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

    #endregion

    /// <summary>
    ///     Returns the opposite boolean value
    /// </summary>
    public class OppositeBoolConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object paramater, CultureInfo culture)
        {
            var v = (bool) value;
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
            var v = (bool) value;
            if (v)
                return Application.Current.FindResource("HighlightBrush") as SolidColorBrush;

            return Application.Current.FindResource("BrightBackgroundBrush") as SolidColorBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parsed = (int) value;

            return parsed > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class BbCodeBaseConverter
    {
        private readonly ICharacterManager characterManager;

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

        private static readonly IList<string> SpecialBbCases = new List<string>
            {
                "url",
                "channel",
                "user",
                "icon",
                "color"
            };

        private static readonly string[] ValidStartTerms = {"http://", "https://", "ftp://"};

        #endregion

        #region Constructors

        protected BbCodeBaseConverter(IChatModel chatModel, ICharacterManager characterManager)
        {
            this.characterManager = characterManager;
            ChatModel = chatModel;
        }

        #endregion

        #region Properties

        private IChatModel ChatModel { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Converts an Icharacter into a username 'button'.
        /// </summary>
        internal static Inline MakeUsernameLink(ICharacter target)
        {
            var toReturn =
                new InlineUIContainer
                    {
                        Child = new ContentControl
                            {
                                ContentTemplate = (DataTemplate) Application.Current.FindResource("UsernameTemplate"),
                                Content = target
                            },
                        BaselineAlignment = BaselineAlignment.TextBottom,
                    };

            return toReturn;
        }

        internal static Inline MakeChannelLink(ChannelModel channel)
        {
            var toReturn =
                new InlineUIContainer
                    {
                        Child = new ContentControl
                            {
                                ContentTemplate = (DataTemplate) Application.Current.FindResource("ChannelTemplate"),
                                Content = channel
                            },
                        BaselineAlignment = BaselineAlignment.TextBottom
                    };

            return toReturn;
        }

        /// <summary>
        ///     Converts a string to richly-formatted inline elements.
        /// </summary>
        internal Inline Parse(string text)
        {
            return BbcodeToInline(PreProcessBbCode(text));
        }

        private static bool ContainsUrl(string args)
        {
            var match = args.Split(' ').FirstOrDefault(StartsWithValidTerm);

            // see if it starts with something useful
            if (match == null)
                return false;

            var starter = ValidStartTerms.First(match.StartsWith);
            return args.Trim().Length > starter.Length;
        }

        private static string FindEndType(string x)
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 4)
                return "n";

            var end = x.IndexOf(']', root);
            if (end == -1)
                return "n";

            if (x[root + 1] != '/')
                return FindEndType(x.Substring(end + 1));

            var type = x.Substring(root + 2, end - (root + 2));

            return BbType.Any(type.Equals) ? type : FindEndType(x.Substring(end + 1));
        }

        private static string FindStartType(string x)
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 3)
                return "n";

            var end = x.IndexOf(']', root);
            if (end == -1)
                return "n";

            var type = x.Substring(root + 1, end - root - 1);

            return BbType.Any(type.StartsWith) ? type : FindStartType(x.Substring(end + 1));
        }

        private static string GetUrlDisplay(string args)
        {
            var match = ValidStartTerms.FirstOrDefault(args.StartsWith);
            if (match == null)
                return args;

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

        private static bool HasOpenTag(string x)
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 3)
                return false;

            var end = x.IndexOf(']', root);
            if (end == -1)
                return false;

            if (x[root + 1] == '/')
                HasOpenTag(x.Substring(end));

            var type = x.Substring(root + 1, end - root - 1);

            return BbType.Any(type.StartsWith) || HasOpenTag(x.Substring(end + 1));
        }

        private static string MarkUpUrlWithBbCode(string args)
        {
            var toShow = GetUrlDisplay(args);
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

        private static string PreProcessBbCode(string text)
        {
            if (!ContainsUrl(text))
                return text; // if there's no url in it, we don't have a link to mark up

            var matches = from word in text.Split(' ')
                where StartsWithValidTerm(word)
                select new Tuple<string, string>(word, MarkUpUrlWithBbCode(word));

            return matches.Aggregate(text, (current, toReplace) => current.Replace(toReplace.Item1, toReplace.Item2));
        }

        private Inline TypeToInline(Inline x, string y)
        {
            switch (y)
            {
                case "b":
                    return new Bold(x);
                case "u":
                    return new Span(x) {TextDecorations = TextDecorations.Underline};
                case "i":
                    return new Italic(x);
                case "s":
                    return new Span(x) {TextDecorations = TextDecorations.Strikethrough};
                case "sub":
                    return new Span(x) {BaselineAlignment = BaselineAlignment.Subscript, FontSize = 10};
                case "sup":
                    return new Span(x) {BaselineAlignment = BaselineAlignment.Top, FontSize = 10};
                case "small":
                    return new Span(x) {FontSize = 9};
                case "big":
                    return new Span(x) {FontSize = 16};
            }

            if (y.StartsWith("url"))
            {
                var url = StripBeforeType(y);

                var toReturn = new Hyperlink(x)
                    {
                        CommandParameter = url,
                        ToolTip = url,
                        Style = (Style) Application.Current.FindResource("Hyperlink")
                    };

                return toReturn;
            }

            if (y.StartsWith("user") || y.StartsWith("icon"))
            {
                var target = StripBeforeType(y);
                return MakeUsernameLink(characterManager.Find(target));
            }

            if (y.StartsWith("channel") || y.StartsWith("session"))
            {
                var channel = StripBeforeType(y);

                return MakeChannelLink(ChatModel.FindChannel(channel));
            }

            if (!y.StartsWith("color") || !ApplicationSettings.AllowColors) return new Span(x);
            var colorString = StripBeforeType(y);

            try
            {
                var convertFromString = ColorConverter.ConvertFromString(colorString);
                if (convertFromString != null)
                {
                    var color = (Color) convertFromString;
                    var brush = new SolidColorBrush(color);
                    return new Span(x) {Foreground = brush};
                }
            }
            catch // the color might be invalid, so ignore if it is
            {
                return new Span(x);
            }

            return new Span(x);
        }

        private Inline BbcodeToInline(string x)
        {
            if (!HasOpenTag(x))
                return new Run(x);

            var toReturn = new Span();

            var startType = FindStartType(x);
            var startIndex = x.IndexOf("[" + startType + "]", StringComparison.Ordinal);

            if (startIndex > 0)
                toReturn.Inlines.Add(new Run(x.Substring(0, startIndex)));

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

            var roughString = x.Substring(startIndex);
            roughString = roughString.Remove(0, roughString.IndexOf(']') + 1);

            var endType = FindEndType(roughString);
            var endIndex = roughString.IndexOf("[/" + endType + "]", StringComparison.Ordinal);
            var endLength = ("[/" + endType + "]").Length;

            // for BbCode with arguments, we must do this
            if (SpecialBbCases.Any(bbcase => startType.Equals(bbcase)))
            {
                startType += "=";

                var content = endIndex != -1 ? roughString.Substring(0, endIndex) : roughString;

                startType += content;
            }

            if (startType == "noparse")
            {
                endIndex = roughString.IndexOf("[/noparse]", StringComparison.Ordinal);
                var restofString = roughString.Substring(endIndex + "[/noparse]".Length);
                var skipthis = roughString.Substring(0, endIndex);

                toReturn.Inlines.Add(new Run(skipthis));
                toReturn.Inlines.Add(BbcodeToInline(restofString));
            }
            else if (endType == "n" || endIndex == -1)
                toReturn.Inlines.Add(TypeToInline(new Run(roughString), startType));
            else if (endType != startType)
            {
                var properEnd = "[/" + StripAfterType(startType) + "]";
                if (roughString.Contains(properEnd))
                {
                    var properIndex = roughString.IndexOf(properEnd, StringComparison.Ordinal);
                    var toMarkUp = roughString.Substring(0, properIndex);
                    var restOfString = roughString.Substring(properIndex + properEnd.Length);

                    toReturn.Inlines.Add(TypeToInline(BbcodeToInline(toMarkUp), startType));

                    toReturn.Inlines.Add(BbcodeToInline(restOfString));
                }
                else
                {
                    toReturn.Inlines.Add(
                        TypeToInline(BbcodeToInline(roughString), startType));
                }
            }
            else if (endIndex + endLength == roughString.Length)
            {
                roughString = roughString.Remove(endIndex, endLength);
                toReturn.Inlines.Add(TypeToInline(new Run(roughString), startType));
            }
            else
            {
                var restOfString = roughString.Substring(endIndex + endLength);

                roughString = roughString.Substring(0, endIndex);

                toReturn.Inlines.Add(TypeToInline(new Run(roughString), startType));

                toReturn.Inlines.Add(BbcodeToInline(restOfString));
            }

            return toReturn;
        }

        #endregion
    }

    /// <summary>
    ///     Converts active messages into textblock inlines.
    /// </summary>
    public sealed class BbCodePostConverter : BbCodeBaseConverter, IMultiValueConverter
    {
        #region Constructors

        public BbCodePostConverter(IChatModel chatModel, ICharacterManager characterManager)
            : base(chatModel, characterManager)
        {
        }

        #endregion

        #region Public Methods and Operators

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
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
                {
                    inlines.Add(Parse(text));
                    return inlines;
                }

                // this creates the name link
                var nameLink = MakeUsernameLink(user);
                inlines.Add(nameLink); // name first

                if (text.Length > "/me".Length)
                {
                    if (text.StartsWith("/me"))
                    {
                        // if the post is a /me "command"
                        text = text.Substring("/me".Length);
                        inlines.Insert(0, new Run("*")); // push the name button to the second slot
                        inlines.Add(new Italic(Parse(text)));
                        inlines.Add(new Run("*"));
                        return inlines;
                    }

                    if (text.StartsWith("/post"))
                    {
                        // or a post "command"
                        text = text.Substring("/post ".Length);

                        inlines.Insert(0, Parse(text));
                        inlines.Insert(1, new Run(" ~"));
                        return inlines;
                    }

                    if (text.StartsWith("/warn"))
                    {
                        // or a warn "command"
                        text = text.Substring("/warn ".Length);
                        inlines.Add(new Run(" warns, "));
                        var toAdd = Parse(text);

                        toAdd.Foreground = (Brush) Application.Current.FindResource("HighlightBrush");
                        toAdd.FontWeight = FontWeights.ExtraBold;
                        inlines.Add(toAdd);

                        return inlines;
                    }

                    inlines.Add(new Run(": "));
                    inlines.Add(Parse(text));
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

    /// <summary>
    ///     Converts active messages into flow document inlines.
    /// </summary>
    public sealed class BbFlowConverter : BbCodeBaseConverter, IValueConverter
    {
        #region Constructors

        public BbFlowConverter(IChatModel chatModel, ICharacterManager characterManager)
            : base(chatModel, characterManager)
        {
        }

        #endregion

        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inlines = new List<Inline>();

            if (value == null)
                return null;

            var text = value as string ?? value.ToString(); // this is the beef of the message
            text = HttpUtility.HtmlDecode(text); // translate the HTML characters

            if (text.StartsWith("/me"))
            {
                // if the post is a /me "command"
                text = text.Substring("/me".Length);
                inlines.Add(new Italic(Parse(text)));
                inlines.Add(new Run("*"));
            }
            else if (text.StartsWith("/post"))
            {
                // or a post "command"
                text = text.Substring("/post ".Length);

                inlines.Insert(0, Parse(text));
                inlines.Insert(1, new Run(" ~"));
            }
            else if (text.StartsWith("/warn"))
            {
                // or a warn "command"
                text = text.Substring("/warn ".Length);
                inlines.Add(new Run(" warns, "));

                var toAdd = Parse(text);
                toAdd.Foreground = (Brush) Application.Current.FindResource("HighlightBrush");
                toAdd.FontWeight = FontWeights.ExtraBold;

                inlines.Add(Parse(text));
            }
            else
            {
                inlines.Add(new Run(": "));
                inlines.Add(Parse(text));
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
    ///     Converts history messages into document inlines.
    /// </summary>
    public sealed class BbCodeConverter : BbCodeBaseConverter, IValueConverter
    {
        #region Constructors

        public BbCodeConverter(IChatModel chatModel, ICharacterManager characterManager)
            : base(chatModel, characterManager)
        {
        }

        #endregion

        #region Explicit Interface Methods

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var text = value as string ?? value.ToString();

            text = HttpUtility.HtmlDecode(text);

            IList<Inline> toReturn = new List<Inline>();
            toReturn.Add(Parse(text));

            return toReturn;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///     Converts gender string into gender color.
    /// </summary>
    public sealed class GenderColorConverter : IMultiValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var gender = (Gender) values[0];
                var findResource = Application.Current.FindResource("ForegroundColor");
                if (findResource != null)
                {
                    var paleColor = (Color) findResource;
                    Color brightColor;
                    if (values.Length > 1 && (bool) values[1])
                        brightColor = (Color) Application.Current.TryFindResource("ContrastColor");
                    else
                        brightColor = (Color) Application.Current.TryFindResource("HighlightColor");

                    var stops = new List<GradientStop>
                        {
                            new GradientStop(paleColor, 0.0),
                            new GradientStop(paleColor, 0.5),
                            new GradientStop(brightColor, 0.5),
                            new GradientStop(brightColor, 1.0)
                        };

                    switch (gender)
                    {
                        case Gender.HermF:
                            return new LinearGradientBrush(new GradientStopCollection(stops), 0);
                        case Gender.HermM:
                            return new LinearGradientBrush(new GradientStopCollection(stops));

                        case Gender.Cuntboy:
                        case Gender.Shemale:
                            return Application.Current.FindResource("ForegroundBrush");
                        default:
                            return new SolidColorBrush(brightColor);
                    }
                }
            }
            catch
            {
                return new SolidColorBrush();
            }

            return new SolidColorBrush();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///     Converts gender string into a gender image.
    /// </summary>
    public sealed class GenderImageConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var gender = (Gender) value;
                Uri uri;

                switch (gender)
                {
                    case Gender.HermM:
                    case Gender.Cuntboy:
                    case Gender.Male:
                        uri = new Uri("pack://application:,,,/icons/male.png");
                        break;
                    case Gender.HermF:
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
    ///     Converts notify level into strings.
    /// </summary>
    public sealed class NotifyLevelConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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
                if (ApplicationSettings.Volume > 0.0)
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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return null;

            var v = (bool) value;

            return v ? "only for people of interest." : "for everyone.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///     Converts a channel type enum into a channel type image representation.
    /// </summary>
    public sealed class ChannelTypeToImageConverter : IValueConverter
    {
        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var args = (ChannelType) value;
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///     Converts a character's interested level to a nameplate color
    /// </summary>
    public sealed class NameplateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool) value;
            SolidColorBrush brush = null;
            if (v)
                brush = (SolidColorBrush) Application.Current.TryFindResource("ContrastBrush");

            return brush ?? (SolidColorBrush) Application.Current.FindResource("HighlightBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Various conversion methods
    /// </summary>
    public static class HelperConverter
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Like above, but works for dates in the time
        /// </summary>
        public static string DateTimeInFutureToRough(DateTimeOffset futureTime)
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
        ///     Converts a datetimeoffset to a "x h x m x s ago" format
        /// </summary>
        public static string DateTimeToRough(DateTimeOffset original, bool returnSeconds = false, bool appendAgo = true)
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
        ///     Used for shitty shit channel names with spaces
        /// </summary>
        public static string EscapeSpaces(string text)
        {
            return text.Replace(" ", "___");
        }

        /// <summary>
        ///     Turns a datetime to a timestamp (hh:mm)
        /// </summary>
        public static string ToTimeStamp(this DateTimeOffset time)
        {
            var minute = time.Minute;
            var minuteFix = minute.ToString(CultureInfo.InvariantCulture).Insert(0, minute < 10 ? "0" : string.Empty);

            return "[" + time.Hour + ":" + minuteFix + "]";
        }

        /// <summary>
        ///     Converts a POSIX/UNIX timecode to a datetime
        /// </summary>
        public static DateTimeOffset UnixTimeToDateTime(long time)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return new DateTimeOffset(epoch.AddSeconds(time));
        }

        #endregion
    }
}