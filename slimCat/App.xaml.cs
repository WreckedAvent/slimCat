#region Copyright

// <copyright file="App.xaml.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
//
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
//
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>

#endregion

namespace slimCat
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Services;
    using Utilities;

    #endregion

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class App
    {
        #region Fields

        private readonly IList<string> requiredFiles = new[]
        {
            "Theme\\Colors.xaml",
            "Theme\\theme.csv",
            "Theme\\Theme.xaml",
            "icons\\auto.png",
            "icons\\auto.png",
            "icons\\browser.png",
            "icons\\channels.png",
            "icons\\chat.png",
            "icons\\close.png",
            "icons\\document.png",
            "icons\\down.png",
            "icons\\edit.png",
            "icons\\filter.png",
            "icons\\folder.png",
            "icons\\friend.png",
            "icons\\global.png",
            "icons\\logout.png",
            "icons\\male.png",
            "icons\\female.png",
            "icons\\markup.png",
            "icons\\more.png",
            "icons\\none.png",
            "icons\\notifications.png",
            "icons\\pin.png",
            "icons\\private_closed.png",
            "icons\\private_open.png",
            "icons\\profile.png",
            "icons\\public.png",
            "icons\\restart.png",
            "icons\\search.png",
            "icons\\send_ad.png",
            "icons\\send_chat.png",
            "icons\\send_console.png",
            "icons\\send_note.png",
            "icons\\settings.png",
            "icons\\stats.png",
            "icons\\transgender.png",
            "icons\\up.png",
            "icons\\userlist.png"
        };

        #endregion

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
            var assembly = Assembly.GetExecutingAssembly();
            var cwd = AppDomain.CurrentDomain.GetData("path") as string ?? Path.GetDirectoryName(assembly.Location);

            Directory.SetCurrentDirectory(cwd);

            base.OnStartup(e);

            var appVersion = assembly.GetName().Version;
            var preferences = SettingsService.Preferences;

            if (preferences.Version != appVersion.ToString())
            {
                preferences.Version = appVersion.ToString();
                if (File.Exists("stacktrace.log")) File.Delete("stacktrace.log");
            }

            var args = Environment.GetCommandLineArgs();

            preferences.IsAdvanced = args.Any(x => x.Equals("advanced", StringComparison.OrdinalIgnoreCase));
            preferences.IsPortable = args.Any(x => x.Equals("portable", StringComparison.OrdinalIgnoreCase));
            preferences.BasePath = cwd;
            SettingsService.Preferences = preferences;

            foreach (var file in requiredFiles.Where(file => !File.Exists(file)))
            {
                Exceptions.ShowErrorBox(
                    $"slimCat will now exit. \nReason: Required theme file \"{file}\" is missing. This is likely due to a bad theme install.\n" +
                    "Please install themes by extracting a theme over the default theme, overwriting when prompted to.",
                    "slimCat Fatal Error");

                Environment.Exit(-1);
            }


            /*
                Here we load in the theme.
                You may notice that this is somewhat involved for just loading in some files, but there are two reasons for this:

                1) slimCat can be ran with the loader in such a way that the starting assembly location is different from running
                   locally. This complicates loading in statically, to say the least.

                2) We might want to load a different theme based on settings. This is not a feature yet, but we load settings literally
                   just a few lines ago, so it is a certain possibility.

                Some other notes here: colors needs to be loaded in first due to themes.xaml using it. I've tried NOT using siteoforigin
                in the pack and it did not work well at all, since it required the theme file to be built into the assembly (defeating the point).
            */
            Action<string> addResourceDictionary = partialUri =>
            {
                var dict = new ResourceDictionary { Source = new Uri("pack://siteoforigin:,,,/" + partialUri) };
                Current.Resources.MergedDictionaries.Add(dict);
            };

            try
            {
                // we will attempt to add them by first looking for a /theme near our executing assembly
                addResourceDictionary("Theme/Colors.xaml");
                addResourceDictionary("Theme/Theme.xaml");
            }
            catch (DirectoryNotFoundException)
            {
                // if that doesn't work, look for theme in client/theme
                addResourceDictionary("Client/Theme/Colors.xaml");
                addResourceDictionary("Client/Theme/Theme.xaml");
            }

            // this depends on our external themes, so it has to be loaded last
            var embeddedTheme = new ResourceDictionary { Source = new Uri("Theme/EmbeddedTheme.xaml", UriKind.Relative) };
            Current.Resources.MergedDictionaries.Add(embeddedTheme);

            new Bootstrapper().Run();
        }

        [Conditional("DEBUG")]
        private static void InitLog()
        {
            if (File.Exists("trace.log")) File.Delete("trace.log");

            Logging.LogHeader("starting " + Constants.FriendlyName);
        }
    }
}