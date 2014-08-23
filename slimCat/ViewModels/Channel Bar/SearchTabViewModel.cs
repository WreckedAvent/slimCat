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

    using Libraries;
    using Microsoft.Practices.Prism;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Newtonsoft.Json.Linq;
    using Services;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Timers;
    using System.Windows.Data;
    using System.Windows.Input;
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

        private RelayCommand sendSearch;

        private readonly TimeSpan searchDebounce = TimeSpan.FromMilliseconds(250);


        private readonly ObservableCollection<SearchTermModel> selectedSearchTerms = new ObservableCollection<SearchTermModel>();

        private readonly ICollectionView selectedView;


        private readonly DeferredAction updateActiveViews;

        private readonly ObservableCollection<SearchTermModel> availableSearchTerms = new ObservableCollection<SearchTermModel>();

        private readonly ICollectionView availableView;

        private string searchString = string.Empty;

        private bool isInSearchCoolDown = false;

        private readonly Timer chatSearchCooldownTimer = new Timer(5500);
        #endregion

        #region Constructors and Destructors

        public SearchTabViewModel(IChatState chatState, IBrowser browser)
            : base(chatState)
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

            chatSearchCooldownTimer.Elapsed += (sender, args) =>
            {
                isInSearchCoolDown = false;
                OnPropertyChanged("CanStartSearch");
                chatSearchCooldownTimer.Stop();
            };
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

        public ICommand SendSearchCommand
        {
            get { return sendSearch ?? (sendSearch = new RelayCommand(SendSearchEvent, param => CanStartSearch)); }
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
            get
            {
                if (isInSearchCoolDown)
                {
                    SearchButtonText = "Must wait 5 seconds";
                    OnPropertyChanged("SearchButtonText");
                    return false;
                }

                if (!selectedSearchTerms.Any(term => term.Category.Equals("kinks")))
                {
                    SearchButtonText = "Must have at least one kink";
                    OnPropertyChanged("SearchButtonText");
                    return false;
                }

                if (selectedSearchTerms.Count(term => term.Category.Equals("kinks")) > 5)
                {
                    SearchButtonText = "Too many kinks";
                    OnPropertyChanged("SearchButtonText");
                    return false;
                }

                SearchButtonText = "Start Chat Search";
                OnPropertyChanged("SearchButtonText");
                return true;
            }
        }

        public string SearchButtonText { get; set; }
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

        private void SendSearchEvent(object obj)
        {
            var toSend = new Dictionary<string, IList<string>>();
            
            selectedSearchTerms.Each(term =>
            {
                if (!toSend.ContainsKey(term.Category))
                    toSend[term.Category] = new List<string>();

                toSend[term.Category].Add(term.UnderlyingValue ?? term.DisplayName);
            });

            ChatConnection.SendMessage(toSend, "FKS");

            isInSearchCoolDown = true;
            chatSearchCooldownTimer.Start();
            OnPropertyChanged("CanStartSearch");
        }

        private void PopulateSearchTerms(string jsonString)
        {
            JToken token = JObject.Parse(jsonString);

            var kinks = token["kinks"].Select(x =>
                new SearchTermModel
                {
                    Category = "kinks",
                    DisplayName = WebUtility.HtmlDecode((string) x["name"]),
                    UnderlyingValue = (string) x["fetish_id"]
                });

            var genders = SearchTermFromArray(token, "genders");

            var roles = SearchTermFromArray(token, "roles");

            var orientations = SearchTermFromArray(token, "orientations");

            var positions = SearchTermFromArray(token, "positions");

            var languages = SearchTermFromArray(token, "languages");

            // oversight, this is not send per the endpoint, must hard-code
            var furryPrefs = new[]
            {
                "No furry characters, just humans", "No humans, just furry characters", "Furries ok, Humans Preferred",
                "Humans ok, Furries Preferred", "Furs and / or humans"
            }.Select(x => new SearchTermModel
            {
                Category = "furryprefs",
                DisplayName = x
            });

            Dispatcher.BeginInvoke((Action)(() =>
                availableSearchTerms
                    .AddRange(kinks)
                    .AddRange(genders)
                    .AddRange(roles)
                    .AddRange(orientations)
                    .AddRange(positions)
                    .AddRange(languages)
                    .AddRange(furryPrefs)));
        }

        private static IEnumerable<SearchTermModel> SearchTermFromArray(JToken token, string category)
        {
            return token[category].Select(x =>
                new SearchTermModel
                {
                    Category = category,
                    DisplayName = WebUtility.HtmlDecode((string) x)
                });
        }
        #endregion

    }

    class CategoryConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var toReturn = CultureInfo.CurrentCulture.TextInfo.ToTitleCase((string)value);

            return toReturn.Equals("Furryprefs") ? "Furry Preference" : toReturn;
        }
    }
}