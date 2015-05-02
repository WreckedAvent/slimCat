#region Copyright

// <copyright file="ConverterTests.cs">
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

namespace slimCatTest
{
    #region Usings

    using System.Windows;
    using System.Windows.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using slimCat.Utilities;

    #endregion

    [TestClass]
    public class ConverterTests
    {
        private const Visibility visible = Visibility.Visible;
        private const Visibility notVisible = Visibility.Collapsed;

        [TestMethod]
        public void OppositeBoolConverterWorks()
        {
            var converter = new OppositeBoolConverter();

            Assert.AreEqual(converter.Call(true), notVisible);
            Assert.AreEqual(converter.Call(false), visible);
        }

        [TestMethod]
        public void GreaterThanZeroConverterWorks()
        {
            var converter = new GreaterThanZeroConverter();

            Assert.AreEqual(converter.Call(10), visible);
            Assert.AreEqual(converter.Call(0), notVisible);
            Assert.AreEqual(converter.Call(-2), notVisible);
        }

        [TestMethod]
        public void EmptyConverterWorks()
        {
            var converter = new EmptyConverter();

            Assert.AreEqual(converter.Call(""), notVisible);
            Assert.AreEqual(converter.Call(null), notVisible);
            Assert.AreEqual(converter.Call("foobar"), visible);
        }

        [TestMethod]
        public void NotEmptyConverterWorks()
        {
            var converter = new NotEmptyConverter();

            Assert.AreEqual(converter.Call(""), visible);
            Assert.AreEqual(converter.Call(null), visible);
            Assert.AreEqual(converter.Call("foobar"), notVisible);
        }
    }

    internal static class ConverterTestsStaticHelpers
    {
        public static object Call(this IValueConverter converter, object value)
        {
            return converter.Convert(value, null, null, null);
        }
    }
}