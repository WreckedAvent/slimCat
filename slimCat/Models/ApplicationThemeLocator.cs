#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationThemeLocator.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System.Windows;

    #endregion

    public class ApplicationThemeLocator : IThemeLocator
    {
        private readonly Application app;

        public ApplicationThemeLocator(Application app)
        {
            this.app = app;
        }

        public Style FindStyle(string styleName)
        {
            return app.TryFindResource(styleName) as Style;
        }

        public T Find<T>(string resourceName) where T : class
        {
            return app.TryFindResource(resourceName) as T;
        }
    }
}