#region Copyright

// <copyright file="PmOrChannelTemplateSelector.cs">
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

    using System.Windows;
    using System.Windows.Controls;
    using Models;

    #endregion

    public class PmOrChannelTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate
            SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;

            if (element == null) return null;

            return (ApplicationSettings.ShowAvatars)
                ? element.FindResource("PmChannelTemplate") as DataTemplate
                : element.FindResource("GeneralChannelTemplate") as DataTemplate;
        }
    }
}