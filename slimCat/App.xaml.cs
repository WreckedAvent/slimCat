using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Practices.Unity;
using Models;
using Services;
using ViewModels;

namespace slimCat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                { // load external colors
                    string exeFilePath = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
                    string exeDirPath = Path.GetDirectoryName(exeFilePath);
                    string targetFile = "Theme\\Colors.xaml";
                    string path_to_xaml_dictionary = new Uri(Path.Combine(exeDirPath, targetFile)).LocalPath;
                    string strXaml = File.ReadAllText(path_to_xaml_dictionary);
                    ResourceDictionary resourceDictionary = (ResourceDictionary)XamlReader.Parse(strXaml);
                    Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
                }

                { // load external theme
                    string exeFilePath = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
                    string exeDirPath = Path.GetDirectoryName(exeFilePath);
                    string targetFile = "Theme\\Theme.xaml";
                    string path_to_xaml_dictionary = new Uri(Path.Combine(exeDirPath, targetFile)).LocalPath;
                    string strXaml = File.ReadAllText(path_to_xaml_dictionary);
                    ResourceDictionary resourceDictionary = (ResourceDictionary)XamlReader.Parse(strXaml);
                    Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
                }

                base.OnStartup(e);

                bootstrapper bstrap = new bootstrapper();
                bstrap.Run();
            }
            catch (Exception ex)
            {
                ex.Source = "Start up";
                Exceptions.HandleException(ex);
            }
        }
    }

}
