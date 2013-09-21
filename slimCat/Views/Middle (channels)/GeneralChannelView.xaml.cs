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

namespace Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows.Documents;
    using System.Windows.Threading;

    using Models;

    using ViewModels;

    /// <summary>
    ///     Interaction logic for GeneralChannelView.xaml
    /// </summary>
    public partial class GeneralChannelView : DisposableView
    {
        #region Fields

        private bool historyLoaded;

        private bool historyInitialized;

        private bool loaded;

        private GeneralChannelViewModel vm;

        private KeepToCurrentScrollViewer scroller;

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

        internal override void Dispose(bool isManaged)
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
                        var items = e.NewItems.Cast<IMessage>();
                        foreach (var template in items.Select(item => new MessageView { DataContext = item }))
                        {
                            this.Messages.Blocks.Add(template);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    {
                        this.Messages.Blocks.Clear();
                        var loadedCount = 0;

                        foreach (var template in this.GetHistory().Reverse().Select(item => new HistoryView { DataContext = item }))
                        {
                            this.AddAsync(template, ref loadedCount);
                        }

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


                        this.Messages.Blocks.Remove(this.Messages.Blocks.FirstBlock);
                        this.PopupAnchor.UpdateLayout();
                        this.scroller.ScrollToStick();
                    }

                    break;
            }
        }

        private void OnLoad(object s, EventArgs e)
        {
            var loadedCount = 0;
            this.scroller = new KeepToCurrentScrollViewer(PopupAnchor);

            foreach (var template in this.vm.CurrentMessages.Reverse().Select(item => new MessageView { DataContext = item }))
            {
                this.AddAsync(template, ref loadedCount);
            }

            if (this.historyInitialized)
            {
                return;
            }

            foreach (var template in this.GetHistory().Reverse().Select(item => new HistoryView { DataContext = item }))
            {
                this.AddAsync(template, ref loadedCount);
            }

            this.historyLoaded = true;
            this.historyInitialized = true;
            this.loaded = true;
        }

        private IEnumerable<string> GetHistory()
        {
            var history = this.vm.Model.History;
            Func<string, bool> isChatMessage = s => s.StartsWith("[", StringComparison.OrdinalIgnoreCase);

            return this.vm.IsDisplayingChat ? history.Where(isChatMessage) : history.Where(s => !isChatMessage(s));
        }

        private void AddAsync(Block item, ref int count)
        {
            count++;

            var priority = count < 25 ? DispatcherPriority.Normal : DispatcherPriority.DataBind;
            if (count > 25) return;

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
        #endregion
    }
}