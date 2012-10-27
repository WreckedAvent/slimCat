using System;
using System.Collections.Generic;
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
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using ViewModels;

namespace slimCat
{
    /// <summary>
    /// The shell has no meaningful purpose other than being a giant container.
    /// </summary>
    public partial class Shell : Window
    {
        public const string MainRegion = "MainRegion";

        public Shell()
        {
            InitializeComponent();
        }
    }
}
