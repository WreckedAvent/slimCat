using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace Slimcat.Views
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    using System.Linq;

    using Slimcat.Models;
    using Slimcat.Utilities;

    /// <summary>
    /// Interaction logic for ObservingFlowDocumentReader.xaml
    /// </summary>
    public partial class ObservingFlowDocumentReader
    {
        #region Fields
        public static readonly DependencyProperty MessageSourceProperty =
            DependencyProperty.Register(
                "MessageSource", 
                typeof(ObservableCollection<IViewableObject>), 
                typeof(ObservingFlowDocumentReader), 
                new PropertyMetadata(default(ObservableCollection<object>), OnMessageSourceChanged));

        public static readonly DependencyProperty HistorySourceProperty =
            DependencyProperty.Register(
                "HistorySource",
                typeof(IEnumerable<string>),
                typeof(ObservingFlowDocumentReader),
                new PropertyMetadata(default(IEnumerable<string>)));

        public static readonly DependencyProperty LoadInReverseProperty =
                DependencyProperty.Register(
                "LoadInReverse", 
                typeof(bool), 
                typeof(ObservingFlowDocumentReader), 
                new PropertyMetadata(default(bool)));

        private KeepToCurrentScrollViewer scroller;

        private bool loaded;

        private int historyCount = 0;
        #endregion

        #region Constructors
        public ObservingFlowDocumentReader()
        {
            this.InitializeComponent();
        }
        #endregion

        #region Properties
        public ObservableCollection<IViewableObject> MessageSource
        {
            get
            {
                return (ObservableCollection<IViewableObject>)GetValue(MessageSourceProperty);
            }

            set
            {
                this.SetValue(MessageSourceProperty, value);
            }
        }

        public IEnumerable<string> HistorySource
        {
            get
            {
                return (IEnumerable<string>)GetValue(HistorySourceProperty);
            }

            set
            {
                this.SetValue(HistorySourceProperty, value);
            }
        }

        public bool LoadInReverse
        {
            get
            {
                return (bool)GetValue(LoadInReverseProperty);
            }
            set
            {
                SetValue(LoadInReverseProperty, value);
            }
        }
        #endregion

        #region Methods
        private static void OnMessageSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var @this = (ObservingFlowDocumentReader)o;

            var old = e.OldValue as ObservableCollection<IViewableObject>;
            var @new = e.NewValue as ObservableCollection<IViewableObject>;

            if (old != null)
            {
                old.CollectionChanged -= @this.OnMessagesUpdate;
            }

            if (@new != null)
            {
                @new.CollectionChanged += @this.OnMessagesUpdate;
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            this.scroller = this.scroller ?? new KeepToCurrentScrollViewer(Root);
            var historySource = this.HistorySource ?? new List<string>();
            IEnumerable<IViewableObject> messageSource = this.MessageSource;

            if (this.LoadInReverse)
            {
                messageSource
                    .Reverse()
                    .Select(x => x.View)
                    .Each(this.AddInReverseAsync);

                historySource
                    .Reverse()
                    .Select(x => new HistoryView { DataContext = x })
                    .Each(x =>
                    {
                        this.historyCount++;
                        this.AddInReverseAsync(x);
                    });

                this.loaded = true;
                return;
            }

            historySource
                .Select(x => new HistoryView { DataContext = x })
                .Each(x =>
                {
                    this.historyCount++;
                    this.AddAsync(x);
                });

            messageSource
                .Select(x => x.View)
                .Each(this.AddAsync);

            this.loaded = true;
        }

        private void OnMessagesUpdate(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!this.loaded)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    e.NewItems.Cast<IViewableObject>()
                        .Select(x => x.View)
                        .Each(x => this.AddAtAsync(e.NewStartingIndex + this.historyCount, x));
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.Messages.Blocks.Clear();
                    this.historyCount = 0;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    this.scroller.Stick();
                    this.Messages.Blocks.RemoveAt(e.OldStartingIndex + this.historyCount);
                    this.scroller.ScrollToStick();
                    break;
            }
        }

        private void AddInReverseAsync(Block item)
        {
            Dispatcher.BeginInvoke(
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

        private void AddAsync(Block item)
        {
            Dispatcher.BeginInvoke((Action)(() => this.Messages.Blocks.Add(item)));
        }

        private void AddAtAsync(int index, Block item)
        {
            Dispatcher.BeginInvoke((Action)(() => this.Messages.Blocks.AddAt(index, item)));
        }
        #endregion
    }
}
