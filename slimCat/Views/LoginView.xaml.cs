using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Practices.Unity;
using ViewModels;

namespace Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        private readonly ViewModelBase _vm;

        public LoginView(LoginViewModel vm)
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
                ex.Source = "Login View, init";
                Exceptions.HandleException(ex);
            }
        }
    }
}
