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

        #endregion
    }
}