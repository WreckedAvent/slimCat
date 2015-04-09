#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchTabViewModel.cs">
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

namespace slimCat.ViewModels
{
    #region Usings

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
    using Libraries;
    using Microsoft.Practices.Prism;
    using Microsoft.Practices.Unity;
    using Models;
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
        private readonly IBrowser browser;

        #region Constants

        public const string SearchTabView = "SearchTabView";

        #endregion

        #region Fields

        private ObservableCollection<SearchTermModel> availableSearchTerms =
            new ObservableCollection<SearchTermModel>();

        private readonly Timer chatSearchCooldownTimer = new Timer(5500);

        private ObservableCollection<SearchTermModel> selectedSearchTerms =
            new ObservableCollection<SearchTermModel>();

        private DeferredAction updateActiveViews;
        private readonly DeferredAction saveTerms;
        private RelayCommand addSearch;
        private RelayCommand clearSearch;

        private bool isInSearchCoolDown;
        private RelayCommand removeSearch;
        private string searchString = string.Empty;
        private RelayCommand sendSearch;

        #endregion

        #region Constructors and Destructors

        public SearchTabViewModel(IChatState chatState, IBrowser browser)
            : base(chatState)
        {
            this.browser = browser;
            Container.RegisterType<object, SearchTabView>(SearchTabView);

            chatSearchCooldownTimer.Elapsed += (sender, args) =>
            {
                isInSearchCoolDown = false;
                OnPropertyChanged("CanStartSearch");
                chatSearchCooldownTimer.Stop();
            };

            saveTerms =
                DeferredAction.Create(
                    () => SettingsService.SaveSearchTerms(ChatModel.CurrentCharacter.Name, new SearchTermsModel
                    {
                        AvailableTerms = availableSearchTerms.ToList(),
                        SelectedTerms = selectedSearchTerms.ToList()
                    }));

            if (ChatModel.CurrentCharacter == null) return;
            GetSearchTerms(ChatModel.CurrentCharacter.Name);
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

        public ICollectionView AvailableSearchTerms { get; private set; }

        public ICollectionView SelectedSearchTerms { get; private set; }

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
            updateActiveViews.Defer(Constants.SearchDebounce);
        }

        private void ClearSearchTermEvent(object obj)
        {
            availableSearchTerms.AddRange(selectedSearchTerms);
            selectedSearchTerms.Clear();
            OnPropertyChanged("CanStartSearch");
            SaveSearchTerms();
        }

        private void RemoveSearchTermEvent(object obj)
        {
            var term = (SearchTermModel) obj;
            selectedSearchTerms.Remove(term);

            availableSearchTerms.Add(term);

            OnPropertyChanged("CanStartSearch");
            SaveSearchTerms();
        }

        private void GetSearchTerms(string character)
        {
            var cache = SettingsService.RetrieveTerms(character);

            if (cache == null)
            {
                var worker = new BackgroundWorker();
                worker.DoWork +=
                    (sender, args) => PopulateSearchTerms(browser.GetResponse(Constants.UrlConstants.SearchFields, true));
                worker.RunWorkerAsync();
            }
            else
            {
                availableSearchTerms = new ObservableCollection<SearchTermModel>(cache.AvailableTerms);
                selectedSearchTerms = new ObservableCollection<SearchTermModel>(cache.SelectedTerms);
            }

            AvailableSearchTerms = new ListCollectionView(availableSearchTerms);
            AvailableSearchTerms.GroupDescriptions.Add(new PropertyGroupDescription("Category", new CategoryConverter()));
            AvailableSearchTerms.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
            AvailableSearchTerms.Filter = o => ((SearchTermModel)o).DisplayName.ContainsOrdinal(searchString);

            SelectedSearchTerms = new ListCollectionView(selectedSearchTerms);
            SelectedSearchTerms.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
            SelectedSearchTerms.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));

            updateActiveViews = DeferredAction.Create(AvailableSearchTerms.Refresh);
            OnPropertyChanged("AvailableSearchTerms");
            OnPropertyChanged("SelectedSearchTerms");
        }

        private void AddSearchTermEvent(object obj)
        {
            var term = (SearchTermModel) obj;
            availableSearchTerms.Remove(term);

            selectedSearchTerms.Add(term);

            OnPropertyChanged("CanStartSearch");
            SaveSearchTerms();
        }

        private void SendSearchEvent(object obj)
        {
            var toSend = new Dictionary<string, IList<string>>();
            toSend["kinks"] = new List<string>();

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

        private void SaveSearchTerms()
        {
            saveTerms.Defer(TimeSpan.FromSeconds(3));
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
            }).Union(new List<SearchTermModel>
            {
                new SearchTermModel
                {
                    Category = "furryprefs",
                    DisplayName = "No furry preference set",
                    UnderlyingValue = "None"
                }
            });

            Dispatcher.BeginInvoke((Action) (() =>
            {
                availableSearchTerms = new ObservableCollection<SearchTermModel>();
                availableSearchTerms
                    .AddRange(kinks)
                    .AddRange(genders)
                    .AddRange(roles)
                    .AddRange(orientations)
                    .AddRange(positions)
                    .AddRange(languages)
                    .AddRange(furryPrefs);

                selectedSearchTerms = new ObservableCollection<SearchTermModel>();

                OnPropertyChanged("AvailableSearchTerms");
                OnPropertyChanged("SelectedSearchTerms");

                AvailableSearchTerms.Refresh();
                SelectedSearchTerms.Refresh();

                SettingsService.SaveSearchTerms(ChatModel.CurrentCharacter.Name, new SearchTermsModel
                {
                    AvailableTerms = availableSearchTerms.ToList()
                });
            }));
        }

        private static IEnumerable<SearchTermModel> SearchTermFromArray(JToken token, string category)
        {
            return token[category].Select(x =>
                new SearchTermModel
                {
                    Category = category,
                    DisplayName = WebUtility.HtmlDecode((string) x)
                }).Union(new List<SearchTermModel>
                {
                    new SearchTermModel
                    {
                        Category = category,
                        DisplayName = "No " + category.Substring(0, category.Length - 1) + " set",
                        UnderlyingValue = "None"
                    }
                });
        }

        #endregion
    }

    internal class CategoryConverter : OneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var toReturn = CultureInfo.CurrentCulture.TextInfo.ToTitleCase((string) value);

            return toReturn.Equals("Furryprefs") ? "Furry Preference" : toReturn;
        }
    }
}