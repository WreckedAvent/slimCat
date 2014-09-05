#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IThemeLocator.cs">
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

    using System.Windows;

    #endregion

    /// <summary>
    ///     Represents theme location for styling WPF elements.
    /// </summary>
    public interface IThemeLocator
    {
        /// <summary>
        ///     Finds the style.
        /// </summary>
        /// <param name="styleName">Name of the style.</param>
        Style FindStyle(string styleName);

        /// <summary>
        ///     Finds the specified resource name.
        /// </summary>
        /// <typeparam name="T">Type of resource to return.</typeparam>
        /// <param name="resourceName">Name of the resource.</param>
        T Find<T>(string resourceName) where T : class;
    }
}