#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Shell.xaml.cs">
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

namespace slimCat
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     The shell has no meaningful purpose other than being a giant container.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class Shell
    {
        #region Constants

        /// <summary>
        ///     The main region.
        /// </summary>
        public const string MainRegion = "MainRegion";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Shell" /> class.
        /// </summary>
        public Shell()
        {
            InitializeComponent();
        }

        #endregion
    }
}