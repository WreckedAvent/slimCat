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
