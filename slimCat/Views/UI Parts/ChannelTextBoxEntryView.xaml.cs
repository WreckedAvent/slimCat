/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace Views
{
    /// <summary>
    /// Interaction logic for ChannelTextBoxEntryView.xaml
    /// </summary>
    public partial class ChannelTextBoxEntryView : UserControl
    {
        private static IDictionary<Key, Tuple<string, bool>> _acceptedKeys = new Dictionary<Key, Tuple<string, bool>>()
        {
            // accepted shorcut keys.
            // format: 
            // target key, matching bbtag, if the bbtag takes arguments
            {Key.B, new Tuple<string, bool>("b", false)},
            {Key.S, new Tuple<string, bool>("s", false)},
            {Key.I, new Tuple<string, bool>("i", false)},
            {Key.U, new Tuple<string, bool>("u", false)},
            {Key.L, new Tuple<string, bool>("url", true)},
            {Key.Up, new Tuple<string, bool>("sup", false)},
            {Key.Down, new Tuple<string, bool>("sub", false)}
        };
        public ChannelTextBoxEntryView()
        {
            InitializeComponent();

            Entry.FocusableChanged += (s, e) =>
                {
                    if ((bool)e.NewValue == true)
                        Entry.Focus();
                };
        }
        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            // this defines shortcuts when the textbox has focus --- in particular, ones which modify the content of the textbox
            #region Text-only (non-command) shortcuts
            if (e.Key == Key.Return && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                var caretIndex = Entry.CaretIndex;
                Entry.Text = Entry.Text.Insert(caretIndex, "\r");
                Entry.CaretIndex = caretIndex + 1;
            }
            else if (e.Key == Key.Return)
                e.Handled = true; // don't do the funny business with inserting a new line
            #endregion

            #region BBCode shortcuts
            else if (_acceptedKeys.ContainsKey(e.Key) && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;

                var tupleData = _acceptedKeys[e.Key];

                var bbtag = tupleData.Item1;
                var useArgs = tupleData.Item2;

                if (!String.IsNullOrWhiteSpace(Entry.SelectedText))
                {
                    var selected = Entry.SelectedText;

                    if (!useArgs)
                        Entry.SelectedText = String.Format("[{0}]{1}[/{0}]", bbtag, selected);
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

                    Entry.Text = Entry.Text.Insert(caretIndex, String.Format("[{0}][/{0}]", bbtag));
                    Entry.CaretIndex = caretIndex + bbtag.Length + 2; // 2 is a magic number representing the brackets around the BBCode
                }
            }
            #endregion
        }
    }
}
