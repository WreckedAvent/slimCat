
namespace slimCatTest
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Slimcat.Models;
    using slimCat.Models;
    using Slimcat.Utilities;

    [TestClass]
    public class ParserTest
    {
        private readonly BbFlowConverter converter;

        public ParserTest()
        {
            var manager = Mock.Of<ICharacterManager>();
            var chatModel = Mock.Of<IChatModel>();
            var locator = Mock.Of<IThemeLocator>();

            converter = new BbFlowConverter(chatModel, manager, locator); 
        }

        [TestClass]
        public class AutoUrlMarkupTest : ParserTest
        {
            [TestMethod]
            public void CanMarkUpPlainHttpsUrl()
            {
                const string text = @"check out this link at https://www.google.com !";

                ShouldContainLink(text, "google.com");
            }

            [TestMethod]
            public void CanMarkUpPlainHttpUrl()
            {
                const string text = @"check out this link at http://www.google.com !";

                ShouldContainLink(text, "google.com");
            }

            [TestMethod]
            public void CanMarkUpPlainFtpUrl()
            {
                const string text = @"check out this link at ftp://www.mysite.com";

                ShouldContainLink(text, "mysite.com");
            }

            [TestMethod]
            public void DoesNotMarkUpPlainJunkLink()
            {
                const string text = @"check out this link at snns://www.google.com";

                ShouldNotContainMarkup(text);
            }

            [TestMethod]
            public void CanHandleUrlsDelimitedBySlash()
            {
                const string text =
                    @"https://e621.net/post/show/410627/anus-areola-balls-big_balls-big_butt-big_penis-bre / https://e621.net/post/show/410627/anus-areola-balls-big_balls-big_butt-big_penis-bre (I'm looking currently at these)";

                var hyperlinks = GetAll<Hyperlink>(text);

                Assert.IsTrue(hyperlinks.Count() == 2);
                Assert.IsTrue(hyperlinks.All(x => x.GetText().Equals("e621.net")));
            }
        }

        [TestClass]
        public class BbCodeMarkupTest : ParserTest
        {
            #region Bbcode Smoke Tests
            [TestMethod]
            public void BoldWorks()
            {
                const string textAtEnd = "some well-formed [b]bold text[/b]";
                const string textAtStart = "[b]some well-formed[/b] bold text";
                const string textAtMiddle = "some well-[b]formed bold[/b] text";

                ShouldBeOneOf<Bold>(textAtEnd).AssertFirstTextIs("bold text");
                ShouldBeOneOf<Bold>(textAtMiddle).AssertFirstTextIs("formed bold");
                ShouldBeOneOf<Bold>(textAtStart).AssertFirstTextIs("some well-formed");
            }

            [TestMethod]
            public void ItalicWorks()
            {
                const string text = "some well-formed [i]italic[/i] text";

                ShouldBeOneOf<Italic>(text).AssertFirstTextIs("italic");
            }

            [TestMethod]
            public void UnderlineWorks()
            {
                const string text = "some well-formed [u]underline[/u] text";

                ShouldBeOneOf<Underline>(text).AssertFirstTextIs("underline");
            }

            [TestMethod]
            public void StrikeThroughWorks()
            {
                const string text = "some well-formed [s]strike-through[/s] text";

                var result = ShouldBeOneOf<Span>(text).First(x => x.TextDecorations.Equals(TextDecorations.Strikethrough));

                result.AssertTextIs("strike-through");
            }

            [TestMethod]
            public void UrlWorks()
            {
                const string text = "some well formed [url=https://www.google.com]url[/url] text";

                ShouldBeOneOf<Hyperlink>(text).AssertFirstTextIs("url");
            }

            [TestMethod]
            public void SimpleUrlWorks()
            {
                const string text = "some well-formed [url]https://www.google.com[/url] text";

                ShouldBeOneOf<Hyperlink>(text).AssertFirstTextIs("google.com");
            }

            [TestMethod]
            public void SessionWorks()
            {
                const string text = "some well-formed [session=Love Bar]ADH-SOMENONSENSE[/session] session";

                ShouldBeOneOf<InlineUIContainer>(text);
                // not sure how to grab text out of InlineUIContainer. TODO
            }

            [TestMethod]
            public void ChannelWorks()
            {
                const string text = "some well-formed [channel]Helpdesk[/channel] channel text";

                ShouldBeOneOf<InlineUIContainer>(text);
            }

            [TestMethod]
            public void BigWorks()
            {
                const string text = "some well-formed [big]big[/big] text";

                ShouldBeOneOf<Span>(text).First(x => x.FontSize > 12).AssertTextIs("big");
            }

            [TestMethod]
            public void UserWorks()
            {
                const string text = "some well-formed [user]user[/user] text";

                ShouldBeOneOf<InlineUIContainer>(text);
            }

            [TestMethod]
            public void SmallWorks()
            {
                const string text = "some well-formed [small]small[/small] text";

                ShouldBeOneOf<Span>(text).First(x => x.FontSize < 12).AssertTextIs("small");
            }

            [TestMethod]
            public void SubscriptWorks()
            {
                const string text = "some well-formed [sub]sub[/sub] text";

                ShouldBeOneOf<Span>(text)
                    .First(x => x.BaselineAlignment == BaselineAlignment.Subscript)
                    .AssertTextIs("sub");
            }

            [TestMethod]
            public void SuperWorks()
            {
                const string text = "some well-formed [sup]sup[/sup] text";

                ShouldBeOneOf<Span>(text)
                    .First(x => x.BaselineAlignment == BaselineAlignment.Superscript)
                    .AssertTextIs("sup");
            }

            [TestMethod]
            public void ColorWorks()
            {
                ApplicationSettings.AllowColors = true; // TODO : get this as a dependency
                const string text = "some well-formed [color=green]color[/color] text";

                ShouldBeOneOf<Span>(text)
                    .First(x => x.Foreground.IsColor(Colors.Green))
                    .AssertTextIs("color");
            }

            [TestMethod]
            public void ColorToggleWorks()
            {
                ApplicationSettings.AllowColors = false;
                const string text = "some well-formed [color=green]color[/color] text";

                var result = ShouldBeOneOf<Span>(text).First(x => x.GetText().Equals("color"));
                Assert.IsFalse(result.Foreground.IsColor(Colors.Green));
            }

            [TestMethod]
            public void InvalidColorIsHandledGracefully()
            {
                ApplicationSettings.AllowColors = false;
                const string text = "some well-formed [color]color[/color] text";

                var result = ShouldBeOneOf<Span>(text).First(x => x.GetText().Equals("color"));
                Assert.IsFalse(result.Foreground.IsColor(Colors.Green));
            }
            #endregion

            [TestMethod]
            public void StackedBbCodeWorks()
            {
                const string text = "some stacked [i][b]bbcode[/b][/i] text";

                var result = ShouldBeOneOf<Italic>(text).First().GetFirstChild<Bold>();

                result.AssertTextIs("bbcode");
            }

            [TestMethod]
            public void PlainChildrenInStackedBbCodeWorks()
            {
                const string text = "some stacked [i]bbcode with [b]plain text[/b] at various points[/i] ... text";

                var root = ShouldBeOneOf<Italic>(text).First();

                root.GetFirstChild().AssertTextIs("bbcode with ");
                root.GetFirstChild<Bold>().AssertTextIs("plain text");
                root.GetChildren().Last().AssertTextIs(" at various points");
            }

            [TestMethod]
            public void MultipleStackedBcodeWorks()
            {
                const string text = "some  [b]bbcode [u]with [i]lots[/i] of[/u] [u]stacking[/u][/b] text";

                var root = ShouldBeOneOf<Bold>(text).First();

                root.GetFirstChild().AssertTextIs("bbcode ");

                var second = root.GetFirstChild<Underline>();
                second.AssertTextIs("with ");

                second.GetFirstChild<Italic>().AssertTextIs("lots");
                second.GetChildren().Last().AssertTextIs(" of");
                root.GetChildren<Underline>().Last().AssertTextIs("stacking");
            }

            [TestMethod]
            public void JunkBbCodeIsIgnored()
            {
                const string missingCloseWrong = "start text [b] run away bbcode end text";
                const string totallyWrong =
                    "this is some [nonsense]nonsense[/nonsense] bbcode (but looks like it's right)";
                const string slightlyWrong = "this is some [bu]slightly wrong[/bu] bbcode";
                const string typoWrong = "this is [is]typoed[/i] bbcode";

                ShouldNotContainMarkup(missingCloseWrong);
                ShouldNotContainMarkup(totallyWrong);
                ShouldNotContainMarkup(slightlyWrong);
                ShouldNotContainMarkup(typoWrong);
            }
        }

        #region Helpers

        private void ShouldContainLink(string input, string expected)
        {
            GetAll<Hyperlink>(input).AssertFirstTextIs(expected);
        }

        private IEnumerable<T> GetAll<T>(string input)
            where T : Inline
        {
            return FirstSpan(input).GetChildren<T>();
        }

        private IEnumerable<T> ShouldBeOneOf<T>(string input)
            where T : Inline
        {
            var all = GetAll<T>(input).ToList();

            Assert.IsTrue(all.Any());

            return all;
        }

        private void ShouldNotContainMarkup(string input)
        {
            var result = FirstSpan(input).GetChildren<Run>().First();

            Assert.IsTrue(result.Text == input);
        }

        private IEnumerable<Inline> Parse(string input)
        {
            return converter.Convert(input, null, null, null) as IEnumerable<Inline>;
        }

        private Span FirstSpan(string input)
        {
            return Parse(input).OfType<Span>().FirstOrDefault();
        }

        #endregion
    }

    public static class ExtensionHelpers
    {
        public static string GetText<T>(this T element)
            where T : Span
        {
            var span = element.GetChildren<Span>().FirstOrDefault();

            var run = span != null
                ? span.GetChildren<Run>().FirstOrDefault()
                : element.GetChildren<Run>().FirstOrDefault();

            Assert.IsNotNull(run);

            return run.Text;
        }

        public static bool IsColor(this Brush brush, Color suspectColor)
        {
            var asSolid = brush as SolidColorBrush;

            return asSolid != null && asSolid.Color.Equals(suspectColor);
        }

        public static IEnumerable<T> GetChildren<T>(this Span element)
            where T : Inline
        {
            return element.Inlines.OfType<T>();
        }

        public static IEnumerable<Span> GetChildren(this Span element)
        {
            return element.Inlines.OfType<Span>();
        }

        public static Span GetFirstChild(this Span element)
        {
            return element.GetChildren().First();
        }

        public static T GetFirstChild<T>(this Span element)
            where T : Inline
        {
            return element.GetChildren<T>().First();
        }

        public static void AssertTextIs(this Span element, string expected)
        {
            Assert.IsTrue(element.GetText().Equals(expected));
        }

        public static void AssertFirstTextIs(this IEnumerable<Span> elements, string expected)
        {
            elements.First().AssertTextIs(expected);
        }
    }
}
