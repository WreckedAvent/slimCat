using System;
using System.Collections.Generic;
using System.Linq;

namespace Slimcat.Utilities
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;

    using Slimcat.ViewModels;

    /// <summary>
    /// A filtered observable collection which syncronizes with another collection
    /// </summary>
    /// <typeparam name="T">The type of collection to filter</typeparam>
    /// <typeparam name="R">The type of collection to return</typeparam>
    public class FilteredCollection<T, R> : SysProp, IDisposable
        where T : R
    {
        #region Fields
        private Func<T, bool> meetsFilter;

        private bool isFiltering;

        private ObservableCollection<T> originalCollection;

        #endregion

        #region Constructors
        public FilteredCollection(ObservableCollection<T> toWatch, Func<T, bool> meetsFilter, bool isFiltering = false)
        {
            this.originalCollection = toWatch;
            this.meetsFilter = meetsFilter;
            this.Collection = new ObservableCollection<R>();

            this.originalCollection.CollectionChanged += this.OnCollectionChanged;
            this.isFiltering = isFiltering;
            this.RebuildItems();
        }
        #endregion

        #region Properties

        public ObservableCollection<R> Collection { get; private set; }

        public ObservableCollection<T> OriginalCollection
        {
            get
            {
                return this.originalCollection;
            }
            set
            {
                this.originalCollection.CollectionChanged -= this.OnCollectionChanged;
                this.originalCollection.Clear();
                this.originalCollection = value;
                this.originalCollection.CollectionChanged += this.OnCollectionChanged;
                this.RebuildItems();
            }
        }

        public bool IsFiltering
        {
            get
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

        #endregion

        #region Public Methods
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
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var items = e.NewItems.Cast<T>();
                    if (this.isFiltering) 
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

        #endregion
    }

    /// <summary>
    /// A filtered observable collection which syncronizes with another collection
    /// </summary>
    /// <typeparam name="T">The type of collection to filter and return</typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Same type as other class, only less specific.")]
    public class FilteredCollection<T> : FilteredCollection<T, T>
        where T : class
    {
        public FilteredCollection(ObservableCollection<T> toWatch, Func<T, bool> meetsFilter, bool isFiltering = false)
            : base(toWatch, meetsFilter, isFiltering)
        {
            
        } 
    }
}
