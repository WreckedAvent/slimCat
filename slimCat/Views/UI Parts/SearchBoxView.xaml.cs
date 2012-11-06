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

namespace Views
{
    /// <summary>
    /// Interaction logic for SearchBoxView.xaml
    /// </summary>
    public partial class SearchBoxView : UserControl
    {
        public SearchBoxView()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// A box and filter chain for all things which can be searched
    /// </summary>
    public interface ISearchable
    {
        string SearchString { get; set; }
    }
}
