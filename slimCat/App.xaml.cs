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
        public App() : base()
        {
            this.Dispatcher.UnhandledException += Exceptions.HandleException;
        }

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

                {// load internal theme

                    ResourceDictionary resources = new ResourceDictionary();
                    resources.Source = new Uri("/slimCat;component/EmbeddedTheme.xaml", UriKind.RelativeOrAbsolute);

                    Application.Current.Resources.MergedDictionaries.Add(resources);

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
