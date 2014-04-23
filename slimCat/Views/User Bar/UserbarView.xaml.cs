#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserbarView.xaml.cs">
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

namespace slimCat.Views
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Shapes;
    using Models;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for Userbar.xaml
    /// </summary>
    public partial class UserbarView
    {
        private readonly UserbarViewModel vm;

        private Point lastPoint;

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserbarView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public UserbarView(UserbarViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            this.vm = vm;
        }

        #endregion

        private void OnPreviewMouseClick(object sender, MouseEventArgs e)
        {
            var draggedItem = sender as ListBoxItem;
            if (draggedItem == null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (e.OriginalSource is Rectangle) return;

            lastPoint = e.GetPosition(this);
        }

        private void OnDragDrop<T>(object sender, DragEventArgs e, IList<T> coll, bool isChannel)
            where T : ChannelModel
        {
            var droppedData = e.Data.GetData(typeof(T)) as T;
            var target = ((ListBoxItem)(sender)).DataContext as T;

            if (droppedData == null || target == null) return;

            var oldIndex = coll.IndexOf(droppedData);
            var newIndex = coll.IndexOf(target);

            if (oldIndex < newIndex)
            {
                coll.Insert(newIndex + 1, droppedData);
                coll.RemoveAt(oldIndex);
            }
            else
            {
                var remIdx = oldIndex + 1;
                if (coll.Count + 1 <= remIdx) return;

                coll.Insert(newIndex, droppedData);
                coll.RemoveAt(remIdx);
            }

            if (isChannel)
                vm.ChannelSelected = newIndex;
            else
                vm.PmSelected = newIndex;
        }

        private void CheckAs<T>(object sender, DragEventArgs e)
            where T : ChannelModel
        {
            var possible = e.Data.GetData(typeof(T)) as T;
            if (possible != null) return;

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void OnChannelModelDragDrop(object sender, DragEventArgs e)
        {
            OnDragDrop(sender, e, vm.ChatModel.CurrentChannels, true);
        }

        private void OnPmModelDragDrop(object sender, DragEventArgs e)
        {
            OnDragDrop(sender, e, vm.ChatModel.CurrentPms, false);
        }

        private void CheckAsChannel(object sender, DragEventArgs e)
        {
            CheckAs<GeneralChannelModel>(sender, e);
        }

        private void CheckAsPm(object sender, DragEventArgs e)
        {
            CheckAs<PmChannelModel>(sender, e);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var draggedItem = sender as ListBoxItem;
            if (draggedItem == null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (e.OriginalSource is Rectangle) return;

            var current = e.GetPosition(this);

            if (Math.Abs(lastPoint.Y - current.Y) >= SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
            }
        }
    }
}