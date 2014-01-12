// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelTextBoxEntryView.xaml.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   Interaction logic for ChannelTextBoxEntryView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Input;
    using System.Windows.Markup;

    using Models;
    using ViewModels;

    /// <summary>
    ///     Interaction logic for ChannelTextBoxEntryView.xaml
    /// </summary>
    public partial class ChannelTextBoxEntryView
    {
        #region Static Fields

        private static readonly IDictionary<Key, Tuple<string, bool>> AcceptedKeys =
            new Dictionary<Key, Tuple<string, bool>>
                {
                    // accepted shorcut keys.
                    { Key.B, new Tuple<string, bool>("b", false) }, 
                    { Key.S, new Tuple<string, bool>("s", false) }, 
                    { Key.I, new Tuple<string, bool>("i", false) }, 
                    { Key.U, new Tuple<string, bool>("u", false) }, 
                    { Key.L, new Tuple<string, bool>("url", true) }, 
                    { Key.Up, new Tuple<string, bool>("sup", false) }, 
                    { Key.Down, new Tuple<string, bool>("sub", false) }
                    
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
            this.InitializeComponent();

            this.Entry.FocusableChanged += (s, e) =>
                {
                    if ((bool)e.NewValue)
                    {
                        this.Entry.Focus();
                    }
                };

            this.Entry.Language = XmlLanguage.GetLanguage(ApplicationSettings.Langauge);
            this.vm = this.DataContext as ViewModelBase;

            if (this.vm != null)
            {
                this.vm.PropertyChanged += this.PropertyChanged;
            }

            this.DataContextChanged += (sender, args) =>
            { 
                if (this.vm != null)
                {
                    this.vm.PropertyChanged -= PropertyChanged;
                }

                this.vm = this.DataContext as ViewModelBase;

                if (this.vm != null)
                {
                    this.vm.PropertyChanged += this.PropertyChanged;
                }
            };
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != "Language")
            {
                return;
            }

            Dispatcher.BeginInvoke(
            (Action)delegate
                {
                    this.Entry.Language = XmlLanguage.GetLanguage(ApplicationSettings.Langauge);
                });
        }

        #endregion

        #region Methods

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // this defines shortcuts when the textbox has focus --- in particular, ones which modify the content of the textbox
            if (e.Key == Key.Return && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                var caretIndex = this.Entry.CaretIndex;
                this.Entry.Text = this.Entry.Text.Insert(caretIndex, "\r");
                this.Entry.CaretIndex = caretIndex + 1;
            }
            else if (e.Key == Key.Return)
            {
                e.Handled = true; // don't do the funny business with inserting a new line
            }
            else if (AcceptedKeys.ContainsKey(e.Key) && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;

                var tupleData = AcceptedKeys[e.Key];

                var bbtag = tupleData.Item1;
                var useArgs = tupleData.Item2;

                if (!string.IsNullOrWhiteSpace(this.Entry.SelectedText))
                {
                    var selected = this.Entry.SelectedText;

                    if (!useArgs)
                    {
                        this.Entry.SelectedText = string.Format("[{0}]{1}[/{0}]", bbtag, selected);
                    }
                    else
                    {
                        var toEnter = string.Format("[{0}={1}]", bbtag, selected);
                        var caretIndex = this.Entry.CaretIndex;

                        this.Entry.SelectedText = string.Format("{0}[/{1}]", toEnter, bbtag);
                        this.Entry.CaretIndex = caretIndex + toEnter.Length;
                    }
                }
                else
                {
                    var caretIndex = this.Entry.CaretIndex;

                    this.Entry.Text = this.Entry.Text.Insert(caretIndex, string.Format("[{0}][/{0}]", bbtag));
                    this.Entry.CaretIndex = caretIndex + bbtag.Length + 2;

                    // 2 is a magic number representing the brackets around the BbCode
                }
            }
        }

        #endregion
    }
}