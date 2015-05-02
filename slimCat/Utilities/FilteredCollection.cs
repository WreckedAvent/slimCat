#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilteredCollection.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Models;
    using ViewModels;

    #endregion

    /// <summary>
    ///     A filtered observable collection which synchronizes with another collection
    /// </summary>
    /// <typeparam name="T">The type of collection to filter</typeparam>
    /// <typeparam name="TR">The type of collection to return</typeparam>
    public class FilteredCollection<T, TR> : SysProp
        where T : TR
    {
        #region Fields

        protected readonly Func<T, bool> ActiveFilter;

        protected readonly object Locker = new object();
        private bool isFiltering;

        private ObservableCollection<T> originalCollection;

        #endregion

        #region Constructors and Destructors

        public FilteredCollection(ObservableCollection<T> toWatch, Func<T, bool> activeFilter, bool isFiltering = false)
        {
            originalCollection = toWatch;
            ActiveFilter = activeFilter;
            Collection = new ObservableCollection<TR>();

            originalCollection.CollectionChanged += OnCollectionChanged;
            this.isFiltering = isFiltering;
            RebuildItems();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<TR> Collection { get; }

        public bool IsFiltering
        {
            get { return isFiltering; }

            set
            {
                isFiltering = value;
                OnPropertyChanged();
                RebuildItems();
            }
        }

        public ObservableCollection<T> OriginalCollection
        {
            get { return originalCollection; }

            set
            {
                if (originalCollection != null)
                    originalCollection.CollectionChanged -= OnCollectionChanged;

                originalCollection = value;

                if (originalCollection == null)
                    return;

                originalCollection.CollectionChanged += OnCollectionChanged;
                RebuildItems();
            }
        }

        #endregion

        #region Public Methods and Operators

        public void RebuildItems()
        {
            lock (Locker)
            {
                Collection.Clear();

                IEnumerable<T> items = originalCollection;

                items = items.Where(MeetsFilter);

                items.Each(item => Collection.Add(item));
            }
        }

        #endregion

        #region Methods

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Collection == null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var items = e.NewItems.Cast<T>();
                    if (IsFiltering)
                        items = items.Where(ActiveFilter);

                    items.Each(item => Collection.Add(item));
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    Collection.Clear();
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    e.OldItems.OfType<T>().Each(item => Collection.Remove(item));
                    break;
                }
            }
        }

        protected virtual bool MeetsFilter(T item)
        {
            return !isFiltering || ActiveFilter(item);
        }

        #endregion
    }

    /// <summary>
    ///     A filtered observable collection which syncronizes with another collection
    /// </summary>
    /// <typeparam name="T">The type of collection to filter and return</typeparam>
    public class FilteredCollection<T> : FilteredCollection<T, T>
        where T : class
    {
        #region Constructors and Destructors

        public FilteredCollection(ObservableCollection<T> toWatch, Func<T, bool> activeFilter, bool isFiltering = false)
            : base(toWatch, activeFilter, isFiltering)
        {
        }

        #endregion
    }

    public class FilteredMessageCollection : FilteredCollection<IMessage, IViewableObject>
    {
        private readonly Func<IMessage, bool> constantFilter;

        public FilteredMessageCollection(ObservableCollection<IMessage> toWatch, Func<IMessage, bool> activeFilter,
            Func<IMessage, bool> constantFilter, bool isFiltering = false)
            : base(toWatch, activeFilter, isFiltering)
        {
            this.constantFilter = constantFilter;
        }

        protected override bool MeetsFilter(IMessage item)
        {
            if (item == null) return false;

            return IsFiltering ? ActiveFilter(item) : constantFilter != null && constantFilter(item);
        }
    }
}