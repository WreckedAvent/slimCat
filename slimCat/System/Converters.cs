using Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace System
{
    /// <summary>
    /// Returns the opposite boolean value
    /// </summary>
    public class OppositeBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object paramater, System.Globalization.CultureInfo culture)
        {
            bool v = (bool)value;
            return !v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// If true, return a bright color
    /// </summary>
    public class BoolColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, Globalization.CultureInfo culture)
        {
            bool v = (bool)value;
            if (v)
                return Application.Current.FindResource("HighlightBrush") as SolidColorBrush;
            return Application.Current.FindResource("BrightBackgroundBrush") as SolidColorBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This is a primary converter which pulls some WPFy magic to convert BBCode
    /// </summary>
    public sealed class BBCodePostConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
            object parameter, CultureInfo culture)
        {
            var inlines = new List<Inline>();
            if (values.Length == 3 
                && values[0] is string 
                && values[1] is ICharacter 
                && values[2] is MessageType) // avoids null reference
            {
                #region Init
                inlines.Clear(); // simple insurance that there's no junk

                string text = (string)values[0]; // this is the beef of the message
                text = HttpUtility.HtmlDecode(text); // translate the HTML characters
                var userName = ((ICharacter)values[1]).Name; // this is our poster's name
                MessageType type = (MessageType)values[2]; // what kind of type our message is

                if (type == MessageType.roll)
                {
                    inlines.Add(HelperConverter.ParseBBCode(text, true));
                    return inlines;
                }

                // this creates the name link
                var nameLink = HelperConverter.MakeUsernameLink(userName, true);

                inlines.Add(nameLink); // name first
                #endregion

                #region BBCode Parsing
                if (text.Length > "/me".Length)
                {
                    if (text.StartsWith("/me")) // if the post is a /me "command"
                    {
                        text = text.Substring("/me".Length);
                        inlines.Insert(0, new Run("*")); // push the name button to the second slot
                        inlines.Add(new Italic(HelperConverter.ParseBBCode(text, true)));
                        inlines.Add(new Run("*"));
                        return inlines;
                    }

                    else if (text.StartsWith("/post")) // or a post "command"
                    {
                        text = text.Substring("/post ".Length);

                        inlines.Insert(0, HelperConverter.ParseBBCode(text, true));
                        inlines.Insert(1, new Run(" ~"));
                        return inlines;
                    }

                    else if (text.StartsWith("/warn")) // or a warn "command"
                    {
                        text = text.Substring("/warn ".Length);
                        inlines.Add(new Run(" warns, "));
                        var toAdd = (HelperConverter.ParseBBCode(text, true));

                        toAdd.Foreground = (Brush)Application.Current.FindResource("HighlightBrush");
                        toAdd.FontWeight = FontWeights.ExtraBold;
                        inlines.Add(toAdd);

                        return inlines;
                    }

                    inlines.Add(new Run(": "));
                    inlines.Add(HelperConverter.ParseBBCode(text, true));
                    return inlines;
                }
                #endregion

                #region Fallback addition
                inlines.Add(new Run(": "));
                inlines.Add(new Run(text));
                #endregion
            }

            else if (values.Length == 1
                && values[0] is NotificationModel)
            {
                inlines.Clear();

                if (values[0] is CharacterUpdateModel)
                {
                    var notification = values[0] as CharacterUpdateModel;
                    var userName = notification.TargetCharacter.Name;
                    string text = HttpUtility.HtmlDecode(notification.Arguments.ToString());

                    var nameLink = HelperConverter.MakeUsernameLink(userName, true);

                    inlines.Add(nameLink);
                    inlines.Add(new Run(" "));
                    inlines.Add(HelperConverter.ParseBBCode(text, true));
                }
            }
            else
                inlines.Clear();
            return inlines;
        }

        public object[] ConvertBack(object values, Type[] targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class BBCodeConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            string text = value as string;

            if (text == null)
                return null;

            text = HttpUtility.HtmlDecode(text);

            IList<Inline> toReturn = new List<Inline>();
            toReturn.Add(HelperConverter.ParseBBCode(text, false));

            return toReturn;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Various conversion methods
    /// </summary>
    public static class HelperConverter
    {
        #region BBFunctions
        static IList<string> BBType = new List<string> { "b", "s", "u", "i", "url", "color", "channel", "session", "user", "noparse", "icon", "sub", "sup", "small", "big" };
        static IList<string> SpecialBBCases = new List<string> { "url", "channel", "user", "icon",  };
        
        // determines if a string has a (valid) opening BBCode tag
        static bool hasOpenTag (string x)
        {
            int root = x.IndexOf('[');
            if (root == -1 || x.Length < 3) return false;

            int end = x.IndexOf(']', root);
            if (end == -1) return false;

            if (x[root + 1] == '/') hasOpenTag(x.Substring(end));

            string type = x.Substring(root + 1, end - root - 1);

            if (!BBType.Any(bbtype => type.StartsWith(bbtype)))
                return hasOpenTag(x.Substring(end+1));
            else
                return true;

        }

        // determines if a string has a (valid) closing BBCode tag
        static bool hasCloseTag(string x)
        {
            int root = x.IndexOf("[/");
            if (root == -1 || x.Length < 4 || x[root + 1] != '/') return false;

            int end = x.IndexOf("]", root);
            if (end == -1) return false;

            string type = x.Substring(root + 1, end - root - 1);

            if (!BBType.Any(bbtype => bbtype.Equals(type)))
                return hasCloseTag(x.Substring(end + 1));
            else
                return true;
        }

        // returns the type of a BBCode mark-up
        static string findStartType(string x)
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 3) return "n";

            var end = x.IndexOf(']', root);
            if (end == -1) return "n";

            var type = x.Substring(root + 1, end - root - 1);

            if (BBType.Any(bbtype => type.StartsWith(bbtype)))
                return type;
            else return findStartType(x.Substring(end + 1));
        }

        static string findEndType(string x)
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 4) return "n";

            var end = x.IndexOf(']', root);
            if (end == -1) return "n";

            if (x[root + 1] != '/') return findEndType(x.Substring(end + 1));

            var type = x.Substring(root + 2, end - (root + 2));

            if (BBType.Any(bbtype => type.Equals(bbtype)))
                return type;
            else return findEndType(x.Substring(end + 1));
        }

        static string removeFirstEndType(string x, string type)
        {
            var endtype = "[/" + type + "]";
            int firstOccur = x.IndexOf(endtype);

            string interestedin = x.Substring(0, firstOccur);
            string rest = x.Substring(firstOccur + endtype.Length);

            return interestedin + rest;
        }

        static string stripBeforeType(string z)
        {
            if (z.Contains('='))
            {
                var type = z.Substring(0, z.IndexOf('='));
                return z.Substring(z.IndexOf('=') + 1, (z.Length - (type.Length + 1)));
            }
            else
                return z;
        }

        static string stripAfterType(string z)
        {
            if (z.Contains('='))
            {
                return z.Substring(0, z.IndexOf('='));
            }
            else
                return z;
        }

        /// <summary>
        /// Turns wraps x with an flowdocument inline matching y
        /// </summary>
        static Inline typeToInline(Inline x, string y, bool useTunnelingStyles)
        {
            if (y == "b")
                return new Bold(x);
            else if (y == "u")
                return new Span(x) { TextDecorations = TextDecorations.Underline };
            else if (y == "i")
                return new Italic(x);
            else if (y == "s")
                return new Span(x) { TextDecorations = TextDecorations.Strikethrough };
            else if (y.StartsWith("channel") || y.StartsWith("session"))
            {
                var channel = stripBeforeType(y);

                return new InlineUIContainer(
                    new Button()
                    {
                        Content = x,
                        Style = (Style)Application.Current.FindResource((useTunnelingStyles ? "TunnelingChannelMarkupButton" : "ChannelMarkupButton")),
                        CommandParameter = channel
                    }) { BaselineAlignment = BaselineAlignment.TextBottom };
            }
            else if (y.StartsWith("url"))
            {
                var url = stripBeforeType(y);

                var toReturn = new Hyperlink(x)
                {
                    CommandParameter = url,
                    ToolTip = url
                };

                if (useTunnelingStyles) toReturn.Style = (Style)Application.Current.FindResource("TunnelingHyperlink");

                return toReturn;
            }
            else if (y.StartsWith("user") || y.StartsWith("icon"))
            {
                var target = stripBeforeType(y);

                return MakeUsernameLink(target, useTunnelingStyles);
            }
            else if (y == "sub")
                return new Span(x) { BaselineAlignment = BaselineAlignment.Subscript, FontSize = 10 };
            else if (y == "sup")
                return new Span(x) { BaselineAlignment = BaselineAlignment.Top, FontSize = 10 };
            else if (y == "small")
                return new Span(x) { FontSize = 9 };
            else if (y == "big")
                return new Span(x) { FontSize = 16 };
            else
                return new Span(x);
        }

        /// <summary>
        /// Converts a marked-up BBCode string to a flowdocument inline
        /// </summary>
        /// <param name="x">string to convert</param>
        /// <param name="useTunnelingStyles">if the styles used need to tunnel up the visual tree</param>
        /// <returns></returns>
        static Inline bbcodeToInline(string x, bool useTunnelingStyles)
        {
            if (!hasOpenTag(x))
                return new Run(x);

            else
            {
                Span toReturn = new Span();

                var startType = findStartType(x);
                var startIndex = x.IndexOf("[" + startType + "]");

                if (startIndex > 0)
                    toReturn.Inlines.Add(new Run(x.Substring(0, startIndex)));

                if (startType.StartsWith("session")) // hack-in for session to work
                {
                    var rough = x.Substring(startIndex);
                    var firstBrace = rough.IndexOf(']');
                    var endInd = rough.IndexOf("[/session]");

                    if (firstBrace != -1 || endInd != -1)
                    {
                        var channel = rough.Substring(firstBrace+1, (endInd-firstBrace-1));
                        var title = rough.Substring("[session=".Length, (firstBrace - "[session=".Length));

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

                var endType = findEndType(roughString);
                var endIndex = roughString.IndexOf("[/" + endType + "]");
                var endLength = ("[/" + endType + "]").Length;

                // for BBCode with arguments, we must do this
                if (SpecialBBCases.Any(bbcase => startType.Equals(bbcase)))
                {
                    startType += "=";

                    string content;
                    if (endIndex != -1)
                        content = roughString.Substring(0, endIndex);
                    else
                        content = roughString;

                    startType += content;
                }

                if (startType == "noparse")
                {
                    endIndex = roughString.IndexOf("[/noparse]");
                    var restofString = roughString.Substring(endIndex + "[/noparse]".Length);
                    var skipthis = roughString.Substring(0, endIndex);

                    toReturn.Inlines.Add(new Run(skipthis));
                    toReturn.Inlines.Add(bbcodeToInline(restofString, useTunnelingStyles));
                }

                else if (endType == "n" || endIndex == -1)
                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType, useTunnelingStyles));

                else if (endType != startType)
                {
                    var properEnd = "[/" + stripAfterType(startType) + "]";
                    if (roughString.Contains(properEnd))
                    {

                        var properIndex = roughString.IndexOf(properEnd);
                        var toMarkUp = roughString.Substring(0, properIndex);
                        var restOfString = roughString.Substring(properIndex + properEnd.Length);

                        toReturn.Inlines
                            .Add(typeToInline(bbcodeToInline(toMarkUp, useTunnelingStyles), startType, useTunnelingStyles));

                        toReturn.Inlines
                            .Add(bbcodeToInline(restOfString, useTunnelingStyles));
                    }
                    else
                        toReturn.Inlines
                            .Add(typeToInline(bbcodeToInline(roughString, useTunnelingStyles), startType, useTunnelingStyles));
                }

                else if (endIndex + endLength  == roughString.Length)
                {
                    roughString = roughString.Remove(endIndex, endLength);
                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType, useTunnelingStyles));
                }

                else
                {
                    var restOfString = roughString.Substring(endIndex + endLength);

                    roughString = roughString.Substring(0, endIndex);

                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType, useTunnelingStyles));

                    toReturn.Inlines.Add(bbcodeToInline(restOfString, useTunnelingStyles));
                }

                return toReturn;
            }
        }
        #endregion

        #region url functions
        static string[] validStartTerms = new[] { "http://", "https://", "ftp://" };

        static bool containsUrl(string args)
        {
            var match = args.Split(' ').FirstOrDefault(word => startsWithValidTerm(word));
            // see if it starts with something useful

            if (match == null)
                return false;
            else
            {
                var starter = validStartTerms.First(term => match.StartsWith(term));
                return args.Trim().Length > starter.Length;
            }
        }

        static string getUrlDisplay(string args)
        { // forgot about the wonderful principle of KISS. This works better and is way more simple
            string stripped;
            var match = validStartTerms.FirstOrDefault(term => args.StartsWith(term));
            stripped = args.Substring(match.Length); // remove the starting term

            if (stripped.Contains('/')) // remove anything after the slash
                stripped = stripped.Substring(0, stripped.IndexOf('/'));

            if (stripped.StartsWith("www.")) // remove the www.
                stripped = stripped.Substring("www.".Length);

            return stripped;
        }

        static string markUpUrlWithBBCode(string args)
        {
            string toShow = getUrlDisplay(args);
            return "[url=" + args + "]" + toShow + "[/url]"; // mark the bitch up
        }

        static bool startsWithValidTerm(string text)
        {
            return validStartTerms.Any(term => text.StartsWith(term));
        }
        #endregion

        public static string DateTimeToRough(DateTimeOffset original, bool returnSeconds = false, bool appendAgo = true)
        {
            var temp = new StringBuilder();
            var rough = DateTimeOffset.Now - original;
            int tolerance = (returnSeconds ? 1 : 60);

            if (rough.TotalSeconds < tolerance)
                return "Just now";

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

        public static DateTimeOffset UnixTimeToDateTime(long time)
        {
            System.DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return new DateTimeOffset(epoch.AddSeconds(time));
        }

        public static string ToTimeStamp(this DateTimeOffset time)
        {
            var minute = time.Minute;
            var minuteFix = minute.ToString().Insert(0, (minute < 10 ? "0" : ""));

            return "[" + time.Hour + ":" + minuteFix + "]";
        }

        public static Inline ParseBBCode(string text, bool useTunnelingStyles)
        {
            return bbcodeToInline(PreProcessBBCode(text), useTunnelingStyles);
        }

        /// <summary>
        /// Right now, all this does is warp a url tag around links.
        /// </summary>
        public static string PreProcessBBCode(string text)
        {
            if (containsUrl(text))
            {
                //#region Find start and end of url
                //int start;

                var matches = from word in text.Split(' ') // explode them into an array
                              where startsWithValidTerm(word)
                              select new Tuple<string, string>(word, markUpUrlWithBBCode(word)); // put the match terms into a useful tuple

                var toReturn = text;
                foreach (var toReplace in matches)
                    toReturn = toReturn.Replace(toReplace.Item1, toReplace.Item2); // replace each match

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
                return text; // if there's no url in it, we don't have a link to mark up
        }

        /// <summary>
        /// Used for shitty shit channel names with spaces
        /// </summary>
        public static string EscapeSpaces(string text)
        {
            return text.Replace(" ", "___");
        }

        /// <summary>
        /// Used to make a username 'button'
        /// </summary>
        public static Inline MakeUsernameLink(string target, bool useTunnelingStyles)
        {
            var toReturn = new InlineUIContainer(
            new ContentControl() 
                {
                    ContentTemplate = (DataTemplate)Application.Current.FindResource((useTunnelingStyles ? "TunnelingUsernameTemplate" : "UsernameTemplate")),
                    Content = target 
                })
            { BaselineAlignment = BaselineAlignment.TextBottom };

            return toReturn;
        }
    }
}
