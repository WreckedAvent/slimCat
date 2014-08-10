#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat
{
    #region Usings

    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Windows;
    using Properties;
    using Utilities;

    #endregion

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class App
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="App" /> class.
        /// </summary>
        public App()
        {
            Dispatcher.UnhandledException += Exceptions.HandleException;
            InitLog();
        }

        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var appVersion = assembly.GetName().Version;
            var appVersionString = appVersion.ToString();

            if (Settings.Default.ApplicationVersion != appVersion.ToString())
            {
                Settings.Default.Upgrade();
                Settings.Default.ApplicationVersion = appVersionString;
            }

            var bstrap = new Bootstrapper();
            bstrap.Run();
        }

        [Conditional("DEBUG")]
        private void InitLog()
        {
            if (File.Exists("trace.log")) File.Delete("trace.log");

            Logging.LogHeader("starting " + Constants.FriendlyName);
        }
    }
}