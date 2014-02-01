
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
    using Slimcat.Views;

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

                ShouldBeOneOf<Bold>(textAtEnd).FirstTextShouldBe("bold text");
                ShouldBeOneOf<Bold>(textAtMiddle).FirstTextShouldBe("formed bold");
                ShouldBeOneOf<Bold>(textAtStart).FirstTextShouldBe("some well-formed");
            }

            [TestMethod]
            public void ItalicWorks()
            {
                const string text = "some well-formed [i]italic[/i] text";

                ShouldBeOneOf<Italic>(text).FirstTextShouldBe("italic");
            }

            [TestMethod]
            public void UnderlineWorks()
            {
                const string text = "some well-formed [u]underline[/u] text";

                ShouldBeOneOf<Underline>(text).FirstTextShouldBe("underline");
            }

            [TestMethod]
            public void StrikeThroughWorks()
            {
                const string text = "some well-formed [s]strike-through[/s] text";

                var result = ShouldBeOneOf<Span>(text).First(x => x.TextDecorations.Equals(TextDecorations.Strikethrough));

                result.TextShouldBe("strike-through");
            }

            [TestMethod]
            public void UrlWorks()
            {
                const string text = "some well formed [url=https://www.google.com]url[/url] text";

                ShouldBeOneOf<Hyperlink>(text).FirstTextShouldBe("url");
            }

            [TestMethod]
            public void SimpleUrlWorks()
            {
                const string text = "some well-formed [url]https://www.google.com[/url] text";

                ShouldBeOneOf<Hyperlink>(text).FirstTextShouldBe("google.com");
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

                ShouldBeOneOf<Span>(text).First(x => x.FontSize > 12).TextShouldBe("big");
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

                ShouldBeOneOf<Span>(text).First(x => x.FontSize < 12).TextShouldBe("small");
            }

            [TestMethod]
            public void SubscriptWorks()
            {
                const string text = "some well-formed [sub]sub[/sub] text";

                ShouldBeOneOf<Span>(text)
                    .First(x => x.BaselineAlignment == BaselineAlignment.Subscript)
                    .TextShouldBe("sub");
            }

            [TestMethod]
            public void SuperWorks()
            {
                const string text = "some well-formed [sup]sup[/sup] text";

                ShouldBeOneOf<Span>(text)
                    .First(x => x.BaselineAlignment == BaselineAlignment.Superscript)
                    .TextShouldBe("sup");
            }

            [TestMethod]
            public void ColorWorks()
            {
                ApplicationSettings.AllowColors = true; // TODO : get this as a dependency
                const string text = "some well-formed [color=green]color[/color] text";

                ShouldBeOneOf<Span>(text)
                    .First(x => x.Foreground.IsColor(Colors.Green))
                    .TextShouldBe("color");
            }

            [TestMethod]
            public void ColorToggleWorks()
            {
                ApplicationSettings.AllowColors = false;
                const string text = "some well-formed [color=green]color[/color] text";

                var result = ShouldBeOneOf<Span>(text).First(x => x.GetText().Equals("color"));
                result.ShouldBeDefaultColor();
            }

            [TestMethod]
            public void MissingColorIsHandledGracefully()
            {
                ApplicationSettings.AllowColors = true;
                const string text = "some well-formed [color]color[/color] text";

                var result = ShouldBeOneOf<Span>(text).First(x => x.GetText().Equals("color"));
                result.ShouldBeDefaultColor();
            }

            [TestMethod]
            public void InvalidColorIsHandledGracefully()
            {
                ApplicationSettings.AllowColors = true;
                const string text = "this is some text with [color=badcolor]a bad color in it[/color].";

                var result = ShouldBeOneOf<Span>(text).First(x => x.GetText().Equals("a bad color in it"));
                result.ShouldBeDefaultColor();
            }
            #endregion

            [TestMethod]
            public void StackedBbCodeWorks()
            {
                const string text = "some stacked [i][b]bbcode[/b][/i] text";

                var result = ShouldBeOneOf<Italic>(text).First().GetFirstChild<Bold>();

                result.TextShouldBe("bbcode");
            }

            [TestMethod]
            public void PlainChildrenInStackedBbCodeWorks()
            {
                const string text = "some stacked [i]bbcode with [b]plain text[/b] at various points[/i] ... text";

                var root = ShouldBeOneOf<Italic>(text).First();

                root.GetFirstChild().TextShouldBe("bbcode with ");
                root.GetFirstChild<Bold>().TextShouldBe("plain text");
                root.GetChildren().Last().TextShouldBe(" at various points");
            }

            [TestMethod]
            public void MultipleStackedBcodeWorks()
            {
                const string text = "some  [b]bbcode [u]with [i]lots[/i] of[/u] [u]stacking[/u][/b] text";

                var root = ShouldBeOneOf<Bold>(text).First();

                root.GetFirstChild().TextShouldBe("bbcode ");

                var second = root.GetFirstChild<Underline>();
                second.TextShouldBe("with ");

                second.GetFirstChild<Italic>().TextShouldBe("lots");
                second.GetChildren().Last().TextShouldBe(" of");
                root.GetChildren<Underline>().Last().TextShouldBe("stacking");
            }

            [TestMethod]
            public void TextBetweenBbCodeWorks()
            {
                const string text =
                    "Any [url=http://static.f-list.net/images/charimage/316487.jpg]Zerglings[/url] or [url=http://static.f-list.net/images/charimage/659750.jpg]Hydralisks[/url] Want a piece of my ass~? Also open to [url=http://static.f-list.net/images/charimage/729294.png]Sangheili[/url] and [url=https://static1.e621.net/data/sample/e9/d0/e9d0295fcd569bda48d2efe2762a2cbc.jpg]Xenomorphs~[/url] Would especially love you if you play a Sangheili~";

                var result = ShouldBeOneOf<Span>(text).ToList();

                // make sure urls are formed correctly
                var hyperlinks = result.OfType<Hyperlink>().ToList();
                hyperlinks[0].TextShouldBe("Zerglings");
                hyperlinks[1].TextShouldBe("Hydralisks");
                hyperlinks[2].TextShouldBe("Sangheili");
                hyperlinks[3].TextShouldBe("Xenomorphs~");

                // make sure text/spacing between them is preserverd
                var plainText = result.Where(x => !(x is Hyperlink)).ToList();
                plainText[0].TextShouldBe("Any ");
                plainText[1].TextShouldBe(" or ");
                plainText[2].TextShouldBe(" Want a piece of my ass~? Also open to ");
                plainText[3].TextShouldBe(" and ");
                plainText[4].TextShouldBe(" Would especially love you if you play a Sangheili~");
            }

            [TestMethod]
            public void StackedBbCodeOfSameTypeWorks()
            {
                const string text =
                    "b [color=red]r[color=green]g[/color][/color] b";

                var root = ShouldBeOneOf<Span>(text).ToList();
                root.FirstTextShouldBe("b ");

                var colorTag = root[1];
                colorTag.GetChildren().FirstTextShouldBe("r");
                colorTag.GetChildren().Skip(1).FirstTextShouldBe("g");

                var suspectedLast = root.Skip(2).First();
                var actualLast = root.Last();

                Assert.AreEqual(suspectedLast, actualLast);
                actualLast.TextShouldBe(" b");
            }

            [TestMethod]
            public void NoParseWorks()
            {
                const string text = "o [noparse][b]b[/b][/noparse] o";

                var result = ShouldBeOneOf<Span>(text).ToList();
                result[0].TextShouldBe("o ");

                result[1].GetChildren().FirstTextShouldBe("[b]b[/b]");
            }

            [TestMethod]
            public void MultipleNoParseWorks()
            {
                const string text = "o [noparse][i]i[/i][/noparse][noparse][b]b[/b][/noparse] o";

                var result = ShouldBeOneOf<Span>(text).ToList();
                result[0].TextShouldBe("o ");

                result[1].GetChildren().FirstTextShouldBe("[i]i[/i]");

                result[2].GetChildren().FirstTextShouldBe("[b]b[/b]");

                result.Last().TextShouldBe(" o");
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

            [TestMethod]
            public void UnclosedTagsWorks()
            {
                const string text = "o [b][b]b[/b] o";

                var result = ShouldBeOneOf<Span>(text).ToList();
                result[0].TextShouldBe("o ");

                Assert.IsFalse(result[1] is Bold);
                result[1].TextShouldBe("[b]");

                Assert.IsTrue(result[2] is Bold);
                result[2].TextShouldBe("b");

                Assert.IsFalse(result[3] is Bold);
                result[3].TextShouldBe(" o");

                Assert.IsTrue(result[3].Equals(result.Last()));
            }

            [TestMethod]
            public void UnclosedNestedTagsWorks()
            {
                const string text = "o [b]some well-formed bold with a run-away [user] tag[/b] o";

                var result = ShouldBeOneOf<Span>(text).ToList();

                result[0].TextShouldBe("o ");

                Assert.IsTrue(result[1] is Bold);
                var boldChildren = result[1].Inlines.OfType<Span>().ToList();
                boldChildren[0].TextShouldBe("some well-formed bold with a run-away ");
                boldChildren[1].TextShouldBe("[user]");
                boldChildren[2].TextShouldBe(" tag");
            }
        }

        #region Helpers

        private void ShouldContainLink(string input, string expected)
        {
            GetAll<Hyperlink>(input).FirstTextShouldBe(expected);
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

        public static void ShouldBeDefaultColor(this Span element)
        {
            Assert.IsTrue(element.Foreground.IsColor(Colors.Black));
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

        public static void TextShouldBe(this Span element, string expected)
        {
            Assert.IsTrue(element.GetText().Equals(expected));
        }

        public static void FirstTextShouldBe(this IEnumerable<Span> elements, string expected)
        {
            elements.First().TextShouldBe(expected);
        }
    }
}
