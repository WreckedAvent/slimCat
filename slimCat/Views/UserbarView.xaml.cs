using System;
using System.Collections.Generic;
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
using ViewModels;

namespace Views
{
    /// <summary>
    /// Interaction logic for Userbar.xaml
    /// </summary>
    public partial class UserbarView : UserControl
    {
        #region Fields
        private readonly ViewModelBase _vm;
        #endregion

        public UserbarView(UserbarViewModel vm)
        {
            try
            {
                InitializeComponent();

                _vm = vm;
                if (_vm == null) throw new ArgumentNullException("vm");

                this.DataContext = _vm;
            }
            catch (Exception ex)
            {
                ex.Source = "Userbar View, init";
                Exceptions.HandleException(ex);
            }
        }
    }
}
