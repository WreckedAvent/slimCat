// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Justin Kadrovach">
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
//   Interaction logic for App.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Markup;

    using Slimcat.Utilities;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="App" /> class.
        /// </summary>
        public App()
        {
            this.Dispatcher.UnhandledException += Exceptions.HandleException;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The on startup.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                {
                    // load external colors
                    string exeFilePath = Assembly.GetExecutingAssembly().GetName().CodeBase;
                    string exeDirPath = Path.GetDirectoryName(exeFilePath);
                    const string TargetFile = "Theme\\Colors.xaml";
                    string pathToXamlDictionary = new Uri(Path.Combine(exeDirPath, TargetFile)).LocalPath;
                    string strXaml = File.ReadAllText(pathToXamlDictionary);
                    var resourceDictionary = (ResourceDictionary)XamlReader.Parse(strXaml);
                    Current.Resources.MergedDictionaries.Add(resourceDictionary);
                }

                {
                    // load external theme
                    string exeFilePath = Assembly.GetExecutingAssembly().GetName().CodeBase;
                    string exeDirPath = Path.GetDirectoryName(exeFilePath);
                    const string TargetFile = "Theme\\Theme.xaml";
                    string pathToXamlDictionary = new Uri(Path.Combine(exeDirPath, TargetFile)).LocalPath;
                    string strXaml = File.ReadAllText(pathToXamlDictionary);
                    var resourceDictionary = (ResourceDictionary)XamlReader.Parse(strXaml);
                    Current.Resources.MergedDictionaries.Add(resourceDictionary);
                }

                {
                    // load internal theme
                    var resources = new ResourceDictionary
                                        {
                                            Source =
                                                new Uri(
                                                "/slimCat;component/EmbeddedTheme.xaml",
                                                UriKind.RelativeOrAbsolute)
                                        };

                    Current.Resources.MergedDictionaries.Add(resources);
                }

                base.OnStartup(e);

                var bstrap = new Bootstrapper();
                bstrap.Run();
            }
            catch (Exception ex)
            {
                ex.Source = "Start up";
                Exceptions.HandleException(ex);
            }
        }

        #endregion
    }
}