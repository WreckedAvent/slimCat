#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsTabViewModel.cs">
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

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;
    using lib;
    using Libraries;
    using Microsoft.Practices.Prism;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using System.Windows.Input;
    using Newtonsoft.Json.Linq;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     This is the tab labled "search" in the channel bar, or the bar on the right-hand side
    /// </summary>
    public class SearchTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        public const string SearchTabView = "SearchTabView";

        #endregion

        #region Fields


        private RelayCommand addSearch;

        private RelayCommand removeSearch;

        private RelayCommand clearSearch;

        private readonly TimeSpan searchDebounce = TimeSpan.FromMilliseconds(250);


        private readonly ObservableCollection<SearchTermModel> selectedSearchTerms = new ObservableCollection<SearchTermModel>();

        private readonly ICollectionView selectedView;


        private readonly DeferredAction updateActiveViews;

        private readonly ObservableCollection<SearchTermModel> availableSearchTerms = new ObservableCollection<SearchTermModel>();

        private readonly ICollectionView availableView;


        private string searchString = string.Empty;
        #endregion

        #region Constructors and Destructors

        public SearchTabViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg,
            ICharacterManager manager, IBrowser browser)
            : base(contain, regman, eventagg, cm, manager)
        {
            Container.RegisterType<object, SearchTabView>(SearchTabView);

            var worker = new BackgroundWorker();
            worker.DoWork += (sender, args) => PopulateSearchTerms(browser.GetResponse(Constants.UrlConstants.SearchFields));
            worker.RunWorkerAsync();

            availableView = new ListCollectionView(availableSearchTerms);
            availableView.GroupDescriptions.Add(new PropertyGroupDescription("Category", new CategoryConverter()));
            availableView.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
            availableView.Filter = o => ((SearchTermModel) o).DisplayName.ContainsOrdinal(searchString);

            selectedView = new ListCollectionView(selectedSearchTerms);
            selectedView.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
            selectedView.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));

            updateActiveViews = DeferredAction.Create(availableView.Refresh);
        }

        #endregion

        #region Public Properties

        public ICommand AddSearchTermCommand
        {
            get { return addSearch ?? (addSearch = new RelayCommand(AddSearchTermEvent)); }
        }

        public ICommand RemoveSearchTermCommand
        {
            get { return removeSearch ?? (removeSearch = new RelayCommand(RemoveSearchTermEvent)); }
        }

        public ICommand ClearSearchTermsCommand
        {
            get { return clearSearch ?? (clearSearch = new RelayCommand(ClearSearchTermEvent)); }
        }

        public ICollectionView AvailableSearchTerms
        {
            get { return availableView; }
        }

        public ICollectionView SelectedSearchTerms
        {
            get { return selectedView; }
        }

        public ICommand OpenSearchSettingsCommand
        {
            get { return null; }
        }

        public string SearchString
        {
            get { return searchString; }
            set
            {
                searchString = value;
                OnPropertyChanged("SearchString");

                UpdateAvailableViews();
            }
        }

        public bool CanStartSearch
        {
            get { return selectedSearchTerms.Any(); }
        }

        #endregion

        #region Methods


        private void UpdateAvailableViews()
        {
            updateActiveViews.Defer(searchDebounce);
        }

        private void ClearSearchTermEvent(object obj)
        {
            availableSearchTerms.AddRange(selectedSearchTerms);
            selectedSearchTerms.Clear();
            OnPropertyChanged("CanStartSearch");
        }

        private void RemoveSearchTermEvent(object obj)
        {
            var term = (SearchTermModel) obj;
            selectedSearchTerms.Remove(term);

            availableSearchTerms.Add(term);

            OnPropertyChanged("CanStartSearch");
        }

        private void AddSearchTermEvent(object obj)
        {
            var term = (SearchTermModel)obj;
            availableSearchTerms.Remove(term);

            selectedSearchTerms.Add(term);

            OnPropertyChanged("CanStartSearch");
        }

        private void PopulateSearchTerms(string jsonString)
        {
            JToken token = JObject.Parse(jsonString);

            var kinks = token["kinks"].Select(x =>
                new SearchTermModel
                {
                    Category = "kink",
                    DisplayName = (string) x["name"],
                    UnderlyingValue = (string) x["fetish_id"]
                });

            var genders = SearchTermFromArray(token, "gender");

            var roles = SearchTermFromArray(token, "role");

            var orientations = SearchTermFromArray(token, "orientation");

            var positions = SearchTermFromArray(token, "position");

            var languages = SearchTermFromArray(token, "language");

            Dispatcher.BeginInvoke((Action)(() =>
                availableSearchTerms
                    .AddRange(kinks)
                    .AddRange(genders)
                    .AddRange(roles)
                    .AddRange(orientations)
                    .AddRange(positions)
                    .AddRange(languages)));
        }

        private static IEnumerable<SearchTermModel> SearchTermFromArray(JToken token, string category)
        {
            return token[category + "s"].Select(x =>
                new SearchTermModel
                {
                    Category = category,
                    DisplayName = (string) x
                });
        }
        #endregion

    }

    class CategoryConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var cased = CultureInfo.CurrentCulture.TextInfo.ToTitleCase((string)value);
            return cased + "s";
        }
    }
}