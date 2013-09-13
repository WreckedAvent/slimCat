// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PMChannelView.xaml.cs" company="Justin Kadrovach">
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
//   Interaction logic for PMChannelView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Views
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows.Documents;
    using System.Windows.Threading;

    using Models;

    using ViewModels;

    /// <summary>
    ///     Interaction logic for PMChannelView.xaml
    /// </summary>
    public partial class PMChannelView : DisposableView
    {
        #region Fields

        private bool _historyLoaded = false;

        private bool _historyInitialized = false;

        private PMChannelViewModel _vm;

        private KeepToCurrentScrollViewer _scroll;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PMChannelView"/> class.
        /// </summary>
        /// <param name="vm">
        /// The vm.
        /// </param>
        public PMChannelView(PMChannelViewModel vm)
        {
            try
            {
                this.InitializeComponent();
                this._vm = vm.ThrowIfNull("vm");

                this.DataContext = this._vm;

                this._vm.Model.Messages.CollectionChanged += this.OnDisplayChanged;
                this._vm.StatusChanged += this.OnStatusChanged;
            }
            catch (Exception ex)
            {
                ex.Source = "PMChannel View, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods
        private void OnDisplayChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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
                    this.Messages.Blocks.Clear();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    {
                        this._scroll.Stick();
                        if (this._historyLoaded)
                        {
                            for (var i = 0; i < this._vm.Model.History.Count; i++)
                            {
                                this.Messages.Blocks.Remove(this.Messages.Blocks.FirstBlock);
                            }

                            this._historyLoaded = false;
                        }


                        this.Messages.Blocks.Remove(this.Messages.Blocks.FirstBlock);
                        this.PopupAnchor.UpdateLayout();
                        this._scroll.ScrollToStick();
                    }

                    break;
            }
        }

        private void OnLoad(object s, EventArgs e)
        {
            var loadedCount = 0;
            this._scroll = new KeepToCurrentScrollViewer(PopupAnchor);

            foreach (var template in this._vm.Model.Messages.Reverse().Select(item => new MessageView { DataContext = item }))
            {
                this.AddAsync(template, ref loadedCount);
            }

            if (this._historyInitialized)
            {
                return;
            }

            foreach (var template in this._vm.Model.History.Reverse().Select(item => new HistoryView { DataContext = item }))
            {
                this.AddAsync(template, ref loadedCount);
            }

            this._historyLoaded = true;
            this._historyInitialized = true;
        }

        private void AddAsync(Block item, ref int count)
        {
            count++;

            var priority = count < 25 ? DispatcherPriority.Render : DispatcherPriority.DataBind;
            Dispatcher.BeginInvoke(
                priority,
                (Action)delegate
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
                });
        }

        internal override void Dispose(bool IsManaged)
        {
            if (!IsManaged)
            {
                return;
            }

            this._vm.StatusChanged -= this.OnStatusChanged;
            this._vm.Model.Messages.CollectionChanged -= this.OnDisplayChanged;
            this.DataContext = null;
            this._vm = null;
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        if (!this.CharacterStatusDisplayer.IsExpanded)
                        {
                            this.CharacterStatusDisplayer.IsExpanded = true;
                        }
                    });
        }

        #endregion
    }
}