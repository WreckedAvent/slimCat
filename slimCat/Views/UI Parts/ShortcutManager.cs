#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchBoxView.xaml.cs">
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
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Libraries;
    using Models;
    using Utilities;
    using ViewModels;
    #endregion

    public class ShortcutManager
    {
        #region Fields
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

        private readonly TextBox entry;
        private readonly ChannelViewModelBase vm;
        private readonly GenericSearchSettingsModel model;

        private static bool bindingsAdded;

        private static readonly IList<KeyBinding> LastBinds = new List<KeyBinding>();

        private RelayCommand focusCommand;
        #endregion

        #region Constructors
        public ShortcutManager(TextBox entry, ChannelViewModelBase vm)
            : this(entry)
        {
            this.vm = vm;
        }

        public ShortcutManager(TextBox entry, GenericSearchSettingsModel model)
            : this (entry)
        {
            this.model = model;
        }

        public ShortcutManager(TextBox entry)
        {
            this.entry = entry;
            entry.FocusableChanged += (s, e) =>
            {
                if (!(bool)e.NewValue) return;

                entry.Focus();
            };

            entry.IsVisibleChanged += OnIsVisibleChanged;
            entry.Loaded += OnLoaded;
            entry.KeyUp += OnKeyUp;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (entry.IsVisible)
                OnLoaded(sender, null);
        }

        #endregion

        #region Commands
        public ICommand FocusCommand
        {
            get { return focusCommand = focusCommand ?? (focusCommand = new RelayCommand(_ => Focus())); }
        }
        #endregion

        #region Methods
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // this defines shortcuts when the textbox has focus --- in particular, ones which modify the content of the textbox
            if (e.Key == Key.Return && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                var caretIndex = entry.CaretIndex;
                entry.Text = entry.Text.Insert(caretIndex, "\r");
                entry.CaretIndex = caretIndex + 1;
            }
            else if (e.Key == Key.Return)
                e.Handled = true; // don't do the funny business with inserting a new line
            else if (e.Key == Key.Up && String.IsNullOrEmpty(vm.Message) && !String.IsNullOrWhiteSpace(vm.LastMessage) &&
                     e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                vm.Message = vm.LastMessage;
                entry.ScrollToEnd();
                entry.CaretIndex = vm.Message.Length;
            }
            else if (AcceptedKeys.ContainsKey(e.Key) && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;

                var tupleData = AcceptedKeys[e.Key];

                var bbtag = tupleData.Item1;
                var useArgs = tupleData.Item2;

                if (!String.IsNullOrWhiteSpace(entry.SelectedText))
                {
                    var selected = entry.SelectedText;

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
                                entry.SelectedText = selected.Replace(openingBb, openingAltBb)
                                    .Replace(closingBb, closingAltBb);
                                return;
                            }
                            if (selected.Contains(openingAltBb))
                            {
                                entry.SelectedText = selected.Replace(openingAltBb, openingBb)
                                    .Replace(closingAltBb, closingBb);
                                return;
                            }
                        }
                        if (entry.SelectedText.Contains("[{0}]".FormatWith(bbtag)) &&
                            TogglingKeys.ContainsKey(e.Key))
                        {
                            entry.SelectedText = entry.SelectedText.Replace("[{0}]".FormatWith(bbtag),
                                "[{0}]".FormatWith(TogglingKeys[e.Key]));
                            entry.SelectedText = entry.SelectedText.Replace("[/{0}]".FormatWith(bbtag),
                                "[/{0}]".FormatWith(TogglingKeys[e.Key]));
                        }

                        entry.SelectedText = String.Format("[{0}]{1}[/{0}]", bbtag, selected);
                    }
                    else
                    {
                        var toEnter = String.Format("[{0}={1}]", bbtag, selected);
                        var caretIndex = entry.CaretIndex;

                        entry.SelectedText = String.Format("{0}[/{1}]", toEnter, bbtag);
                        entry.CaretIndex = caretIndex + toEnter.Length;
                    }
                }
                else
                {
                    var caretIndex = entry.CaretIndex;
                    var bbfragment = "[{0}][/{0}]".FormatWith(bbtag);
                    if (TogglingKeys.ContainsKey(e.Key) && caretIndex > 0)
                    {
                        var altbbtag = TogglingKeys[e.Key];

                        var altbbfragment = "[{0}][/{0}]".FormatWith(altbbtag);

                        if (entry.Text.Contains(bbfragment))
                        {
                            entry.Text = ReplaceFirst(entry.Text, bbfragment, altbbfragment);
                            entry.CaretIndex = caretIndex - (bbtag.Length - altbbtag.Length);
                            return;
                        }

                        if (entry.Text.Contains(altbbtag))
                        {
                            entry.Text = ReplaceFirst(entry.Text, altbbfragment, bbfragment);
                            entry.CaretIndex = caretIndex - (altbbtag.Length - bbtag.Length);
                            return;
                        }
                    }

                    entry.Text = entry.Text.Insert(caretIndex, bbfragment);
                    entry.CaretIndex = caretIndex + bbtag.Length + 2;

                    // 2 is a magic number representing the brackets around the BbCode
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var bindings = Application.Current.MainWindow.InputBindings;

            if (!entry.IsVisible) return;
            if (vm == null && model == null) return;

            Focus();

            if (model != null)
            {
                // update focus shortcut to the new searchbox
                var focusBind = LastBinds.FirstOrDefault(x => x.Key == Key.Tab && x.Modifiers == 0);
                if (focusBind != null)
                {
                    bindings.Remove(focusBind);
                    LastBinds.Remove(focusBind);
                }
                AddContextualBinding(new KeyBinding(FocusCommand, new KeyGesture(Key.Tab)));
                return;
            }


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
                LastBinds.Each(bindings.Remove);
                LastBinds.Clear();

                ReAddContextualKeybinds();
            }

            if (!ApplicationSettings.AllowGreedyTextboxFocus)
            {
                var element = FocusManager.GetFocusedElement(Application.Current.MainWindow);
                if (element is ListBoxItem) return;
            }

            if (!String.IsNullOrEmpty(vm.Message)) entry.CaretIndex = vm.Message.Length;
        }

        private void Focus()
        {
            entry.Focus();
            entry.ScrollToEnd();
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

        private static void AddContextualBinding(KeyBinding binding)
        {
            LastBinds.Add(binding);
            Application.Current.MainWindow.InputBindings.Add(binding);
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
