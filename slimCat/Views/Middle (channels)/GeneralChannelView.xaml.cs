// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralChannelView.xaml.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   Interaction logic for GeneralChannelView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows.Documents;
    using System.Windows.Threading;

    using Slimcat.Models;
    using Slimcat.Utilities;
    using Slimcat.ViewModels;

    /// <summary>
    ///     Interaction logic for GeneralChannelView.xaml
    /// </summary>
    public partial class GeneralChannelView
    {
        #region Fields

        private bool historyLoaded;

        private bool historyInitialized;

        private bool loaded;

        private GeneralChannelViewModel vm;

        private KeepToCurrentScrollViewer scroller;

        private int loadedCount;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralChannelView"/> class.
        /// </summary>
        /// <param name="vm">
        /// The vm.
        /// </param>
        public GeneralChannelView(GeneralChannelViewModel vm)
        {
            try
            {
                this.InitializeComponent();
                this.vm = vm.ThrowIfNull("vm");

                this.DataContext = this.vm;
                this.vm.CurrentMessages.CollectionChanged += this.OnDisplayChanged;
            }
            catch (Exception ex)
            {
                ex.Source = "General Channel View, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
            {
                return;
            }

            this.vm.CurrentMessages.CollectionChanged -= this.OnDisplayChanged;
            this.vm = null;
            this.DataContext = null;
            this.scroller = null;
        }

        private void OnDisplayChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!this.loaded)
            {
                return; // sometimes we will get a collection changed before our UI has finished loading
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        e.NewItems
                            .Cast<IMessage>()
                            .Select(item => new MessageView { DataContext = item })
                            .Each(t => this.AddAtPositionAsync(t, e.NewStartingIndex));
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    {
                        this.Messages.Blocks.Clear();
                        this.loadedCount = 0;
                        this.historyLoaded = false;

                        break;
                    }

                case NotifyCollectionChangedAction.Remove:
                    {
                        this.scroller.Stick();
                        if (this.historyLoaded)
                        {
                            for (var i = 0; i < this.vm.Model.History.Count; i++)
                            {
                                this.Messages.Blocks.Remove(this.Messages.Blocks.FirstBlock);
                            }

                            this.historyLoaded = false;
                        }

                        this.Messages.Blocks.Remove(
                            e.OldStartingIndex != -1
                                ? this.Messages.Blocks.ElementAt(e.OldStartingIndex)
                                : this.Messages.Blocks.FirstBlock);

                        this.PopupAnchor.UpdateLayout();
                        this.scroller.ScrollToStick();
                        this.loadedCount--;
                    }

                    break;
            }
        }

        private void OnLoad(object s, EventArgs e)
        {
            lock (this.vm.CurrentMessages)
            lock (this.Messages)
            {
                this.scroller = new KeepToCurrentScrollViewer(PopupAnchor);

                this.vm.CurrentMessages
                    .Reverse()
                    .Select(item => new MessageView() { DataContext = item })
                    .Each(this.AddAsync);

                if (this.historyInitialized)
                {
                    return;
                }

                this.GetHistory()
                    .Reverse()
                    .Select(item => new HistoryView { DataContext = item })
                    .Each(this.AddAsync);

                this.historyLoaded = true;
                this.historyInitialized = true;
                this.loaded = true;
            }
        }

        private IEnumerable<string> GetHistory()
        {
            var history = this.vm.Model.History;
            Func<string, bool> isChatMessage = s => s.StartsWith("[", StringComparison.OrdinalIgnoreCase);

            return this.vm.IsDisplayingChat ? history.Where(isChatMessage) : history.Where(s => !isChatMessage(s));
        }

        private void AddAsync(Block item)
        {
            this.loadedCount++;

            var priority = this.loadedCount < 25 ? DispatcherPriority.Normal : DispatcherPriority.DataBind;
            if (this.loadedCount > 25)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                priority,
                (Action)(() =>
                    {
                        var last = this.Messages.Blocks.LastBlock;
                        if (last != null)
                        {
                            this.Messages.Blocks.InsertBefore(this.Messages.Blocks.FirstBlock, item);
                        }
                        else
                        {
                            this.Messages.Blocks.Add(item);
                        }
                }));
        }

        private void AddAtPositionAsync(Block item, int index)
        {
            var offset = this.historyLoaded ? index + this.GetHistory().Count() : index;
            offset--;
            offset = Math.Min(offset, this.Messages.Blocks.Count() - 1);

            if (offset > 0)
            {
                var block = this.Messages.Blocks.ElementAt(offset);
                this.Dispatcher.BeginInvoke((Action)(() => this.Messages.Blocks.InsertAfter(block, item)));
            }
            else
            {
                this.Dispatcher.BeginInvoke((Action)(() => this.Messages.Blocks.Add(item)));
            }
        }
        #endregion
    }
}