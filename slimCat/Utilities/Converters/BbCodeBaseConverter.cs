#region Copyright

// <copyright file="BbCodeBaseConverter.cs">
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
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Libraries;
    using Models;
    using Services;

    #endregion

    /// <summary>
    ///     The common logic for parsing bbcode.
    /// </summary>
    public abstract class BbCodeBaseConverter
    {
        private readonly ICharacterManager characters;
        internal readonly IThemeLocator Locator;

        #region Properties

        internal IChatModel ChatModel { get; set; }

        #endregion

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
            {"eicon", BbCodeType.EIcon},
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

        protected BbCodeBaseConverter(IChatState chatState, IThemeLocator locator)
        {
            characters = chatState.CharacterManager;
            Locator = locator;
            ChatModel = chatState.ChatModel;
        }

        protected BbCodeBaseConverter()
        {
        }

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

        internal Inline MakeEIcon(string target)
        {
            return MakeInlineContainer(target, "EIconTemplate");
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
                {BbCodeType.EIcon, MakeEIcon},
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
                var user = MakeUsernameLink(characters.Find(arg.Children.First().InnerText));
                arg.Children.Clear();
                return user;
            }

            return !string.IsNullOrEmpty(arg.Arguments)
                ? MakeUsernameLink(characters.Find(arg.Arguments))
                : MakeNormalText(arg);
        }

        private Inline MakeIcon(ParsedChunk arg)
        {
            if (!ApplicationSettings.AllowIcons) return MakeUser(arg);

            if (arg.Children != null && arg.Children.Any())
            {
                var characterName = arg.Children.First().InnerText;

                var character = characters.Find(characterName);
                var icon = MakeIcon(character);

                arg.Children.Clear();
                return icon;
            }

            return !string.IsNullOrEmpty(arg.Arguments)
                ? MakeIcon(characters.Find(arg.Arguments))
                : MakeNormalText(arg);
        }

        private Inline MakeEIcon(ParsedChunk arg)
        {
            if (!ApplicationSettings.AllowIcons) return MakeUser(arg);

            if (arg.Children != null && arg.Children.Any())
            {
                var target = arg.Children.First().InnerText;
                var icon = MakeEIcon(target);

                arg.Children.Clear();
                return icon;
            }

            return !string.IsNullOrEmpty(arg.Arguments)
                ? MakeIcon(characters.Find(arg.Arguments))
                : MakeNormalText(arg);
        }

        private Inline MakeSuperscript(ParsedChunk arg)
        {
            var small = MakeSmall(arg);
            small.BaselineAlignment = BaselineAlignment.TextTop;
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
            toReturn.FontSize = ApplicationSettings.FontSize * 0.75;
            return toReturn;
        }

        private Inline MakeBig(ParsedChunk arg)
        {
            var toReturn = new Span(WrapInRun(arg.InnerText));
            toReturn.FontSize = ApplicationSettings.FontSize * 1.5;
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
            return MakeBlockWithAlignment(arg, TextAlignment.Left,
                new Thickness(ApplicationSettings.AllowIndent ? 15 : 0, 0, 0, 0));
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
}