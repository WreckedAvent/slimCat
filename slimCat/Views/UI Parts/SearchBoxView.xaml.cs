#region Copyright

// <copyright file="SearchBoxView.xaml.cs">
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

namespace slimCat.Views
{
    #region Usings

    using Models;

    #endregion

    /// <summary>
    ///     Interaction logic for SearchBoxView.xaml
    /// </summary>
    public partial class SearchBoxView
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchBoxView" /> class.
        /// </summary>
        public SearchBoxView()
        {
            InitializeComponent();

            OnContextChanged();

            DataContextChanged += (s, e) => OnContextChanged();
        }

        #endregion

        private void OnContextChanged()
        {
            vm = DataContext as GenericSearchSettingsModel;
            if (vm != null)
                shortcuts = new ShortcutManager(Entry, vm);
        }

        #region Fields

        private GenericSearchSettingsModel vm;
        private ShortcutManager shortcuts;

        #endregion
    }
}