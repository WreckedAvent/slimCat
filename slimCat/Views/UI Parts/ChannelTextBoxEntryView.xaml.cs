#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelTextBoxEntryView.xaml.cs">
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

namespace slimCat.Views
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Markup;
    using Models;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for ChannelTextBoxEntryView.xaml
    /// </summary>
    public partial class ChannelTextBoxEntryView
    {
        #region Static Fields

        private static readonly IDictionary<Key, Tuple<string, bool>> AcceptedKeys =
            new Dictionary<Key, Tuple<string, bool>>
                {
                    // accepted shortcut keys.
                    {Key.B, new Tuple<string, bool>("b", false)},
                    {Key.S, new Tuple<string, bool>("s", false)},
                    {Key.I, new Tuple<string, bool>("i", false)},
                    {Key.U, new Tuple<string, bool>("u", false)},
                    {Key.L, new Tuple<string, bool>("url", true)},
                    {Key.Up, new Tuple<string, bool>("sup", false)},
                    {Key.Down, new Tuple<string, bool>("sub", false)},
                    {Key.O, new Tuple<string, bool>("icon", false)},
                    {Key.P, new Tuple<string, bool>("user", false)}

                    // format: 
                    // target key, matching bbtag, if the bbtag takes arguments
                };

        #endregion

        #region Fields

        private ViewModelBase vm;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelTextBoxEntryView" /> class.
        /// </summary>
        public ChannelTextBoxEntryView()
        {
            InitializeComponent();

            Entry.FocusableChanged += (s, e) =>
                {
                    if ((bool) e.NewValue)
                        Entry.Focus();
                };

            Entry.Language = XmlLanguage.GetLanguage(ApplicationSettings.Langauge);
            vm = DataContext as ViewModelBase;

            if (vm != null)
                vm.PropertyChanged += PropertyChanged;

            DataContextChanged += (sender, args) =>
                {
                    if (vm != null)
                        vm.PropertyChanged -= PropertyChanged;

                    vm = DataContext as ViewModelBase;

                    if (vm != null)
                        vm.PropertyChanged += PropertyChanged;
                };
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != "Language")
                return;

            Dispatcher.BeginInvoke(
                (Action) delegate { Entry.Language = XmlLanguage.GetLanguage(ApplicationSettings.Langauge); });
        }

        #endregion

        #region Methods

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // this defines shortcuts when the textbox has focus --- in particular, ones which modify the content of the textbox
            if (e.Key == Key.Return && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                var caretIndex = Entry.CaretIndex;
                Entry.Text = Entry.Text.Insert(caretIndex, "\r");
                Entry.CaretIndex = caretIndex + 1;
            }
            else if (e.Key == Key.Return)
                e.Handled = true; // don't do the funny business with inserting a new line
            else if (AcceptedKeys.ContainsKey(e.Key) && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;

                var tupleData = AcceptedKeys[e.Key];

                var bbtag = tupleData.Item1;
                var useArgs = tupleData.Item2;

                if (!string.IsNullOrWhiteSpace(Entry.SelectedText))
                {
                    var selected = Entry.SelectedText;

                    if (!useArgs)
                        Entry.SelectedText = string.Format("[{0}]{1}[/{0}]", bbtag, selected);
                    else
                    {
                        var toEnter = string.Format("[{0}={1}]", bbtag, selected);
                        var caretIndex = Entry.CaretIndex;

                        Entry.SelectedText = string.Format("{0}[/{1}]", toEnter, bbtag);
                        Entry.CaretIndex = caretIndex + toEnter.Length;
                    }
                }
                else
                {
                    var caretIndex = Entry.CaretIndex;

                    Entry.Text = Entry.Text.Insert(caretIndex, string.Format("[{0}][/{0}]", bbtag));
                    Entry.CaretIndex = caretIndex + bbtag.Length + 2;

                    // 2 is a magic number representing the brackets around the BbCode
                }
            }
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!ApplicationSettings.AllowGreedyTextboxFocus)
            {
                var element = FocusManager.GetFocusedElement(Application.Current.MainWindow);
                if (element is ListBoxItem) return;
            }

            Entry.Focus();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.Control) return;

            if (e.Key == Key.Up || e.Key == Key.Down) e.Handled = true;
        }
        #endregion
    }
}