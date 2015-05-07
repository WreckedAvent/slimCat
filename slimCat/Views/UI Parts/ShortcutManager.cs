#region Copyright

// <copyright file="ShortcutManager.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
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
    using System.Windows.Threading;
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
                {Key.O, "user"}
            };

        private readonly TextBox entry;
        private readonly ChannelViewModelBase vm;
        private readonly GenericSearchSettingsModel model;

        private static bool bindingsAdded;

        private static readonly IList<KeyBinding> LastBinds = new List<KeyBinding>();

        private RelayCommand focusCommand;

        private string lastNameComplete;

        #endregion

        #region Constructors

        public ShortcutManager(TextBox entry, ChannelViewModelBase vm)
            : this(entry)
        {
            this.vm = vm;
        }

        public ShortcutManager(TextBox entry, GenericSearchSettingsModel model)
            : this(entry)
        {
            this.model = model;
        }

        public ShortcutManager(TextBox entry)
        {
            this.entry = entry;
            entry.FocusableChanged += (s, e) =>
            {
                if (!(bool) e.NewValue) return;

                entry.Focus();
            };

            entry.IsVisibleChanged += OnIsVisibleChanged;
            entry.Loaded += OnLoaded;
            entry.KeyUp += OnKeyUp;
        }

        private void OnIsVisibleChanged(object sender,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (entry.IsVisible)
                OnLoaded(sender, null);
        }

        #endregion

        #region Commands

        public ICommand FocusCommand
        {
            get { return focusCommand = focusCommand ?? (focusCommand = new RelayCommand(FocusEvent)); }
        }

        private void FocusEvent(object o)
        {
            if (entry.IsFocused)
            {
                if (vm == null) return;
                var character = vm.TabComplete(lastNameComplete);

                lastNameComplete = character;
                if (character == null) return;

                // why invoke? Entry.Text doesn't update immediately, it's updated on a scheduler or something
                // this just schedules it after that
                Dispatcher.CurrentDispatcher.BeginInvoke((Action) (() =>
                {
                    var startIndex = entry.Text.IndexOf(character, StringComparison.Ordinal);
                    if (startIndex == -1) return; // don't want to crash if something weird happens
                    entry.Select(startIndex, character.Length);
                }));
            }
            else Focus();
        }

        #endregion

        #region Methods

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab) lastNameComplete = null;

            // this defines shortcuts when the textbox has focus --- in particular, ones which modify the content of the textbox
            if (e.Key == Key.Return && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                var caretIndex = entry.CaretIndex;
                entry.Text = entry.Text.Insert(caretIndex, "\r");
                entry.CaretIndex = caretIndex + 1;
            }
            else if (e.Key == Key.Return)
                e.Handled = true; // don't do the funny business with inserting a new line
            else if (e.Key == Key.Up && string.IsNullOrEmpty(vm.Message) && !string.IsNullOrWhiteSpace(vm.LastMessage) &&
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

                if (!string.IsNullOrWhiteSpace(entry.SelectedText))
                {
                    var selected = entry.SelectedText;

                    if (!useArgs)
                    {
                        if (TogglingKeys.ContainsKey(e.Key))
                        {
                            var altbbtag = TogglingKeys[e.Key];

                            var openingBb = $"[{bbtag}]";
                            var openingAltBb = $"[{altbbtag}]";
                            var closingBb = $"[/{bbtag}]";
                            var closingAltBb = $"[/{altbbtag}]";

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
                        if (entry.SelectedText.Contains($"[{bbtag}]") && TogglingKeys.ContainsKey(e.Key))
                        {
                            entry.SelectedText = entry.SelectedText.Replace($"[{bbtag}]", $"[{TogglingKeys[e.Key]}]");
                            entry.SelectedText = entry.SelectedText.Replace($"[/{bbtag}]",
                                $"[/{TogglingKeys[e.Key]}]");
                        }

                        entry.SelectedText = $"[{bbtag}]{selected}[/{bbtag}]";
                    }
                    else
                    {
                        var toEnter = $"[{bbtag}={selected}]";
                        var caretIndex = entry.CaretIndex;

                        entry.SelectedText = $"{toEnter}[/{bbtag}]";
                        entry.CaretIndex = caretIndex + toEnter.Length;
                    }
                }
                else
                {
                    var caretIndex = entry.CaretIndex;
                    var bbfragment = string.Format("[{0}][/{0}]", bbtag);
                    if (TogglingKeys.ContainsKey(e.Key) && caretIndex > 0)
                    {
                        var altbbtag = TogglingKeys[e.Key];

                        var altbbfragment = string.Format("[{0}][/{0}]", altbbtag);

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
                AddContextualBinding(FocusCommand, new KeyGesture(Key.Tab));
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

            if (!string.IsNullOrEmpty(vm.Message)) entry.CaretIndex = vm.Message.Length;
        }

        private void Focus()
        {
            entry.Focus();
            entry.ScrollToEnd();
        }

        private void ReAddContextualKeybinds()
        {
            AddContextualBinding(FocusCommand, new KeyGesture(Key.Tab));
            AddContextualBinding(vm.TogglePreviewCommand, new KeyGesture(Key.Enter, ModifierKeys.Alt));

            var pmVm = vm as PmChannelViewModel;
            var channelVm = vm as GeneralChannelViewModel;
            if (pmVm != null)
            {
                AddContextualBinding(pmVm.SwitchCommand, new KeyGesture(Key.Tab, ModifierKeys.Shift));
            }

            if (channelVm != null)
            {
                AddContextualBinding(channelVm.SwitchCommand, new KeyGesture(Key.Tab, ModifierKeys.Shift));

                AddContextualBinding(channelVm.SwitchSearchCommand, new KeyGesture(Key.F, ModifierKeys.Control));
            }
        }

        private static void AddContextualBinding(ICommand command, KeyGesture keyGesture)
        {
            var binding = new KeyBinding(command, keyGesture);
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