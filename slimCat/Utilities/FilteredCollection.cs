#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilteredCollection.cs">
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

namespace Slimcat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using ViewModels;

    #endregion

    /// <summary>
    ///     A filtered observable collection which syncronizes with another collection
    /// </summary>
    /// <typeparam name="T">The type of collection to filter</typeparam>
    /// <typeparam name="TR">The type of collection to return</typeparam>
    public class FilteredCollection<T, TR> : SysProp
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
            originalCollection = toWatch;
            this.meetsFilter = meetsFilter;
            Collection = new ObservableCollection<TR>();

            originalCollection.CollectionChanged += OnCollectionChanged;
            this.isFiltering = isFiltering;
            RebuildItems();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<TR> Collection { get; private set; }

        public bool IsFiltering
        {
            private get { return isFiltering; }

            set
            {
                isFiltering = value;
                OnPropertyChanged("IsFiltering");
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
            Collection.Clear();

            IEnumerable<T> items = originalCollection;
            if (isFiltering)
                items = items.Where(meetsFilter);

            items.Each(item => Collection.Add(item));
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                originalCollection = null;
                meetsFilter = null;
                Collection = null;
            }

            base.Dispose(isManaged);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Collection == null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var items = e.NewItems.Cast<T>();
                    if (IsFiltering)
                        items = items.Where(meetsFilter);

                    items.Each(item => Collection.Add(item));
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Collection.Clear();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        return;

                    Collection.RemoveAt(e.OldStartingIndex);
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