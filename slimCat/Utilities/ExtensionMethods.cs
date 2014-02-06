#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Documents;
    using Models;

    #endregion

    /// <summary>
    ///     The extension methods.
    /// </summary>
    public static class ExtensionMethods
    {
        #region Public Methods and Operators

        /// <summary>
        ///     The first by id or default.
        /// </summary>
        /// <param name="model">
        ///     The model.
        /// </param>
        /// <param name="id">
        ///     The id.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="T" />.
        /// </returns>
        public static T FirstByIdOrDefault<T>(this ICollection<T> model, string id) where T : ChannelModel
        {
            return model.FirstOrDefault(param => param.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     The throw if null.
        /// </summary>
        /// <param name="x">
        ///     The x.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="T" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static T ThrowIfNull<T>(this T x, string name) where T : class
        {
            if (x == null)
                throw new ArgumentNullException(name);

            return x;
        }

        public static void Each<T>(this IEnumerable<T> collection, Action<T> functor)
        {
            foreach (var item in collection)
                functor(item);
        }

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

        #endregion
    }
}