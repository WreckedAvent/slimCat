#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Documents;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using System.Windows.Media;
    using System.Windows;
    using System.Windows.Controls;
    using slimCat.ViewModels;

    #endregion

    /// <summary>
    ///     The extension methods.
    /// </summary>
    public static class ExtensionMethods
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Returns the first channel model with the given Id, or <see langword="null" /> if none exist.
        /// </summary>
        public static T FirstByIdOrNull<T>(this ICollection<T> model, string id) where T : ChannelModel
        {
            return model.FirstOrDefault(param => param.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

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
        ///     Does an action against each item in the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="functor">The functor to apply to each item.</param>
        public static void Each<T>(this IEnumerable<T> collection, Action<T> functor)
        {
            foreach (var item in collection.ToList())
                functor(item);
        }

        /// <summary>
        ///     Uses a string as a format provider with the given arguments.
        /// </summary>
        public static string FormatWith(this string toFormat, params object[] args)
        {
            return string.Format(toFormat, args);
        }

        public static void RemoveAt(this BlockCollection collection, int index)
        {
            if (index == -1 || collection.Count == 0)
                return;

            collection.Remove(collection.ElementAt(index));
        }

        public static void AddAt(this BlockCollection collection, int index, Block item)
        {
            if (index == -1)
                return;

            if (collection.Count == 0)
            {
                collection.Add(item);
                return;
            }

            index = Math.Min(index, collection.Count - 1);

            collection.InsertAfter(collection.ElementAt(index), item);
        }

        public static string Get(this IDictionary<string, object> command, string key)
        {
            return command.Get<string>(key);
        }

        public static T Get<T>(this IDictionary<string, object> command, string key) where T : class
        {
            return command[key] as T;
        }

        public static T ToEnum<T>(this object obj)
        {
            var str = obj as string;
            return (T) Enum.Parse(typeof (T), str, true);
        }

        public static T ToEnum<T>(this string str)
        {
            return (T) Enum.Parse(typeof (T), str, true);
        }

        /// <summary>
        ///     Sends the command as the current user.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="channel">The channel.</param>
        public static void SendUserCommand(this IEventAggregator events, string commandName,
            IList<string> arguments = null, string channel = null)
        {
            events.GetEvent<UserCommandEvent>()
                .Publish(CommandDefinitions.CreateCommand(commandName, arguments, channel).ToDictionary());
        }

        /// <summary>
        ///     Backlogs the item in the specified collections. Removes items from the start of the list until it is under the max
        ///     count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="item">The item to add.</param>
        /// <param name="maxCount">The maximum count.</param>
        public static void Backlog<T>(this IList<T> collection, T item, int maxCount)
        {
            if (maxCount > 0)
            {
                while (collection.Count >= maxCount)
                {
                    collection.RemoveAt(0);
                }
            }

            collection.Add(item);
        }

        public static void BacklogWithUpdate<T>(this IList<T> collection, T item, int maxCount)
        {
            var index = collection.IndexOf(item);
            if (index == -1)
            {
                collection.Backlog(item, maxCount);
                return;
            }

            collection.RemoveAt(index);

            if (maxCount > 0)
            {
                while (collection.Count > maxCount)
                {
                    collection.RemoveAt(0);
                }
            }

            collection.Add(item);
        }

        /// <summary>
        ///     Break a list of items into chunks of a specific size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }

        public static void TryOpenRightClickMenuCommand(this object sender, int ancestorLevel)
        {
            var hyperlink = (Hyperlink)sender;

            var characterName = string.Empty;
            if (hyperlink.DataContext is CharacterModel)
                characterName = ((CharacterModel)hyperlink.DataContext).Name;
            else if (hyperlink.DataContext is MessageModel)
                characterName = ((MessageModel)hyperlink.DataContext).Poster.Name;
            else
                return;

            var parentGrid = TryFindAncestor<Grid>(hyperlink, ancestorLevel);
            if (parentGrid == null)
                return;

            var parentGridDataContext = parentGrid.DataContext as ViewModelBase;
            if (parentGridDataContext == null)
                return;

            var relayCommand = parentGridDataContext.OpenRightClickMenuCommand;

            if (relayCommand.CanExecute(characterName))
                relayCommand.Execute(characterName);
        }

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <param name="ancestorLevel">The number of times the type must
        /// be found up the tree.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T TryFindAncestor<T>(this DependencyObject child, int ancestorLevel)
            where T : DependencyObject
        {
            //get parent item
            var parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            var parent = parentObject as T;
            if (parent != null)
                ancestorLevel--;

            if (ancestorLevel == 0)
                return parent;

            //use recursion to proceed with next level
            return TryFindAncestor<T>(parentObject, ancestorLevel);
        }

        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Keep in mind that for content element,
        /// this method falls back to the logical tree of the element!
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(this DependencyObject child)
        {
            if (child == null) return null;

            //handle content elements separately
            var contentElement = child as ContentElement;
            if (contentElement != null)
            {
                var parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                var fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            //also try searching for parent in framework elements (such as DockPanel, etc)
            var frameworkElement = child as FrameworkElement;
            if (frameworkElement != null)
            {
                var parent = frameworkElement.Parent;
                if (parent != null) return parent;
            }

            //if it's not a ContentElement/FrameworkElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }

        #endregion
    }
}