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
using System.Windows.Media;

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
            if (values.GetLength(0) == 2) // avoids null reference
            {
                #region Init
                inlines.Clear(); // simple insurance that there's no junk

                string text = (string)values[0]; // this is the beef of the message
                text = HttpUtility.HtmlDecode(text); // translate the HTML characters

                // This creates the click-able name 'button'
                InlineUIContainer nameButton =
                    new InlineUIContainer(
                        new ContentControl
                        {
                            Margin = new Thickness(0), // no like-y margin
                            ContentTemplate = (DataTemplate)Application.Current.FindResource("UsernamesTemplate"),
                            Content = values[1],
                        })
                        { BaselineAlignment = BaselineAlignment.TextBottom, };

                inlines.Add(nameButton); // name first
                #endregion

                #region BBCode Parsing
                if (text.Length > 3)
                {
                    if (text.StartsWith("/me")) // if the post is a /me "command"
                    {
                        text = text.Substring(3);
                        inlines.Insert(0, new Run("*")); // push the name button to the second slot
                        inlines.Add(new Italic(HelperConverter.ParseBBCode(text)));
                        inlines.Add(new Run("*"));
                        return inlines;
                    }

                    else if (text.StartsWith("/post"))
                    {
                        text = text.Substring(5);

                        inlines.Insert(0, HelperConverter.ParseBBCode(text));
                        inlines.Insert(1, new Run(" ~"));
                        return inlines;
                    }

                    // list of valid BBCode strings

                    inlines.Add(new Run(": "));
                    inlines.Add(HelperConverter.ParseBBCode(text));
                    return inlines;
                }
                #endregion

                #region Fallback addition
                inlines.Add(new Run(": "));
                inlines.Add(new Run(text));
                #endregion
            }

            else
            {
                inlines.Clear();

                string text = (string)values[0];
                text = HttpUtility.HtmlDecode(text);

                inlines.Add(HelperConverter.ParseBBCode(text));

                return inlines;
            }
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
            string text = value as string;

            if (text == null)
                return null;

            text = HttpUtility.UrlDecode(text);

            /*
            IList<Inline> toReturn = new List<Inline>();
            toReturn.Add(HelperConverter.ParseBBCode(text));
            */

            return new List<Inline>() { new Run(text) };
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
        static IList<string> BBType = new List<string> { "b", "s", "u", "i", "url", "color", "channel", "session", "user", "noparse", "icon" };
        static IList<string> SpecialBBCases = new List<string> { "url", "channel", "user", "session"  };
        // determines if a string has a (valid) opening BBCode tag
        static Func<string, bool> hasOpenTag = x =>
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

        };

        // determines if a string has a (valid) closing BBCode tag
        static Func<string, bool> hasCloseTag = x =>
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
        };

        // returns the type of a BBCode mark-up
        static Func<string, string> findStartType = x =>
        {
            var root = x.IndexOf('[');
            if (root == -1 || x.Length < 3) return "n";

            var end = x.IndexOf(']', root);
            if (end == -1) return "n";

            var type = x.Substring(root + 1, end - root - 1);

            if (BBType.Any(bbtype => type.StartsWith(bbtype)))
                return type;
            else return findStartType(x.Substring(end + 1));
        };

        static Func<string, string> findEndType = x =>
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
        };

        static Func<string, string, string> removeFirstEndType = (x, type) =>
        {
            var endtype = "[/" + type + "]";
            int firstOccur = x.IndexOf(endtype);

            string interestedin = x.Substring(0, firstOccur);
            string rest = x.Substring(firstOccur + endtype.Length);

            return interestedin + rest;
        };

        static Func<string, string> stripBeforeType = z =>
        {
            if (z.Contains('='))
            {
                var type = z.Substring(0, z.IndexOf('='));
                return z.Substring(z.IndexOf('=') + 1, (z.Length - (type.Length + 1)));
            }
            else
                return z;
        };

        static Func<string, string> stripAfterType = z =>
        {
            if (z.Contains('='))
            {
                return z.Substring(0, z.IndexOf('='));
            }
            else
                return z;
        };

        static Func<Inline, string, Inline> typeToInline = (x, y) =>
        {
            if (y == "b")
                return new Bold(x);
            else if (y == "u")
                return new Span(x) { TextDecorations = TextDecorations.Underline };
            else if (y == "i")
                return new Italic(x);
            else if (y == "s")
                return new Span(x) { TextDecorations = TextDecorations.Strikethrough };
            else if (y.StartsWith("channel"))
            {
                var channel = stripBeforeType(y);

                return new InlineUIContainer(
                    new Button()
                    {
                        Content = x,
                        Style = (Style)Application.Current.FindResource("ChannelMarkupButton"),
                        CommandParameter = channel
                    })
                    { BaselineAlignment = BaselineAlignment.TextBottom };
            }
            else if (y.StartsWith("url") && !y.Equals("url"))
            {
                var url = stripBeforeType(y);

                return new Hyperlink(x)
                {
                    CommandParameter = url,
                    Style = (Style)Application.Current.FindResource("TunnelingHyperlink"),
                    ToolTip = url
                };
            }
            else
                return new Span(x);
        };

        static Func<string, Inline> bbcodeToInline = x =>
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

                string roughString = x.Substring(startIndex);
                roughString = roughString.Remove(0, roughString.IndexOf(']') + 1);

                var endType = findEndType(roughString);
                var endIndex = roughString.IndexOf("[/" + endType + "]");


                if (SpecialBBCases.Any(bbcase => startType.Equals(bbcase)))
                {
                    startType += "=";;

                    var content = roughString.Substring(0, endIndex);

                    startType += content;
                }

                if (startType == "noparse")
                {
                    endIndex = roughString.IndexOf("[/noparse]");
                    var restofString = roughString.Substring(endIndex + "[/noparse]".Length);
                    var skipthis = roughString.Substring(0, endIndex);

                    toReturn.Inlines.Add(new Run(skipthis));
                    toReturn.Inlines.Add(bbcodeToInline(restofString));
                }

                else if (endType == "n" || endIndex == -1)
                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType));

                else if (endType != startType)
                {
                    var properEnd = "[/" + stripAfterType(startType) + "]";
                    if (roughString.Contains(properEnd))
                    {
                        var properIndex = roughString.IndexOf(properEnd);
                        var toMarkUp = roughString.Substring(0, properIndex);
                        var restOfString = roughString.Substring(properIndex + properEnd.Length);

                        toReturn.Inlines
                            .Add(typeToInline(bbcodeToInline(toMarkUp), startType));

                        toReturn.Inlines
                            .Add(bbcodeToInline(restOfString));
                    }
                    else
                        toReturn.Inlines
                            .Add(typeToInline(bbcodeToInline(roughString), startType));
                }

                else if (endIndex + 4 == roughString.Length)
                {
                    roughString = roughString.Remove(endIndex, 4);
                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType));
                }

                else
                {
                    var restOfString = roughString.Substring(endIndex + 4);

                    roughString = roughString.Substring(0, endIndex);

                    toReturn.Inlines.Add(typeToInline(new Run(roughString), startType));

                    toReturn.Inlines.Add(bbcodeToInline(restOfString));
                }

                return toReturn;
            }
        };
        #endregion

        #region url functions
        static Func<string, bool> containsUrl = args => args.Contains("http://") || args.Contains("https://");
        static Func<string, string> getUrlDispay = args =>
        {
            if (containsUrl(args))
            {
                string urlDisplay;
                if (args.Contains("http://"))
                    urlDisplay = args.Substring("http://".Length); // start past the http://
                else
                    urlDisplay = args.Substring("https://".Length); // start past the https://

                int firstPeriod = urlDisplay.IndexOf('.'); // find our first period (again)
                if (urlDisplay.LastIndexOf('.') == firstPeriod)
                    // our url is in the form something.domain (such as imgur)
                    if (urlDisplay.Contains('/'))
                        urlDisplay = urlDisplay.Substring(0, urlDisplay.IndexOf('/'));
                    else
                        return urlDisplay;

                else if (urlDisplay.Substring(firstPeriod).Contains('/'))
                // if there is a slash after our first period AND we have more than one period before the slash,
                // then our url is in the form www.something.domain/something
                // and we will show something.domain
                {
                    int ending = urlDisplay.Substring(firstPeriod).IndexOf('/');
                    urlDisplay = urlDisplay.Substring((firstPeriod + 1), ending - 1);
                }
                else
                    // if there is more than one period and we do not have a slash in our url, it must be in the form
                    // www.something.domain and there might be a sub domain or an extension domain after it.
                    // we will show something.domain and whatever is after the domain
                    urlDisplay = urlDisplay.Substring(firstPeriod + 1);

                return urlDisplay;
            }
            else
                return args;
        };
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

        public static Inline ParseBBCode(string text)
        {
            return bbcodeToInline(PreProcessBBCode(text));
        }

        /// <summary>
        /// Right now, all this does is warp a url tag around links.
        /// </summary>
        public static string PreProcessBBCode(string text)
        {
            if (containsUrl(text))
            {
                #region Find start and end of url
                int start = text.IndexOf("http://");
                if (start == -1) start = text.IndexOf("https://");

                int end = text.IndexOf(text.Skip(start).FirstOrDefault(Char.IsWhiteSpace), start);

                if (end == -1) // if the string doesn't contain a space, assume the entire thing is part of the url
                    end = text.Length;
                end -= start;

                string fullurl = text.Substring(start, end); // this should be our entire url
                string final = null;
                #endregion


                #region mark it up
                if (start == 0 || Char.IsWhiteSpace(text[start - 1]))
                {
                    string toShow = getUrlDispay(fullurl);
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

            }
            else
                return text; // if there's no url in it, we don't have a link to mark up
        }
    }
}
