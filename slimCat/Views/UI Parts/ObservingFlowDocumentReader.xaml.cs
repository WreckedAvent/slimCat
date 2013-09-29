using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Slimcat.Views
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    using Slimcat.Models;
    using Slimcat.Utilities;

    using System.Linq;

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

            if (this.HistorySource != null)
            {
                this.HistorySource.Select(x => new HistoryView { DataContext = x }).Each(this.AddAsync);
            }

            if (this.MessageSource != null)
            {
                this.MessageSource.Select(x => x.View).Each(this.AddAsync);
            }

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
                        .Each(x => this.AddAtAsync(e.NewStartingIndex, x));
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.Messages.Blocks.Clear();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    this.Messages.Blocks.RemoveAt(e.OldStartingIndex + this.historyCount);
                    break;
            }
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
