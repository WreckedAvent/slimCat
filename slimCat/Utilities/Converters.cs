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

namespace slimCat.Utilities
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
        NoParse
    }

    public abstract class OneWayConverter : IValueConverter
    {
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
            return !v;
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

    public class GreaterThanZeroConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parsed = (int) value;

            return parsed > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    public abstract class BbCodeBaseConverter
    {
        internal readonly IThemeLocator Locator;
        private readonly ICharacterManager characterManager;

        #region Static Fields

        private static readonly string[] ValidStartTerms = {"http://", "https://", "ftp://"};

        private static readonly IDictionary<string, BbCodeType> Types = new Dictionary<string, BbCodeType>
            {
                {"big", BbCodeType.Big},
                {"b", BbCodeType.Bold},
                {"i", BbCodeType.Italic},
                {"user", BbCodeType.User},
                {"url", BbCodeType.Url},
                {"u", BbCodeType.Underline},
                {"icon", BbCodeType.User},
                {"sup", BbCodeType.Superscript},
                {"sub", BbCodeType.Subscript},
                {"small", BbCodeType.Small},
                {"session", BbCodeType.Session},
                {"s", BbCodeType.Strikethrough},
                {"channel", BbCodeType.Channel},
                {"color", BbCodeType.Color},
                {"noparse", BbCodeType.NoParse}
            };

        #endregion

        #region Constructors

        protected BbCodeBaseConverter(IChatModel chatModel, ICharacterManager characterManager, IThemeLocator locator)
        {
            this.characterManager = characterManager;
            Locator = locator;
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
        internal Inline MakeUsernameLink(ICharacter target)
        {
            var toReturn =
                new InlineUIContainer
                    {
                        Child = new ContentControl
                            {
                                ContentTemplate = Locator.Find<DataTemplate>("UsernameTemplate"),
                                Content = target
                            },
                        BaselineAlignment = BaselineAlignment.TextBottom,
                    };

            return toReturn;
        }

        internal Inline MakeChannelLink(ChannelModel channel)
        {
            var toReturn =
                new InlineUIContainer
                    {
                        Child = new ContentControl
                            {
                                ContentTemplate = Locator.Find<DataTemplate>("ChannelTemplate"),
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
            return AsInline(PreProcessBbCode(text));
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

        private static string MarkUpUrlWithBbCode(string args)
        {
            var toShow = GetUrlDisplay(args);
            return "[url=" + args + "]" + toShow + "[/url]"; // mark the bitch up
        }

        private static bool StartsWithValidTerm(string text)
        {
            return ValidStartTerms.Any(text.StartsWith);
        }

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

            var matches = valid.Select(x => new Tuple<string, string>(x, MarkUpUrlWithBbCode(x))).Distinct();

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
                    {BbCodeType.NoParse, MakeNormalText}
                };

            var converter = converters[chunk.Type];
            var toReturn = converter(chunk);

            var span = toReturn as Span;
            if (chunk.Children != null && chunk.Children.Any() && span != null)
                span.Inlines.AddRange(chunk.Children.Select(ToInline));

            return toReturn;
        }

        private Inline MakeUser(ParsedChunk arg)
        {
            var user = MakeUsernameLink(characterManager.Find(arg.Children.First().InnerText));
            arg.Children.Clear();
            return user;
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
            return new Span(WrapInRun(arg.InnerText)) {FontSize = 9};
        }

        private Inline MakeBig(ParsedChunk arg)
        {
            return new Span(WrapInRun(arg.InnerText)) {FontSize = 16};
        }

        private Inline MakeChannel(ParsedChunk arg)
        {
            var channel = MakeChannelLink(ChatModel.FindChannel(arg.Children.First().InnerText));
            arg.Children.Clear();
            return channel;
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
            var url = arg.Arguments;
            var display = arg.Children != null
                ? arg.Children.First().InnerText
                : GetUrlDisplay(arg.Arguments);

            if (url == null)
            {
                url = arg.InnerText ?? string.Empty;
                display = arg.Children != null
                    ? GetUrlDisplay(arg.Children.First().InnerText)
                    : string.Empty;
            }

            if (arg.Children != null)
                arg.Children.Clear();

            return new Hyperlink(WrapInRun(display))
                {
                    CommandParameter = url,
                    ToolTip = url,
                    Style = Locator.FindStyle("Hyperlink")
                };
        }

        private Inline MakeSession(ParsedChunk arg)
        {
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

            #region handle unbalanced tags

            var unbalancedTags =
                (from t in tags
                    where t.Type != BbCodeType.None
                    group t by t.Type
                    into g
                    select new {Type = g.Key, Tags = g})
                    .Where(x => x.Tags.Count()%2 == 1);

            foreach (var tagGroup in unbalancedTags.ToList())
                tagGroup.Tags.First().Type = BbCodeType.None;

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

                    // check if we're the closing for the last open tag
                    if (lastOpen.Type == tag.Type
                        && tag.IsClosing)
                    {
                        lastOpen.ClosingTag = tag;
                        openTags.Pop();

                        #region handle noparse

                        if (lastOpen.Type == BbCodeType.NoParse)
                        {
                            lastOpen.Children = lastOpen.Children ?? new List<BbTag>();
                            lastOpen.Children.Add(new BbTag
                                {
                                    Type = BbCodeType.None,
                                    End = tag.Start,
                                    Start = lastOpen.End
                                });
                        }

                        #endregion
                    }
                    else
                    {
                        if (lastOpen.Type != BbCodeType.NoParse)
                        {
                            // if not, we have to be a child of it
                            lastOpen.Children = lastOpen.Children ?? new List<BbTag>();

                            lastOpen.Children.Add(tag);
                        }

                        addToQueue = false;
                    }
                }

                #endregion

                // we don't need to continue processing closing tags
                if (tag.IsClosing) continue;

                // tell the system we're in the context of this tag now
                if (tag.Type != BbCodeType.None) // text content can't have children
                    openTags.Push(tag);

                // if we're added as a child to another tag, don't process independently of parent
                if (addToQueue) processedQueue.Enqueue(tag);
            }

            // if in the process of removing improper tags we end up with no bbcode,
            // return original
            if (processedQueue.All(x => x.Type == BbCodeType.None))
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
            var last = tag.ClosingTag != null ? tag.ClosingTag.End : tag.End;
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
                {
                    end = start + 2;
                }

                var toReturn = new BbTag
                    {
                        Type = BbCodeType.None,
                        Start = start,
                        End = end - 1 
                    };

                var rewindTo = Last != null ? Last.End : 0;
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

                    var start = Last != null ? Last.End : 0;
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
                        Start = lastStart-1,
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

                        toAdd.Foreground = Locator.Find<Brush>("HighlightBrush");
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

        public BbFlowConverter(IChatModel chatModel, ICharacterManager characterManager, IThemeLocator locator)
            : base(chatModel, characterManager, locator)
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
                toAdd.Foreground = Locator.Find<Brush>("HighlightBrush");
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

        public BbCodeConverter(IChatModel chatModel, ICharacterManager characterManager, IThemeLocator locator)
            : base(chatModel, characterManager, locator)
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

    public abstract class GenderColorConverterBase : OneWayMultiConverter
    {
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

        protected SolidColorBrush GetBrush(Gender? gender)
        {
            return (SolidColorBrush) TryGet(GetGenderName(gender), true);
        }

        protected Color GetColor(Gender? gender)
        {
            return (Color) TryGet(GetGenderName(gender), false);
        }

        protected static Object TryGet(string name, bool isBrush)
        {
            var toReturn = Application.Current.TryFindResource(name + (isBrush ? "Brush" : "Color"));

            if (isBrush)
                return toReturn as SolidColorBrush ?? Application.Current.FindResource("HighlightBrush");

            var color = toReturn as Color?;
            return color.HasValue
                ? color.Value
                : Application.Current.FindResource("HighlightColor");
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
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var gender = values[0] as Gender?;
            var isInteresting = values[1] as bool?;

            Color baseColor;
            var brightColor = (Color) TryGet("Foreground", false);

            if (isInteresting.HasValue && isInteresting.Value)
                baseColor = (Color) TryGet("Contrast", false);
            else
                baseColor = GetColor(gender);

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
    ///     Converts notify level into strings.
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
    }

    /// <summary>
    ///     Converts Interested-only data into strings
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
    ///     Converts a character's interested level to a nameplate color
    /// </summary>
    public sealed class NameplateColorConverter : GenderColorConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var gender = values[0] as Gender?;
            var interesting = values[1] as bool?;

            if (interesting.HasValue && interesting.Value)
                return TryGet("Contrast", true);

            return GetBrush(gender);
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