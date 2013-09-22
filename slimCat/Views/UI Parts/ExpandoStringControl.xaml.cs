// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExpandoStringControl.xaml.cs" company="Justin Kadrovach">
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
//   Interaction logic for ExpandoStringControl.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Views
{
    using System;
    using System.ComponentModel;
    using System.Windows;

    /// <summary>
    ///     Interaction logic for ExpandoStringControl.xaml
    /// </summary>
    public partial class ExpandoStringControl : INotifyPropertyChanged
    {
        #region Static Fields

        /// <summary>
        ///     The text property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", 
            typeof(string), 
            typeof(ExpandoStringControl), 
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        #endregion

        #region Fields

        private string fullText;

        private bool isExpanded = true;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpandoStringControl" /> class.
        /// </summary>
        public ExpandoStringControl()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether can expand.
        /// </summary>
        public bool CanExpand
        {
            get
            {
                if (this.fullText != null)
                {
                    return this.fullText.Length > 50;
                }

                return false;
            }
        }

        /// <summary>
        ///     Gets or sets the display text.
        /// </summary>
        public string DisplayText
        {
            get
            {
                if (!this.isExpanded && this.fullText.Length > 50)
                {
                    return this.fullText.Substring(0, 50) + " ...";
                }
                return this.fullText;
            }

            set
            {
                this.fullText = value;
            }
        }

        /// <summary>
        ///     Gets the expand string.
        /// </summary>
        public string ExpandString
        {
            get
            {
                if (!this.CanExpand)
                {
                    return string.Empty;
                }

                return this.IsExpanded ? "<--" : "-->";
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return this.isExpanded;
            }

            set
            {
                this.isExpanded = value;
                this.OnPropertyChanged("IsExpanded");
                this.OnPropertyChanged("CanExpand");
                this.OnPropertyChanged("DisplayText");
                this.OnPropertyChanged("ExpandString");
            }
        }

        /// <summary>
        ///     Gets or sets the text.
        /// </summary>
        public string Text
        {
            get
            {
                return (string)this.GetValue(TextProperty);
            }

            set
            {
                this.SetValue(TextProperty, value);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The on text property changed.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public static void OnTextPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var element = obj as ExpandoStringControl;
            if (element != null)
            {
                element.DisplayText = e.NewValue as string;
            }
        }

        /// <summary>
        /// The on expand.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public void OnExpand(object sender = null, EventArgs e = null)
        {
            this.IsExpanded = !this.IsExpanded;
        }

        /// <summary>
        /// The on property changed.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public void OnPropertyChanged(string args)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(args));
            }
        }

        #endregion
    }
}