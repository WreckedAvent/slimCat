#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelTextBoxEntryView.xaml.cs">
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
    using Libraries;
    using Models;
    using Utilities;
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
                {Key.N, new Tuple<string, bool>("noparse", false)},
                {Key.L, new Tuple<string, bool>("url", true)},
                {Key.Up, new Tuple<string, bool>("sup", false)},
                {Key.Down, new Tuple<string, bool>("sub", false)},
                {Key.O, new Tuple<string, bool>("icon", false)},
                {Key.K, new Tuple<string, bool>("channel", false)},
                {Key.J, new Tuple<string, bool>("color", false)}
                // format: 
                // target key, matching bbtag, if the bbtag takes arguments
            };

        private static readonly IDictionary<Key, string> TogglingKeys =
            new Dictionary<Key, string>
            {
                {Key.K, "session"},
                {Key.O, "user"},
            };

        #endregion

        #region Fields

        private static bool bindingsAdded;

        private static readonly IList<KeyBinding> lastBinds = new List<KeyBinding>(); 

        private RelayCommand focusCommand;
        private ChannelViewModelBase vm;

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
                if (!(bool) e.NewValue) return;

                Entry.Focus();
            };

            Entry.Language = XmlLanguage.GetLanguage(ApplicationSettings.Langauge);
            vm = DataContext as ChannelViewModelBase;

            if (vm != null)
                vm.PropertyChanged += PropertyChanged;

            DataContextChanged += (sender, args) =>
            {
                if (vm != null)
                    vm.PropertyChanged -= PropertyChanged;

                vm = DataContext as ChannelViewModelBase;

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

        public ICommand FocusCommand
        {
            get { return focusCommand = focusCommand ?? (focusCommand = new RelayCommand(_ => OnLoaded(null, null))); }
        }

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
            else if (e.Key == Key.Up && string.IsNullOrEmpty(vm.Message) && !string.IsNullOrWhiteSpace(vm.LastMessage) &&
                     e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                vm.Message = vm.LastMessage;
                Entry.ScrollToEnd();
                Entry.CaretIndex = vm.Message.Length;
            }
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
                    {
                        if (TogglingKeys.ContainsKey(e.Key))
                        {
                            var altbbtag = TogglingKeys[e.Key];

                            var openingBb = "[{0}]".FormatWith(bbtag);
                            var openingAltBb = "[{0}]".FormatWith(altbbtag);
                            var closingBb = "[/{0}]".FormatWith(bbtag);
                            var closingAltBb = "[/{0}]".FormatWith(altbbtag);

                            if (selected.Contains(openingBb))
                            {
                                Entry.SelectedText = selected.Replace(openingBb, openingAltBb)
                                    .Replace(closingBb, closingAltBb);
                                return;
                            }
                            if (selected.Contains(openingAltBb))
                            {
                                Entry.SelectedText = selected.Replace(openingAltBb, openingBb)
                                    .Replace(closingAltBb, closingBb);
                                return;
                            }
                        }
                        if (Entry.SelectedText.Contains("[{0}]".FormatWith(bbtag)) &&
                            TogglingKeys.ContainsKey(e.Key))
                        {
                            Entry.SelectedText = Entry.SelectedText.Replace("[{0}]".FormatWith(bbtag),
                                "[{0}]".FormatWith(TogglingKeys[e.Key]));
                            Entry.SelectedText = Entry.SelectedText.Replace("[/{0}]".FormatWith(bbtag),
                                "[/{0}]".FormatWith(TogglingKeys[e.Key]));
                        }

                        Entry.SelectedText = string.Format("[{0}]{1}[/{0}]", bbtag, selected);
                    }
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
                    var bbfragment = "[{0}][/{0}]".FormatWith(bbtag);
                    if (TogglingKeys.ContainsKey(e.Key) && caretIndex > 0)
                    {
                        var altbbtag = TogglingKeys[e.Key];

                        var altbbfragment = "[{0}][/{0}]".FormatWith(altbbtag);

                        if (Entry.Text.Contains(bbfragment))
                        {
                            Entry.Text = ReplaceFirst(Entry.Text, bbfragment, altbbfragment);
                            Entry.CaretIndex = caretIndex - (bbtag.Length - altbbtag.Length);
                            return;
                        }

                        if (Entry.Text.Contains(altbbtag))
                        {
                            Entry.Text = ReplaceFirst(Entry.Text, altbbfragment, bbfragment);
                            Entry.CaretIndex = caretIndex - (altbbtag.Length - bbtag.Length);
                            return;
                        }
                    }

                    Entry.Text = Entry.Text.Insert(caretIndex, bbfragment);
                    Entry.CaretIndex = caretIndex + bbtag.Length + 2;

                    // 2 is a magic number representing the brackets around the BbCode
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (vm != null)
            {
                var bindings = Application.Current.MainWindow.InputBindings;

                if (!bindingsAdded)
                {
                    bindings.Add(new KeyBinding(vm.NavigateUpCommand, Key.Up, ModifierKeys.Alt));
                    bindings.Add(new KeyBinding(vm.NavigateDownCommand, Key.Down, ModifierKeys.Alt));
                    bindings.Add(new KeyBinding(vm.NavigateUpCommand, Key.Tab, ModifierKeys.Control));
                    bindings.Add(new KeyBinding(vm.NavigateDownCommand, Key.Tab, ModifierKeys.Control | ModifierKeys.Shift));

                    ReAddContextualKeybinds();

                    bindingsAdded = true;
                }
                else
                {
                    lastBinds.Each(bindings.Remove);
                    lastBinds.Clear();

                    ReAddContextualKeybinds();
                }
            }

            if (!ApplicationSettings.AllowGreedyTextboxFocus)
            {
                var element = FocusManager.GetFocusedElement(Application.Current.MainWindow);
                if (element is ListBoxItem) return;
            }

            Entry.Focus();
            Entry.ScrollToEnd();
            if (!string.IsNullOrEmpty(vm.Message)) Entry.CaretIndex = vm.Message.Length;
        }

        private void ReAddContextualKeybinds()
        {
            AddContextualBinding(new KeyBinding(FocusCommand, new KeyGesture(Key.Tab)));
            AddContextualBinding(new KeyBinding(vm.TogglePreviewCommand, new KeyGesture(Key.Enter, ModifierKeys.Alt)));

            var pmVm = vm as PmChannelViewModel;
            var channelVm = vm as GeneralChannelViewModel;
            if (pmVm != null)
            {
                AddContextualBinding(new KeyBinding(pmVm.SwitchCommand, new KeyGesture(Key.Tab, ModifierKeys.Shift)));
            }

            if (channelVm != null)
            {
                AddContextualBinding(new KeyBinding(channelVm.SwitchCommand, new KeyGesture(Key.Tab, ModifierKeys.Shift)));

                AddContextualBinding(new KeyBinding(channelVm.SwitchSearchCommand,
                    new KeyGesture(Key.F, ModifierKeys.Control)));
            }
        }

        private void AddContextualBinding(KeyBinding binding)
        {
            lastBinds.Add(binding);
            Application.Current.MainWindow.InputBindings.Add(binding);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.Control) return;

            if (e.Key == Key.Up || e.Key == Key.Down) e.Handled = true;
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            var pos = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
                return text;

            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        #endregion
    }
}