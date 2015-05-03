#region Copyright

// <copyright file="DictionaryExtensions.cs">
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

    using System.Collections.Generic;

    #endregion

    internal static class DictionaryExtensions
    {
        /// <summary>
        ///     Gets a string value out of the dictionary.
        /// </summary>
        public static string Get(this IDictionary<string, object> command, string key)
        {
            return command.Get<string>(key);
        }

        /// <summary>
        ///     Gets a kind of value out of the dictionary with a cast.
        /// </summary>
        public static T Get<T>(this IDictionary<string, object> command, string key) where T : class
        {
            return command[key] as T;
        }
    }
}