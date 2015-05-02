#region Copyright

// <copyright file="ListExtensions.cs">
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
    using System.Linq;
    using Models;

    #endregion

    public static class ListExtensions
    {
        /// <summary>
        ///     Returns the first channel model with the given Id, or <see langword="null" /> if none exist.
        /// </summary>
        public static T FirstByIdOrNull<T>(this ICollection<T> model, string id) where T : ChannelModel
        {
            return model.FirstOrDefault(param => param.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Does an action against each item in the collection.
        /// </summary>
        public static void Each<T>(this IEnumerable<T> collection, Action<T> functor)
        {
            foreach (var item in collection.ToList())
                functor(item);
        }

        /// <summary>
        ///     Backlogs the item in the specified collections. Removes items from the start of the list until it is under the max
        ///     count.
        /// </summary>
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

        public static bool CharacterIsInList(this ICollection<ICharacter> collection, ICharacter toFind)
        {
            return collection.Any(character => character.Name.Equals(toFind.Name, StringComparison.OrdinalIgnoreCase));
        }
    }
}