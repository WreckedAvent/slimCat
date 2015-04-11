#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="F-ChatConnection.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

/*
 * Through .NET witchcraft, this bootstrapper starts slimCat in an isolated app domain
 * with all of its dependencies "shadow copied" to a temporary folder
 * This enables the client to update itself while it is running.
 * 
 * Which is really crazy when you think about it.
 * 
 * This file contains no real logic, in case I need to update the updater. You heard me.
 */

namespace Bootstrapper
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    class Program
    {
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        [STAThread]
        static void Main(string[] args)
        {
            var startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (startupPath == null) return;

            var cachePath = Path.GetTempPath();

            // slimCat client names
            var configFile = Path.Combine(startupPath, "slimCat.exe.config");
            var assembly = Path.Combine(startupPath, "slimCat.exe");

            // Want to know what shadow copying is?
            var setup = new AppDomainSetup
            {
                ApplicationName = "slimCat",
                ShadowCopyFiles = "true",
                CachePath = cachePath,
                ConfigurationFile = configFile
            };

            var domain = AppDomain.CreateDomain("slimCat", AppDomain.CurrentDomain.Evidence, setup);

            domain.SetData("path", startupPath);
            domain.ExecuteAssembly(assembly, args);

            // we can do slimCat clean up here if necessary
            AppDomain.Unload(domain);
            try { Directory.Delete(cachePath, true); } 
            catch { }
        }
    }
}
