#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PMChannelView.xaml.cs">
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

namespace slimCat.Views
{
    #region Usings

    using slimCat.Models;
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for PmChannelView.xaml
    /// </summary>
    public partial class PmChannelView
    {
        #region Fields

        private bool isAdded = true;
        private Inline lastItem;
        private PmChannelViewModel vm;
        private ScrollViewer scroller;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PmChannelView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public PmChannelView(PmChannelViewModel vm)
        {
            try
            {
                InitializeComponent();
                this.vm = vm.ThrowIfNull("vm");

                DataContext = this.vm;

                this.vm.StatusChanged += OnStatusChanged;
            }
            catch (Exception ex)
            {
                ex.Source = "PmChannel View, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        override protected void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            vm.StatusChanged -= OnStatusChanged;
            DataContext = null;
            vm = null;
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(
                (Action) delegate
                {
                    if (!CharacterStatusDisplayer.IsExpanded)
                        CharacterStatusDisplayer.IsExpanded = true;
                });
        }

        #endregion

        private void CloseImage(object sender, RoutedEventArgs e)
        {
            if (vm == null || !isAdded) return;

            vm.CurrentImage = null;
            lastItem = ProfileParagraph.Inlines.FirstInline;
            ProfileParagraph.Inlines.Remove(lastItem);
            isAdded = false;
        }

        private void OnSelected(object sender, RoutedEventArgs e)
        {
            if (isAdded)
            {
                Reader.Document.BringIntoView(); // Scrolls to top, where the image is
                return;
            }

            ProfileParagraph.Inlines.InsertBefore(ProfileParagraph.Inlines.FirstInline, lastItem);
            Reader.Document.BringIntoView(); // Scrolls to top, where the image is

            isAdded = true;
        }

        private void OnEntryBoxResizeRequested(object sender, MouseButtonEventArgs e)
        {
            EntryBoxRowDefinition.Height = new GridLength();
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            StaticFunctions.TryOpenRightClickMenuCommand<Grid>(sender, 2);
        }

        private void OnProfileMouseWheelPreview(object sender, MouseWheelEventArgs e)
        {
            if (scroller == null)
            {
                scroller = StaticFunctions.FindChild<ScrollViewer>(sender as DependencyObject);
                if (scroller == null)
                    return;
            }

            scroller.ScrollToVerticalOffset(scroller.VerticalOffset - StaticFunctions.GetScrollDistance(e.Delta, ApplicationSettings.FontSize-4));

            e.Handled = true;
        }
    }
}