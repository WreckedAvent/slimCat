#region Copyright

// <copyright file="BbCodeType.cs">
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
}