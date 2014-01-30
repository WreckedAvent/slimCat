
namespace slimCatTest
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Documents;
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
            var dummySession = new GeneralChannelModel("Love Room", ChannelType.Private);

            var chat = new Mock<IChatModel>();

            chat.Setup(x => x.FindChannel("Love Room", It.IsAny<string>()))
                .Returns(dummySession);

            var manager = Mock.Of<ICharacterManager>();
            var chatModel = chat.Object;
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
            [TestMethod]
            public void BoldWorks()
            {
                const string textAtEnd = "some well-formed [b]bold text[/b]";
                const string textAtStart = "[b]some well-formed[/b] bold text";
                const string textAtMiddle = "some well-[b]formed bold[/b] text";

                ShouldBeOneOf<Bold>(textAtEnd);
                ShouldBeOneOf<Bold>(textAtMiddle);
                ShouldBeOneOf<Bold>(textAtStart);
            }

            [TestMethod]
            public void ItalicWorks()
            {
                const string text = "some well-formed [i]italic[/i] text";

                var result = ShouldBeOneOf<Italic>(text).First();
                Assert.IsTrue(result.GetText() == "italic");
            }

            [TestMethod]
            public void UnderlineWorks()
            {
                const string text = "some well-formed [u]underline[/u] text";

                var result = ShouldBeOneOf<Underline>(text).First();
                Assert.IsTrue(result.GetText().Equals("underline"));
            }

            [TestMethod]
            public void StrikeThroughWorks()
            {
                const string text = "some well-formed [s]strike-through[/s] text";

                var result = ShouldBeOneOf<Span>(text).First(x => x.TextDecorations.Equals(TextDecorations.Strikethrough));

                Assert.IsTrue(result.GetText().Equals("strike-through"));
            }

            [TestMethod]
            public void UrlWorks()
            {
                const string text = "some well formed [url=https://www.google.com]url[/url] text";

                var result = ShouldBeOneOf<Hyperlink>(text).First();

                Assert.IsTrue(result.GetText().Equals("url"));
            }

            [TestMethod]
            public void SimpleUrlWorks()
            {
                const string text = "some well-formed [url]https://www.google.com[/url] text";

                var result = ShouldBeOneOf<Hyperlink>(text).First();

                Assert.IsTrue(result.GetText().Equals("google.com"));
            }

            [TestMethod]
            public void SessionWorks()
            {
                const string text = "some well-formed [session=Love Bar]ADH-SOMENONSENSE[/session] session";

                ShouldBeOneOf<InlineUIContainer>(text);
            }

            [TestMethod]
            public void ChannelWorks()
            {
                const string text = "some well-formed [channel]Helpdesk[/channel] channel text";

                ShouldBeOneOf<InlineUIContainer>(text);
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
            var result = GetAll<Hyperlink>(input).First().GetText();

            Assert.IsTrue(result == expected);
        }

        private IEnumerable<T> GetAll<T>(string input)
            where T : Inline
        {
            return FirstSpan(input).Inlines.OfType<T>();
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
            var result = FirstSpan(input).Inlines.OfType<Run>().FirstOrDefault();

            Assert.IsNotNull(result);
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
            var span = element.Inlines.OfType<Span>().FirstOrDefault();

            var run = span != null
                ? span.Inlines.OfType<Run>().FirstOrDefault()
                : element.Inlines.OfType<Run>().FirstOrDefault();

            Assert.IsNotNull(run);

            return run.Text;
        }
    }
}
