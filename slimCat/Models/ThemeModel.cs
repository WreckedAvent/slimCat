#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThemeModel.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System.Windows.Media;

    #endregion

    public class ThemeModel
    {
        public string Name { get; set; }

        public ICharacter Author { get; set; }

        public string Version { get; set; }

        public Color BackgroundColor { get; set; }

        public Color ForegroundColor { get; set; }

        public Color ContrastColor { get; set; }

        public Color DepressedColor { get; set; }

        public Color BrightBackgroundColor { get; set; }

        public Color HighlightColor { get; set; }

        public string Url { get; set; }
    }
}