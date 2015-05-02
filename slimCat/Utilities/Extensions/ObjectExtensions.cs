#region Copyright

// <copyright file="ObjectExtensions.cs">
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
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;
    using lib;
    using Models;
    using ViewModels;

    #endregion

    public static class ObjectExtensions
    {
        private static readonly Dictionary<Type, Func<Hyperlink, string>> TypeToGetName = new Dictionary
            <Type, Func<Hyperlink, string>>
        {
            {typeof (CharacterModel), x => ((CharacterModel) x.DataContext).Name},
            {typeof (MessageModel), x => ((MessageModel) x.DataContext).Poster.Name},
            {typeof (CharacterUpdateModel), x => ((CharacterUpdateModel) x.DataContext).TargetCharacter.Name}
        };

        /// <summary>
        ///     Throws an <see cref="ArgumentNullException" /> if the value provided is null.
        /// </summary>
        public static T ThrowIfNull<T>(this T x, string name) where T : class
        {
            if (x == null)
                throw new ArgumentNullException(name);

            return x;
        }

        /// <summary>
        ///     Attempts to convert to an enum.
        /// </summary>
        public static T ToEnum<T>(this object obj)
        {
            var str = obj as string;
            return (T) Enum.Parse(typeof (T), str, true);
        }

        public static T FindChild<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T)
                    return child as T;

                var grandchild = FindChild<T>(child);

                if (grandchild != null)
                    return grandchild;
            }

            return default(T);
        }

        public static void TryOpenRightClickMenuCommand<T>(this object sender, int ancestorLevel)
            where T : DependencyObject
        {
            var hyperlink = (Hyperlink) sender;

            string characterName;
            Func<Hyperlink, string> nameFunc;
            if (TypeToGetName.TryGetValue(hyperlink.DataContext.GetType(), out nameFunc))
                characterName = nameFunc(hyperlink);
            else
                return;

            var parentObject = hyperlink.TryFindAncestor<T>(ancestorLevel);
            if (parentObject == null)
                return;

            ViewModelBase parentDataContext;
            var frameworkElement = parentObject as FrameworkElement;
            var contentElement = parentObject as FrameworkContentElement;
            if (frameworkElement != null)
                parentDataContext = frameworkElement.DataContext as ViewModelBase;
            else if (contentElement != null)
                parentDataContext = contentElement.DataContext as ViewModelBase;
            else
                return;

            var relayCommand = parentDataContext.OpenRightClickMenuCommand;
            if (relayCommand.CanExecute(characterName))
                relayCommand.Execute(characterName);
        }
    }
}