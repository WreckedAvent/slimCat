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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViewModels;

namespace Views
{
    /// <summary>
    /// Interaction logic for GeneralChannelView.xaml
    /// </summary>
    public partial class GeneralChannelView : DisposableView
    {
        #region Fields
        private GeneralChannelViewModel _vm;
        private SnapToBottomManager _manager;
        #endregion

        #region Constructors
        public GeneralChannelView(GeneralChannelViewModel vm)
        {
            try
            {
                InitializeComponent();
                _vm = vm.ThrowIfNull("vm");

                this.DataContext = _vm;

                _manager = new SnapToBottomManager(messages);

                _vm.NewAdArrived += OnNewAdArrived;
                _vm.NewMessageArrived += OnNewMessageArrived;
                _vm.PropertyChanged += OnNewPropertyChanged;
            }

            catch (Exception ex)
            {
                ex.Source = "PMChannel View, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        private void OnMessagesLoaded(object sender, EventArgs e)
        {
            _manager.AutoDownScroll(false, true);
        }

        private void OnNewAdArrived(object sender, EventArgs e)
        {
            bool keepAtCurrent = _vm.Model.Messages.Count >= Models.ApplicationSettings.BackLogMax;

            _manager.AutoDownScroll(keepAtCurrent);
        }

        private void OnNewMessageArrived(object sender, EventArgs e)
        {
            bool keepAtCurrent = _vm.Model.Messages.Count >= Models.ApplicationSettings.BackLogMax;

            _manager.AutoDownScroll(keepAtCurrent);
        }

        private void OnNewPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsSearching"))
                _manager.AutoDownScroll(true, true);
        }
        #endregion

        internal override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                _vm.NewAdArrived -= OnNewAdArrived;
                _vm.NewMessageArrived -= OnNewMessageArrived;
                _vm.PropertyChanged -= OnNewPropertyChanged;
                _vm = null;
                _manager = null;
                history.ItemsSource = null;
                current.ItemsSource = null;
                this.DataContext = null;
            }
        }
    }
}
