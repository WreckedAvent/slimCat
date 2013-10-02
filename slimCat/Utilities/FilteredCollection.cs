namespace Slimcat.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Slimcat.ViewModels;

    /// <summary>
    ///     A filtered observable collection which syncronizes with another collection
    /// </summary>
    /// <typeparam name="T">The type of collection to filter</typeparam>
    /// <typeparam name="TR">The type of collection to return</typeparam>
    public class FilteredCollection<T, TR> : SysProp, IDisposable
        where T : TR
    {
        #region Fields

        private bool isFiltering;

        private Func<T, bool> meetsFilter;

        private ObservableCollection<T> originalCollection;

        #endregion

        #region Constructors and Destructors

        public FilteredCollection(ObservableCollection<T> toWatch, Func<T, bool> meetsFilter, bool isFiltering = false)
        {
            this.originalCollection = toWatch;
            this.meetsFilter = meetsFilter;
            this.Collection = new ObservableCollection<TR>();

            this.originalCollection.CollectionChanged += this.OnCollectionChanged;
            this.isFiltering = isFiltering;
            this.RebuildItems();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<TR> Collection { get; private set; }

        public bool IsFiltering
        {
            private get
            {
                return this.isFiltering;
            }

            set
            {
                this.isFiltering = value;
                this.OnPropertyChanged("IsFiltering");
                this.RebuildItems();
            }
        }

        public ObservableCollection<T> OriginalCollection
        {
            get
            {
                return this.originalCollection;
            }

            set
            {
                if (this.originalCollection != null)
                {
                    this.originalCollection.CollectionChanged -= this.OnCollectionChanged;
                }

                this.originalCollection = value;

                if (this.originalCollection == null)
                {
                    return;
                }

                this.originalCollection.CollectionChanged += this.OnCollectionChanged;
                this.RebuildItems();
            }
        }

        #endregion

        #region Public Methods and Operators

        public void Dispose()
        {
            this.Dispose(true);
        }

        public void RebuildItems()
        {
            this.Collection.Clear();

            IEnumerable<T> items = this.originalCollection;
            if (this.isFiltering)
            {
                items = items.Where(this.meetsFilter);
            }

            items.Each(item => this.Collection.Add(item));
        }

        #endregion

        #region Methods

        private void Dispose(bool isManaged)
        {
            if (!isManaged)
            {
                return;
            }

            this.originalCollection = null;
            this.meetsFilter = null;
            this.Collection = null;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.Collection == null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var items = e.NewItems.Cast<T>();
                    if (this.IsFiltering)
                    {
                        items = items.Where(this.meetsFilter);
                    }

                    items.Each(item => this.Collection.Add(item));
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.Collection.Clear();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                    {
                        return;
                    }

                    this.Collection.RemoveAt(e.OldStartingIndex);
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    ///     A filtered observable collection which syncronizes with another collection
    /// </summary>
    /// <typeparam name="T">The type of collection to filter and return</typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", 
        Justification = "Same type as other class, only less specific.")]
    public class FilteredCollection<T> : FilteredCollection<T, T>
        where T : class
    {
        #region Constructors and Destructors

        public FilteredCollection(ObservableCollection<T> toWatch, Func<T, bool> meetsFilter, bool isFiltering = false)
            : base(toWatch, meetsFilter, isFiltering)
        {
        }

        #endregion
    }
}