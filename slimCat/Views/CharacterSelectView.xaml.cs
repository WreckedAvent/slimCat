using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for CharacterSelectView.xaml
    /// </summary>
    public partial class CharacterSelectView : UserControl
    {
        private readonly ViewModelBase _vm;

        public CharacterSelectView(CharacterSelectViewModel vm)
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
                ex.Source = "Character select view, init";
                Exceptions.HandleException(ex);
            }
        }
    }
}
