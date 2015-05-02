#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Converters.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Net.Cache;
    using System.Text;
    using System.Web;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Markup;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using Libraries;
    using Models;
    using Services;

    #endregion

    /// <summary>
    ///     Valid and accepted BBCode types.
    /// </summary>
    public enum BbCodeType
    {
        None,
        Bold,
        Underline,
        Italic,
        Session,
        Superscript,
        Subscript,
        Small,
        Big,
        Strikethrough,
        Url,
        Channel,
        User,
        Icon,
        Invalid,
        Color,
        NoParse,
        Indent,
        Collapse,
        Quote,
        HorizontalRule,
        Justify,
        Heading,
        Left,
        Right,
        Center
    }

    /// <summary>
    ///     Represents a converter which only converts to and not back.
    /// </summary>
    public abstract class OneWayConverter : IValueConverter
    {
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Represents a converter which only coverts to and not back.
    /// </summary>
    public abstract class OneWayMultiConverter : IMultiValueConverter
    {
        public abstract object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Returns the opposite boolean value
    /// </summary>
    public class OppositeBoolConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object paramater, CultureInfo culture)
        {
            var v = (bool) value;
            return !v ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    ///     If true, return a bright color
    /// </summary>
    public class BoolColorConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool) value;
            if (v)
                return Application.Current.FindResource("HighlightBrush") as SolidColorBrush;

            return Application.Current.FindResource("BrightBackgroundBrush") as SolidColorBrush;
        }
    }

    public class ImagePathConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo info)
        {
            if (value == null) return null;

            var uri = new Uri(value.ToString(), UriKind.RelativeOrAbsolute);
            var imageBrush = new ImageBrush {ImageSource = new BitmapImage(uri)};
            return imageBrush;
        }
    }

    public class CharacterAvatarConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            var character = (string) value;
            return new BitmapImage(
                new Uri(Constants.UrlConstants.CharacterAvatar + character.ToLower() + ".png",
                    UriKind.Absolute), new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable));
        }
    }

    public class AutoOrStarConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool) value)
                return "*";
            return "auto";
        }
    }

    public class CacheUriForeverConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            var character = (Uri) value;
            var bi = new BitmapImage();
            bi.BeginInit();

            bi.UriSource = character;
            bi.CacheOption = BitmapCacheOption.OnDemand;
            bi.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);
            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

            bi.EndInit();

            return bi;
        }
    }

    public class ImageWidthConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var imageWidthString = value as string;
            try
                { return int.Parse(imageWidthString); }
            catch
                { }
            
            return 150;
        }
    }

    public class NotNullConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class NullConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    ///     If greater than zero, return visible.
    /// </summary>
    public class GreaterThanZeroConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var parsed = System.Convert.ToInt32(value);

                return parsed > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch
            {
                return Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    ///     If string is empty, return visible.
    /// </summary>
    public class NotEmptyConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parsed = (string) value;

            return string.IsNullOrEmpty(parsed)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    /// <summary>
    ///     If string is empty, return invisible.
    /// </summary>
    public class EmptyConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parsed = (string) value;

            return string.IsNullOrEmpty(parsed)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    /// <summary>
    ///     If string is not empty, return true.
    /// </summary>
    public class NotEmptyBoolConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parsed = (string)value;

            return !string.IsNullOrEmpty(parsed);
        }
    }

    public class CommaConverter : OneWayMultiConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // This was changed to ensure that a series of strings do not end with a comma
            var stringCount = 0;
            var toReturn = new StringBuilder();
            string parsed;

            for (int i = 0; i < values.Count(); i++)
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

    public class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }

    /// <summary>
    ///     The common logic for parsing bbcode.
    /// </summary>
    public abstract class BbCodeBaseConverter
    {
        internal readonly IThemeLocator Locator;
        private readonly ICharacterManager characterManager;

        #region Static Fields

        private static readonly string[] ValidStartTerms = {"http://", "https://", "ftp://"};

        public static readonly IDictionary<string, BbCodeType> Types = new Dictionary<string, BbCodeType>
        {
            {"big", BbCodeType.Big},
            {"b", BbCodeType.Bold},
            {"i", BbCodeType.Italic},
            {"user", BbCodeType.User},
            {"url", BbCodeType.Url},
            {"u", BbCodeType.Underline},
            {"icon", BbCodeType.Icon},
            {"sup", BbCodeType.Superscript},
            {"sub", BbCodeType.Subscript},
            {"small", BbCodeType.Small},
            {"session", BbCodeType.Session},
            {"s", BbCodeType.Strikethrough},
            {"channel", BbCodeType.Channel},
            {"color", BbCodeType.Color},
            {"noparse", BbCodeType.NoParse},
            {"collapse", BbCodeType.Collapse},
            {"quote", BbCodeType.Quote},
            {"hr", BbCodeType.HorizontalRule},
            {"indent", BbCodeType.Indent},
            {"justify", BbCodeType.Justify},
            {"heading", BbCodeType.Heading},
            {"left", BbCodeType.Left},
            {"right", BbCodeType.Right},
            {"center", BbCodeType.Center}
        };

        #endregion

        #region Constructors

        protected BbCodeBaseConverter(IChatModel chatModel, ICharacterManager characterManager, IThemeLocator locator)
        {
            this.characterManager = characterManager;
            Locator = locator;
            ChatModel = chatModel;
        }

        protected BbCodeBaseConverter()
        {
        }

        #endregion

        #region Properties

        internal IChatModel ChatModel { get; set; }

        #endregion

        #region Methods

        internal Inline MakeUsernameLink(ICharacter target)
        {
            return MakeInlineContainer(target, "UsernameTemplate");
        }

        internal Inline MakeIcon(ICharacter target)
        {
            return MakeInlineContainer(target, "UserIconTemplate");
        }

        internal Inline MakeChannelLink(ChannelModel channel)
        {
            return MakeInlineContainer(channel, "ChannelTemplate");
        }

        private Inline MakeInlineContainer(object model, string template)
        {
            return
                new InlineUIContainer
                {
                    Child = new ContentControl
                    {
                        ContentTemplate = Locator.Find<DataTemplate>(template),
                        Content = model,
                        Margin = new Thickness(2, 0, 2, 0)
                    },
                    BaselineAlignment = BaselineAlignment.TextBottom
                };
        }

        /// <summary>
        ///     Converts a string to richly-formatted inline elements.
        /// </summary>
        internal Inline Parse(string text)
        {
            return AsInline(PreProcessBbCode(text));
        }

        /// <summary>
        ///     Gets a simplified display Url from a string.
        /// </summary>
        /// <example>
        ///     <code>
        ///         GetUrlDisplay("https://www.google.com"); // returns google.com
        ///     </code>
        /// </example>
        private static string GetUrlDisplay(string args)
        {
            if (args == null) return null;

            var match = ValidStartTerms.FirstOrDefault(args.StartsWith);
            if (match == null) return args;

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

        /// <summary>
        ///     Marks up URL with the corresponding url bbcode and with a simplified link to display.
        /// </summary>
        private static string MarkUpUrlWithBbCode(string args)
        {
            var toShow = GetUrlDisplay(args);
            return "[url=" + args + "]" + toShow + "[/url]";
        }

        /// <summary>
        ///     If a given string starts with text valid for a link.
        /// </summary>
        private static bool StartsWithValidTerm(string text)
        {
            return ValidStartTerms.Any(text.StartsWith);
        }

        /// <summary>
        ///     Auto-marks up links; a user expectation.
        /// </summary>
        private static string PreProcessBbCode(string text)
        {
            var exploded = text.Split(new[] {' ', '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            var valid = new List<string>();

            for (var i = 0; i < exploded.Length; i++)
            {
                var current = exploded[i];

                if (i != 0)
                {
                    var last = exploded[i - 1];
                    if (last.EndsWith("[url=", StringComparison.Ordinal)) continue;
                    if (last.EndsWith("url:")) continue;
                }

                if (StartsWithValidTerm(current)) valid.Add(current);
            }

            var matches =
                valid.Select(x => new Tuple<string, string>(x, MarkUpUrlWithBbCode(x))).Distinct();

            return matches.Aggregate(text, (current, toReplace) => current.Replace(toReplace.Item1, toReplace.Item2));
        }

        private Inline ToInline(ParsedChunk chunk)
        {
            var converters = new Dictionary<BbCodeType, Func<ParsedChunk, Inline>>
            {
                {BbCodeType.Bold, MakeBold},
                {BbCodeType.Italic, MakeItalic},
                {BbCodeType.Underline, MakeUnderline},
                {BbCodeType.Url, MakeUrl},
                {BbCodeType.None, MakeNormalText},
                {BbCodeType.Color, MakeColor},
                {BbCodeType.Strikethrough, MakeStrikeThrough},
                {BbCodeType.Session, MakeSession},
                {BbCodeType.Channel, MakeChannel},
                {BbCodeType.Big, MakeBig},
                {BbCodeType.Small, MakeSmall},
                {BbCodeType.Subscript, MakeSubscript},
                {BbCodeType.Superscript, MakeSuperscript},
                {BbCodeType.User, MakeUser},
                {BbCodeType.NoParse, MakeNormalText},
                {BbCodeType.Icon, MakeIcon},
                {BbCodeType.Collapse, MakeCollapse},
                {BbCodeType.Quote, MakeQuote},
                {BbCodeType.HorizontalRule, MakeHorizontalRule},
                {BbCodeType.Indent, MakeIndentText},
                {BbCodeType.Heading, MakeHeading},
                {BbCodeType.Justify, MakeNormalText},
                {BbCodeType.Right, MakeRightText},
                {BbCodeType.Center, MakeCenterText},
                {BbCodeType.Left, MakeBlockText}
            };

            var converter = converters[chunk.Type];
            Inline toReturn;
            try
            {
                toReturn = converter(chunk);
            }
            catch
            {
                toReturn = MakeNormalText(chunk);
            }

            var span = toReturn as Span;
            if (chunk.Children?.Any() ?? false)
                span?.Inlines.AddRange(chunk.Children.Select(ToInline));

            return toReturn;
        }

        public static IList<ParsedChunk> ParseChunk(string input)
        {
            #region init

            // init
            var toReturn = new List<ParsedChunk>();
            var openTags = new Stack<BbTag>();
            var tags = new Queue<BbTag>();
            var processedQueue = new Queue<BbTag>();

            var finder = new BbFinder(input);

            // find all tags
            while (!finder.HasReachedEnd)
            {
                var next = finder.Next();
                if (next != null)
                    tags.Enqueue(next);
            }


            // return original input if we've no valid bbcode tags
            if (tags.All(x => x.Type == BbCodeType.None))
                return new[] {AsChunk(input)};

            #endregion

            while (tags.Count > 0)
            {
                // get the next tag to process
                var tag = tags.Dequeue();
                var addToQueue = true;

                #region add as child of last tag

                // check if we're in the context of another open tag
                if (openTags.Count > 0)
                {
                    var lastOpen = openTags.Peek();
                    var lastMatching = openTags.FirstOrDefault(x => tag.IsClosing && x.Type == tag.Type);

                    // check if we're closing any previous tags
                    if (lastMatching != null)
                    {
                        lastMatching.ClosingTag = tag;

                        // keep going through our opened tag stack until we find the one 
                        // we're closing
                        do
                        {
                            lastOpen = openTags.Pop();

                            // if we end up with a tag that isn't the one we're closing,
                            // it must not have been closed correctly, e.g
                            // [i] [b] [/i]
                            // we'll treat that '[b]' as text
                            if (lastOpen != lastMatching) lastOpen.Type = BbCodeType.None;
                        } while (lastOpen != lastMatching);
                        

                        #region handle noparse

                        if (lastMatching.Type == BbCodeType.NoParse)
                        {
                            lastMatching.Children = lastMatching.Children ?? new List<BbTag>();
                            lastMatching.Children.Add(new BbTag
                            {
                                Type = BbCodeType.None,
                                End = tag.Start,
                                Start = lastMatching.End
                            });
                        }

                        #endregion
                    }
                    else
                    {
                        if (openTags.All(x => x.Type != BbCodeType.NoParse))
                        {
                            // if not, we have to be a child of it
                            lastOpen.Children = lastOpen.Children ?? new List<BbTag>();

                            lastOpen.Children.Add(tag);
                        }

                        // any matching closing tags would be caught in the if part of this
                        // branch, this is an invalid tag, treat as text
                        if (tag.IsClosing) tag.Type = BbCodeType.None;

                        addToQueue = false;
                    }
                }

                #endregion

                // we don't need to continue processing closing tags
                if (tag.IsClosing) continue;

                // tell the system we're in the context of this tag now
                // though ignore children of 'text' and 'hr'
                if (tag.Type != BbCodeType.None && tag.Type != BbCodeType.HorizontalRule)
                    openTags.Push(tag);

                // if we're added as a child to another tag, don't process independently of parent
                if (addToQueue) processedQueue.Enqueue(tag);
            }

            // these tags haven't been closed, so treat them as invalid
            foreach (var openTag in openTags)
                openTag.Type = BbCodeType.None;
           
            // if we have no bbcode present, just return the text as-is
            if (processedQueue.All(x => x.Type == BbCodeType.None && x.Children == null))
                return new[] {AsChunk(input)};

            toReturn.AddRange(processedQueue.Select(x => FromTag(x, input)));

            return toReturn;
        }

        internal static ParsedChunk AsChunk(string input)
        {
            return new ParsedChunk
            {
                Type = BbCodeType.None,
                Start = 0,
                End = input.Length,
                InnerText = input
            };
        }

        public Inline AsInline(string bbcode)
        {
            var inlines = ParseChunk(bbcode).Select(ToInline).ToList();
            if (inlines.Count == 1) return inlines.First();

            var toReturn = new Span();
            toReturn.Inlines.AddRange(inlines);

            return toReturn;
        }

        internal static ParsedChunk FromTag(BbTag tag, string context)
        {
            var last = tag.ClosingTag?.End ?? tag.End;
            var toReturn = new ParsedChunk
            {
                Start = tag.Start,
                End = last,
                Type = tag.Type,
                Arguments = tag.Arguments
            };

            if (tag.Children != null && tag.Children.Any())
                toReturn.Children = tag.Children.Select(x => FromTag(x, context)).ToList();

            if (tag.Type == BbCodeType.None)
                toReturn.InnerText = context.Substring(tag.Start, tag.End - tag.Start);

            return toReturn;
        }

        #region BBCode implementations

        private Inline MakeUser(ParsedChunk arg)
        {
            if (arg.Children != null && arg.Children.Any())
            {
                var user = MakeUsernameLink(characterManager.Find(arg.Children.First().InnerText));
                arg.Children.Clear();
                return user;
            }

            return !string.IsNullOrEmpty(arg.Arguments)
                ? MakeUsernameLink(characterManager.Find(arg.Arguments))
                : MakeNormalText(arg);
        }

        private Inline MakeIcon(ParsedChunk arg)
        {
            if (!ApplicationSettings.AllowIcons) return MakeUser(arg);

            if (arg.Children != null && arg.Children.Any())
            {
                var characterName = arg.Children.First().InnerText;

                var character = characterManager.Find(characterName);
                var icon = MakeIcon(character);

                arg.Children.Clear();
                return icon;
            }

            return !string.IsNullOrEmpty(arg.Arguments)
                ? MakeIcon(characterManager.Find(arg.Arguments))
                : MakeNormalText(arg);
        }

        private Inline MakeSuperscript(ParsedChunk arg)
        {
            var small = MakeSmall(arg);
            small.BaselineAlignment = BaselineAlignment.Superscript;
            return small;
        }

        private Inline MakeSubscript(ParsedChunk arg)
        {
            var small = MakeSmall(arg);
            small.BaselineAlignment = BaselineAlignment.Subscript;
            return small;
        }

        private Inline MakeSmall(ParsedChunk arg)
        {
            var toReturn = new Span(WrapInRun(arg.InnerText));
            toReturn.FontSize *= 0.75;
            return toReturn;
        }

        private Inline MakeBig(ParsedChunk arg)
        {
            var toReturn = new Span(WrapInRun(arg.InnerText));
            toReturn.FontSize *= 1.5;
            return toReturn;
        }

        private Inline MakeChannel(ParsedChunk arg)
        {
            if (arg.Children != null && arg.Children.Any())
            {
                var channel = MakeChannelLink(ChatModel.FindChannel(arg.Children.First().InnerText));
                arg.Children.Clear();
                return channel;
            }

            return !string.IsNullOrEmpty(arg.Arguments)
                ? MakeChannelLink(ChatModel.FindChannel(arg.Arguments))
                : MakeNormalText(arg);
        }

        private Span MakeStrikeThrough(ParsedChunk arg)
        {
            return new Span(WrapInRun(arg.InnerText)) {TextDecorations = TextDecorations.Strikethrough};
        }

        private static Span MakeNormalText(ParsedChunk arg)
        {
            return new Span(WrapInRun(arg.InnerText));
        }

        private static Run WrapInRun(string text)
        {
            return new Run(text);
        }

        private Span MakeUrl(ParsedChunk arg)
        {
            if (arg.Arguments == null && arg.Children == null) return MakeNormalText(arg);

            var url = arg.Arguments;
            var display = arg.Children != null
                ? arg.Children.First().InnerText
                : GetUrlDisplay(arg.Arguments);

            if (url == null)
            {
                url = arg.InnerText;
                display = arg.Children != null
                    ? GetUrlDisplay(arg.Children.First().InnerText)
                    : string.Empty;
            }

            if (url == null && arg.Children != null)
            {
                url = arg.Children.First().InnerText;
            }

            arg.Children?.Clear();

            var contextMenu = new ContextMenu();
            var menuItemCopyLink = new MenuItem
            {
                CommandParameter = url,
                Style = Locator.FindStyle("MenuItemCopy")
            };
            contextMenu.Items.Add(menuItemCopyLink);

            return new Hyperlink(WrapInRun(display))
            {
                CommandParameter = url,
                ToolTip = url,
                ContextMenu = contextMenu,
                Style = Locator.FindStyle("Hyperlink")
            };
        }

        private Inline MakeSession(ParsedChunk arg)
        {
            if (arg.Children == null || !arg.Children.Any() || string.IsNullOrEmpty(arg.Arguments))
                return MakeNormalText(arg);

            var channel = MakeChannelLink(ChatModel.FindChannel(arg.Children.First().InnerText, arg.Arguments));
            arg.Children.Clear();
            return channel;
        }

        private static Span MakeUnderline(ParsedChunk arg)
        {
            return new Underline(WrapInRun(arg.InnerText));
        }

        private static Span MakeBold(ParsedChunk arg)
        {
            return new Bold(WrapInRun(arg.InnerText));
        }

        private static Span MakeItalic(ParsedChunk arg)
        {
            return new Italic(WrapInRun(arg.InnerText));
        }

        private Span MakeColor(ParsedChunk arg)
        {
            var colorString = arg.Arguments;

            if (!ApplicationSettings.AllowColors || colorString == null)
                return MakeNormalText(arg);

            try
            {
                var brush = new BrushConverter().ConvertFromString(colorString) as SolidColorBrush;

                return brush == null
                    ? new Span()
                    : new Span {Foreground = brush};
            }

            catch (FormatException)
            {
            }

            return new Span();
        }

        private Inline MakeCollapse(ParsedChunk arg)
        {
            var title = arg.Arguments;

            var container = new InlineUIContainer();
            var panel = new StackPanel();

            var expander = new Expander
            {
                Header = title,
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            };

            var text = new TextBlock
            {
                Foreground = Locator.Find<SolidColorBrush>("ForegroundBrush"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(25, 0, 0, 0)
            };
            TextBlockHelper.SetInlineList(text, arg.Children.Select(ToInline).ToList());

            expander.Content = text;
            panel.Children.Add(expander);
            panel.Children.Add(new Line
            {
                Stretch = Stretch.Fill,
                X2 = 1,
                Stroke = new SolidColorBrush(Colors.Transparent)
            });
            container.Child = panel;

            arg.Children.Clear();
            return container;
        }

        private Inline MakeQuote(ParsedChunk arg)
        {
            var container = new InlineUIContainer();

            var text = new TextBlock
            {
                Foreground = Locator.Find<SolidColorBrush>("ForegroundBrush"),
                Opacity = 0.8,
                Margin = new Thickness(50, 5, 0, 5),
                TextWrapping = TextWrapping.Wrap
            };
            TextBlockHelper.SetInlineList(text, arg.Children.Select(ToInline).ToList());

            container.Child = text;
            arg.Children.Clear();
            return container;
        }

        private Inline MakeHorizontalRule(ParsedChunk arg)
        {
            return new InlineUIContainer(new Line
            {
                Stretch = Stretch.Fill,
                X2 = 1,
                Margin = new Thickness(0, 5, 0, 5),
                Stroke = Locator.Find<SolidColorBrush>("HighlightBrush")
            });
        }

        private Span MakeHeading(ParsedChunk arg)
        {
            var toReturn = new Span
            {
                Foreground = Locator.Find<SolidColorBrush>("ContrastBrush")
            };
            toReturn.FontSize *= 2;
            SpanHelper.SetInlineSource(toReturn, arg.Children.Select(ToInline).ToList());

            toReturn.Inlines.Add(new Line
            {
                Stretch = Stretch.Fill,
                X2 = 1,
                Stroke = new SolidColorBrush(Colors.Transparent)
            });

            arg.Children.Clear();
            return toReturn;
        }

        private Inline MakeBlockText(ParsedChunk arg)
        {
            return MakeBlockWithAlignment(arg, TextAlignment.Left, new Thickness(0));
        }

        private Inline MakeIndentText(ParsedChunk arg)
        {
            return MakeBlockWithAlignment(arg, TextAlignment.Left, new Thickness(ApplicationSettings.AllowIndent ? 15 : 0, 0, 0, 0));
        }

        private Inline MakeBlockWithAlignment(ParsedChunk arg, TextAlignment alignment, Thickness thickness)
        {
            if (arg.Children == null || !arg.Children.Any()) return MakeNormalText(arg);

            var container = new InlineUIContainer();
            var panel = new StackPanel();
            var text = new TextBlock
            {
                Foreground = Locator.Find<SolidColorBrush>("ForegroundBrush"),
                Margin = thickness,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = alignment
            };
            TextBlockHelper.SetInlineList(text, arg.Children.Select(ToInline).ToList());

            panel.Children.Add(text);
            panel.Children.Add(new Line
            {
                Stretch = Stretch.Fill,
                X2 = 1,
                Stroke = new SolidColorBrush(Colors.Transparent)
            });

            container.Child = panel;
            arg.Children.Clear();
            return container;
        }

        private Inline MakeRightText(ParsedChunk arg)
        {
            return ApplicationSettings.AllowAlignment
                ? MakeBlockWithAlignment(arg, TextAlignment.Right, new Thickness(0))
                : MakeNormalText(arg);
        }

        private Inline MakeCenterText(ParsedChunk arg)
        {
            var padding = ApplicationSettings.AllowIndent ? 15 : 0;

            return ApplicationSettings.AllowAlignment
                ? MakeBlockWithAlignment(arg, TextAlignment.Center, new Thickness(padding, 0, padding, 0))
                : MakeNormalText(arg);
        }

        #endregion

        public class BbFinder
        {
            private const int NotFound = -1;
            private readonly string input;
            private string arguments;
            private int currentPosition;
            private int lastStart;

            public BbFinder(string input)
            {
                this.input = input;
            }

            public BbTag Last { get; private set; }

            public bool HasReachedEnd { get; private set; }

            private BbTag ReturnAsTextBetween(int start, int end)
            {
                if (end - start <= 1)
                    end = start + 2;

                var toReturn = new BbTag
                {
                    Type = BbCodeType.None,
                    Start = start,
                    End = end - 1
                };

                var rewindTo = Last?.End ?? 0;
                currentPosition = rewindTo;
                Last = toReturn;
                return toReturn;
            }

            public BbTag Next()
            {
                if (HasReachedEnd)
                    return NoResult();

                var openBrace = input.IndexOf('[', currentPosition);
                var closeBrace = input.IndexOf(']', currentPosition);

                currentPosition = closeBrace + 1;
                lastStart = openBrace + 1;

                if (openBrace == NotFound || closeBrace == NotFound)
                {
                    HasReachedEnd = true;

                    var start = Last?.End ?? 0;
                    var end = input.Length;

                    if (end - start > 0)
                    {
                        return new BbTag
                        {
                            Type = BbCodeType.None,
                            Start = start,
                            End = end
                        };
                    }
                    return null;
                }

                if (Last == null && openBrace > 0)
                    return ReturnAsTextBetween(0, lastStart);

                if (Last != null && lastStart - Last.End > 1)
                    return ReturnAsTextBetween(Last.End, lastStart);

                if (closeBrace < openBrace)
                    return ReturnAsTextBetween(closeBrace, openBrace);

                arguments = null;
                var type = input.Substring(openBrace + 1, closeBrace - openBrace - 1);

                var equalsSign = type.IndexOf('=');
                if (equalsSign != NotFound)
                {
                    var typeBeforeEquals = type.Substring(0, equalsSign);

                    arguments = type.Substring(equalsSign + 1, type.Length - equalsSign - 1).Trim();
                    type = typeBeforeEquals.Trim();
                }

                var isEndType = false;

                if (type.Length > 1)
                {
                    isEndType = type[0].Equals('/');
                    type = isEndType ? type.Substring(1) : type;
                }

                var possibleMatch = Types.Keys.FirstOrDefault(x => x.Equals(type, StringComparison.Ordinal));

                if (possibleMatch == null)
                    return NoResult();

                Last = new BbTag
                {
                    Arguments = arguments,
                    End = currentPosition,
                    Start = openBrace,
                    Type = Types[possibleMatch],
                    IsClosing = isEndType
                };

                return Last;
            }

            private BbTag NoResult()
            {
                var toReturn = new BbTag
                {
                    Start = lastStart - 1,
                    End = currentPosition,
                    Type = BbCodeType.None
                };
                Last = toReturn;
                return toReturn;
            }
        }

        public class BbTag
        {
            public BbCodeType Type { get; set; }

            public int Start { get; set; }

            public int End { get; set; }

            public string Arguments { get; set; }

            public bool IsClosing { get; set; }

            public BbTag ClosingTag { get; set; }

            public IList<BbTag> Children { get; set; }

            public BbTag Parent { get; set; }
        }

        public class ParsedChunk
        {
            public int Start { get; set; }

            public int End { get; set; }

            public string InnerText { get; set; }

            public string Arguments { get; set; }

            public BbCodeType Type { get; set; }

            public IList<ParsedChunk> Children { get; set; }
        }

        #endregion
    }

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
                    return Parse(text);

                // this creates the name link
                var nameLink = MakeUsernameLink(user);
                inlines.Add(nameLink); // name first

                if (text[0] == '/')
                {
                    var check = text.Substring(0, text.IndexOf(' ')+1);
                    Func<string, string> nonCommandCommand;

                    if (CommandDefinitions.NonCommandCommands.TryGetValue(check, out nonCommandCommand))
                    {
                        var command = text[1];
                        text = nonCommandCommand(text);

                        if (command == 'm')  // is an emote
                        {
                            inlines.Insert(0, new Run("*")); // push the name button to the second slot
                            inlines[1].FontStyle = FontStyles.Italic;
                            inlines.Add(new Italic(Parse(text)));
                            inlines.Add(new Run("*"));
                        }
                        else if (command == 'w')  // is a warn
                        {
                            var toAdd = Parse(text);
                            toAdd.Foreground = Locator.Find<Brush>("ModeratorBrush");
                            toAdd.FontWeight = FontWeights.Medium;
                            inlines.Add(toAdd);
                        }
                        else if (command == 'p')  // is a post
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

    /// <summary>
    ///     Converts active messages into flow document inlines.
    /// </summary>
    public sealed class BbFlowConverter : BbCodeBaseConverter, IValueConverter
    {
        private readonly IGetPermissions permissions;

        #region Constructors

        public BbFlowConverter(IChatModel chatModel, ICharacterManager characterManager, IThemeLocator locator,
            IGetPermissions permissions)
            : base(chatModel, characterManager, locator)
        {
            this.permissions = permissions;
        }

        public BbFlowConverter()
        { 
        }

        #endregion

        #region Public Methods and Operators

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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
                var check = text.Substring(0, text.IndexOf(' ')+1);
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
                else if (command == 'm')  // is an emote
                    inlines.Add(new Italic(Parse(text)));
                else
                {
                    inlines.Add(new Run(" : "));
                    inlines.Add(Parse(text));
                }

                return inlines;
            }
            inlines.Add(new Run(" : "));
            inlines.Add(Parse(text));
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

        public BbCodeConverter(IChatModel chatModel, ICharacterManager characterManager, IThemeLocator locator)
            : base(chatModel, characterManager, locator)
        {
        }

        public BbCodeConverter()
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
    ///     Contains common logic for turning values into gender colors.
    /// </summary>
    public abstract class GenderColorConverterBase : OneWayConverter
    {
        internal readonly IGetPermissions Permissions;

        private readonly IDictionary<Gender, Gender> genderFallbacks = new Dictionary<Gender, Gender>
        {
            {Gender.Male, Gender.Male},
            {Gender.HermM, Gender.Male},
            {Gender.Cuntboy, Gender.Male},
            {Gender.Female, Gender.Female},
            {Gender.Shemale, Gender.Female},
            {Gender.HermF, Gender.Female},
            {Gender.Transgender, Gender.None},
            {Gender.None, Gender.None}
        };

        private readonly IDictionary<Gender, string> genderNames = new Dictionary<Gender, string>
        {
            {Gender.HermM, "MaleHerm"},
            {Gender.Cuntboy, "Cunt"},
            {Gender.Male, "Male"},
            {Gender.HermF, "Herm"},
            {Gender.Female, "Female"},
            {Gender.Shemale, "Shemale"},
            {Gender.Transgender, "Transgender"},
            {Gender.None, "Highlight"}
        };

        private readonly ICharacterManager manager;

        protected GenderColorConverterBase(IGetPermissions permissions, ICharacterManager manager)
        {
            Permissions = permissions;
            this.manager = manager;
        }

        protected SolidColorBrush GetBrush(ICharacter character)
        {
            if (Permissions != null && Permissions.IsModerator(character.Name))
                return (SolidColorBrush) Application.Current.FindResource("ModeratorBrush");

            if (manager != null && manager.IsOnList(character.Name, ListKind.NotInterested))
                return (SolidColorBrush) Application.Current.FindResource("NotAvailableBrush");

            if (!ApplicationSettings.AllowStatusDiscolor)
                return (SolidColorBrush) TryGet(GetGenderName(character.Gender), true);

            if (character.Status == StatusType.Crown
                || character.Status == StatusType.Online
                || character.Status == StatusType.Looking)
                return (SolidColorBrush) TryGet(GetGenderName(character.Gender), true);

            return (SolidColorBrush) Application.Current.FindResource("NotAvailableBrush");
        }

        protected Color GetColor(ICharacter character)
        {
            if (Permissions != null && Permissions.IsModerator(character.Name))
                return (Color) Application.Current.FindResource("ModeratorColor");

            if (manager != null && manager.IsOnList(character.Name, ListKind.NotInterested))
                return (Color) Application.Current.FindResource("NotAvailableColor");

            if (!ApplicationSettings.AllowStatusDiscolor)
                return (Color) TryGet(GetGenderName(character.Gender), false);

            if (character.Status == StatusType.Crown
                || character.Status == StatusType.Online
                || character.Status == StatusType.Looking)
                return (Color) TryGet(GetGenderName(character.Gender), false);

            return (Color) Application.Current.FindResource("NotAvailableColor");
        }

        protected static object TryGet(string name, bool isBrush)
        {
            var toReturn = Application.Current.TryFindResource(name + (isBrush ? "Brush" : "Color"));

            if (isBrush)
                return toReturn as SolidColorBrush ?? Application.Current.FindResource("HighlightBrush");

            var color = toReturn as Color?;
            return color ?? Application.Current.FindResource("HighlightColor");
        }

        protected string GetGenderName(Gender? gender)
        {
            if (gender == null || ApplicationSettings.GenderColorSettings == GenderColorSettings.None)
                return "Highlight";

            if (ApplicationSettings.GenderColorSettings == GenderColorSettings.Full)
                return genderNames[gender.Value];

            if (ApplicationSettings.GenderColorSettings == GenderColorSettings.GenderOnly)
                return genderNames[genderFallbacks[gender.Value]];

            if (ApplicationSettings.GenderColorSettings == GenderColorSettings.GenderAndHerm
                && (gender == Gender.HermM || gender == Gender.HermF))
                return genderNames[gender.Value];

            return genderNames[genderFallbacks[gender.Value]];
        }
    }

    /// <summary>
    ///     Converts gender string into gender color.
    /// </summary>
    public sealed class GenderColorConverter : GenderColorConverterBase
    {
        public GenderColorConverter(IGetPermissions permissions, ICharacterManager manager)
            : base(permissions, manager)
        {
        }

        public GenderColorConverter()
            : base(null, null)
        {
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ICharacter))
                return Application.Current.FindResource("ForegroundBrush");

            var character = (ICharacter) value;
            var gender = character.Gender;
            var isInteresting = character.IsInteresting;

            Color baseColor;
            var brightColor = (Color) TryGet("Foreground", false);

            if (isInteresting && ApplicationSettings.AllowOfInterestColoring)
                baseColor = (Color) TryGet("Contrast", false);
            else
                baseColor = GetColor(character);

            var stops = new List<GradientStop>
            {
                new GradientStop(baseColor, 0.0),
                new GradientStop(baseColor, 0.5),
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
                    return TryGet("Foreground", true);
                default:
                    return new SolidColorBrush(baseColor);
            }
        }
    }

    /// <summary>
    ///     Converts gender string into a gender image.
    /// </summary>
    public sealed class GenderImageConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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
    }

    /// <summary>
    ///     Converts notification notify level into descriptive strings.
    /// </summary>
    public sealed class NotifyLevelConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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
                if (ApplicationSettings.AllowSound)
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
    }

    /// <summary>
    ///     Converts notification Interested-only settings into descriptive strings.
    /// </summary>
    public sealed class InterestedOnlyBoolConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return null;

            var v = (bool) value;

            return v ? "only for people of interest." : "for everyone.";
        }
    }

    /// <summary>
    ///     Converts a channel type enum into a channel type image representation.
    /// </summary>
    public sealed class ChannelTypeToImageConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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
    }

    /// <summary>
    ///     Converts a character's interested level to a nameplate color.
    /// </summary>
    public sealed class NameplateColorConverter : GenderColorConverterBase
    {
        public NameplateColorConverter(IGetPermissions permissions, ICharacterManager manager)
            : base(permissions, manager)
        {
        }

        public NameplateColorConverter()
            : base(null, null)
        {
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ICharacter))
                return Application.Current.FindResource("ForegroundBrush");

            var character = (ICharacter) value;
            var interesting = character.IsInteresting && ApplicationSettings.AllowOfInterestColoring;

            return interesting
                ? TryGet("Contrast", true)
                : GetBrush(character);
        }
    }

    /// <summary>
    ///     Converts a character's interested level to a nameplate color for a message. Accounts for message being of interest.
    /// </summary>
    public sealed class NameplateMessageColorConverter : GenderColorConverterBase
    {
        public NameplateMessageColorConverter(IGetPermissions permissions, ICharacterManager manager)
            : base(permissions, manager)
        {
        }

        public NameplateMessageColorConverter()
            : base(null, null)
        {
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IMessage))
                return Application.Current.FindResource("ForegroundBrush");

            var message = (IMessage) value;

            return message.Poster.IsInteresting && ApplicationSettings.AllowOfInterestColoring
                ? TryGet("Contrast", true)
                : GetBrush(message.Poster);
        }
    }

    /// <summary>
    ///     Converts a message's of interest / not state to an appropriate foreground
    /// </summary>
    public sealed class MessageInterestedColorConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IMessage))
                return Application.Current.FindResource("BackgroundBrush");

            var message = (IMessage) value;

            return Application.Current.FindResource(message.IsOfInterest ? "HighlightBrush" : "BackgroundBrush");
        }
    }

    public sealed class MessageDelimiterColorConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IMessage))
                return new SolidColorBrush(Colors.Transparent);

            var message = (IMessage) value;

            if (message.IsLastViewed)
                return
                    Application.Current.FindResource(message.Type == MessageType.Ad
                        ? "ContrastBrush"
                        : "BrightBackgroundBrush");

            if (message.IsOfInterest)
                return Application.Current.FindResource("HighlightBrush");

            if (message.Type != MessageType.Normal)
                return Application.Current.FindResource("BrightBackgroundBrush");

            return new SolidColorBrush(Colors.Transparent);
        }
    }

    public sealed class MessageThicknessConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const int top = 0;
            const int left = 0;

            var bottom = 0;
            var right = 0;

            var message = value as IMessage;
            if (message == null) return new Thickness(left, top, right, bottom);

            if (message.IsLastViewed) bottom = 2;

            else if (message.Type == MessageType.Ad) bottom = 1;

            else if (message.IsOfInterest)
            {
                right = 8;
                bottom = 2;
            }

            return new Thickness(left, top, right, bottom);
        }
    }

    public sealed class CharacterStatusOpacityConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is StatusType))
                return 1.0;

            return ((StatusType) value) == StatusType.Offline ? 0.4 : 1;
        }
    }

    public sealed class ForegroundBrushConverter : OneWayConverter
    {
        private readonly IChatModel chatModel;
        private readonly IThemeLocator locator;

        public ForegroundBrushConverter(IChatModel chatModel, IThemeLocator locator)
        {
            this.chatModel = chatModel;
            this.locator = locator;
        }

        public ForegroundBrushConverter()
        { 
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush defaultBrush;
            if (locator != null) defaultBrush = locator.Find<Brush>("ForegroundBrush");
            else defaultBrush = (Brush) Application.Current.FindResource("ForegroundBrush");

            if (value == null || chatModel == null) return defaultBrush;

            var message = value as IMessage; // this is the beef of the message

            if (message == null) return defaultBrush;

            if (chatModel.CurrentCharacter.NameEquals(message.Poster.Name) && locator != null)
                return locator.Find<Brush>("SelfBrush");

            return defaultBrush;
        }
    }

    public class RemoveSomeConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Max(((double) value) - (System.Convert.ToDouble(parameter)), 0);
        }
    }

    public class ReduceRowConverter : OneWayMultiConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Max(((double)values[0] * 0.8) - ((double)values[1]) - (System.Convert.ToDouble(parameter)), 0);
        }
    }

    /// <summary>Represents a chain of <see cref="IValueConverter" />s to be executed in succession.</summary>
    [ContentProperty("Converters")]
    [ContentWrapper(typeof (ValueConverterCollection))]
    public class ConverterChain : OneWayConverter
    {
        private ValueConverterCollection converters;

        public ValueConverterCollection Converters => converters ?? (converters = new ValueConverterCollection());

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Converters.Aggregate(value,
                (current, valueConverter) => valueConverter.Convert(current, targetType, parameter, culture));
        }
    }

    /// <summary>Represents a collection of <see cref="IValueConverter" />s.</summary>
    public sealed class ValueConverterCollection : Collection<IValueConverter>
    {
    }

    /// <summary>
    ///     Various conversion methods.
    /// </summary>
    public static class HelperConverter
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Converts a <see cref="System.DateTimeOffset" /> to a rough time in the future.
        /// </summary>
        /// <returns>A string in the "hours h minutes m seconds s" format.</returns>
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
        ///     Converts a <see cref="System.DateTimeOffset" /> to a rough time in the past.
        /// </summary>
        /// <returns>A string in the "hours h minutes m seconds s ago" format.</returns>
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
        ///     Used to replace channel names with spaces to something more unique.
        /// </summary>
        public static string EscapeSpaces(string text)
        {
            return text.Replace(" ", "___");
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

        /// <summary>
        ///     Converts a POSIX/UNIX timecode to a <see cref="System.DateTimeOffset" />.
        /// </summary>
        public static DateTimeOffset UnixTimeToDateTime(long time)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return new DateTimeOffset(epoch.AddSeconds(time));
        }

        #endregion
    }

    public class PmOrChannelTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate
            SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;

            if (element == null) return null;

            return (ApplicationSettings.ShowAvatars)
                ? element.FindResource("PmChannelTemplate") as DataTemplate
                : element.FindResource("GeneralChannelTemplate") as DataTemplate;
        }
    }
}