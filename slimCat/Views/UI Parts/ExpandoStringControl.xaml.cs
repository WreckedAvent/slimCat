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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Views
{
    /// <summary>
    /// Interaction logic for ExpandoStringControl.xaml
    /// </summary>
    public partial class ExpandoStringControl : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private string _fullText;
        private bool _isExpanded = true;
        #endregion

        #region Properties
        public string DisplayText
        {
            get
            {
                if (!_isExpanded && _fullText.Length > 50)
                    return _fullText.Substring(0, 50) + " ...";
                else
                    return _fullText;
            }
            set
            {
                _fullText = value;
            }
        }

        public bool CanExpand
        { 
            get 
            { 
                if (_fullText != null)
                    return _fullText.Length > 50;
                return false;
            } 
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set 
            {
                _isExpanded = value; 
                OnPropertyChanged("IsExpanded");
                OnPropertyChanged("CanExpand");
                OnPropertyChanged("DisplayText");
                OnPropertyChanged("ExpandString");
            }
        }

        public string ExpandString
        {
            get 
            {
                if (!CanExpand) return "";
                if (IsExpanded) return "<--"; else return "-->";
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ExpandoStringControl), new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        public static void OnTextPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var element = obj as ExpandoStringControl;
            if (element != null)
            {
                element.DisplayText = e.NewValue as string;
            }
        }
        
        #endregion

        #region Constructor
        public ExpandoStringControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods

        #region Methods
        public void OnExpand(object sender = null, EventArgs e = null)
        {
            IsExpanded = !IsExpanded;
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string args)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(args));
        }
        #endregion
    }
}
