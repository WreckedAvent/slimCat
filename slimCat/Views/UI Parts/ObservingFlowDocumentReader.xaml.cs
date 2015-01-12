#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObservingFlowDocumentReader.xaml.cs">
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

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Threading;
    using Models;
    using Utilities;

    #endregion

    /// <summary>
    ///     Interaction logic for ObservingFlowDocumentReader.xaml
    /// </summary>
    public partial class ObservingFlowDocumentReader
    {
        #region Fields

        public static readonly DependencyProperty MessageSourceProperty =
            DependencyProperty.Register(
                "MessageSource",
                typeof (ObservableCollection<IViewableObject>),
                typeof (ObservingFlowDocumentReader),
                new PropertyMetadata(default(ObservableCollection<object>), OnMessageSourceChanged));

        public static readonly DependencyProperty LoadInReverseProperty =
            DependencyProperty.Register(
                "LoadInReverse",
                typeof (bool),
                typeof (ObservingFlowDocumentReader),
                new PropertyMetadata(default(bool)));

        private bool loaded;
        private KeepToCurrentScrollViewer scroller;

        #endregion

        #region Constructors

        public ObservingFlowDocumentReader()
        {
            InitializeComponent();
            Root.VerticalContentAlignment = ApplicationSettings.StickNewMessagesToBottom ? VerticalAlignment.Bottom : VerticalAlignment.Top;
        }

        #endregion

        #region Properties

        public ObservableCollection<IViewableObject> MessageSource
        {
            private get { return (ObservableCollection<IViewableObject>) GetValue(MessageSourceProperty); }

            set { SetValue(MessageSourceProperty, value); }
        }

        public bool LoadInReverse
        {
            private get { return (bool) GetValue(LoadInReverseProperty); }

            set { SetValue(LoadInReverseProperty, value); }
        }
        #endregion

        #region Methods

        private static void OnMessageSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var @this = (ObservingFlowDocumentReader) o;

            @this.Messages.Blocks.Clear();

            var old = e.OldValue as ObservableCollection<IViewableObject>;
            var @new = e.NewValue as ObservableCollection<IViewableObject>;

            if (old != null)
                old.CollectionChanged -= @this.OnMessagesUpdate;

            if (@new != null)
                @new.CollectionChanged += @this.OnMessagesUpdate;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            scroller = scroller ?? new KeepToCurrentScrollViewer(Root);
            IEnumerable<IViewableObject> messageSource = MessageSource;

            if (LoadInReverse)
            {
                var count = 0;
                var messages = messageSource
                    .Reverse()
                    .Select(x => x.View)
                    .GetEnumerator();

                while (count <= ApplicationSettings.PreloadMessageAmount && messages.MoveNext())
                {
                    count++;
                    AddInReverseAsync(messages.Current, DispatcherPriority.DataBind);
                }

                if (count >= ApplicationSettings.PreloadMessageAmount)
                {
                    var delayTimer = new DispatcherTimer(DispatcherPriority.DataBind)
                    {
                        Interval = TimeSpan.FromMilliseconds(450)
                    };
                    delayTimer.Tick += (o, args) =>
                    {
                        var batch = count + 25;
                        while (count < batch)
                        {
                            if (messages.MoveNext())
                            {
                                count++;
                                AddInReverseAsync(messages.Current, DispatcherPriority.DataBind);
                            }
                            else
                            {
                                delayTimer.Stop();
                                break;
                            }
                        }
                    };
                    delayTimer.Start();
                }

                loaded = true;
                return;
            }

            messageSource
                .Select(x => x.View)
                .Each(AddAsync);

            loaded = true;
        }


        private void OnMessagesUpdate(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!loaded)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    e.NewItems.Cast<IViewableObject>()
                        .Select(x => x.View)
                        .Each(x => AddAtAsync(e.NewStartingIndex, x));
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Dispatcher.Invoke((Action) (() => Messages.Blocks.Clear()));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    scroller.Stick();
                    Messages.Blocks.RemoveAt(e.OldStartingIndex);
                    scroller.ScrollToStick();
                    break;
            }
        }

        private void AddInReverseAsync(Block item, DispatcherPriority priority)
        {
            Dispatcher.BeginInvoke(
                (Action) delegate
                {
                    var last = Messages.Blocks.LastBlock;
                    if (last != null)
                        Messages.Blocks.InsertBefore(Messages.Blocks.FirstBlock, item);
                    else
                        Messages.Blocks.Add(item);
                }, priority);
        }

        private void AddAsync(Block item)
        {
            Dispatcher.BeginInvoke((Action) (() => Messages.Blocks.Add(item)));
        }

        private void AddAtAsync(int index, Block item)
        {
            Dispatcher.BeginInvoke((Action) (() => Messages.Blocks.AddAt(index, item)));
        }

        #endregion
    }
}