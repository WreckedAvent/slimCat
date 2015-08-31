#region Copyright

// <copyright file="Program.cs">
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
    #region Usings

    using System;
    using System.IO;
    using System.Reflection;
    using static System.IO.Path;

    #endregion

    internal class Program
    {
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                var startupPath = GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (startupPath == null) return;

                var clientPath = Combine(startupPath, "client");

                var cachePath = GetTempPath();

                // slimCat client names
                var configFile = Combine(clientPath, "client.exe.config");
                var assembly = Combine(clientPath, "client.exe");

                // Want to know what shadow copying is?
                var setup = new AppDomainSetup
                {
                    ApplicationName = "slimCat",
                    ShadowCopyFiles = "true",
                    CachePath = cachePath,
                    ConfigurationFile = configFile
                };

                var domain = AppDomain.CreateDomain("slimCat", AppDomain.CurrentDomain.Evidence, setup);

                FileUnblocker.Unblock(assembly);
                domain.SetData("path", clientPath);
                domain.ExecuteAssembly(assembly, args);


                // we can do slimCat clean up here if necessary
                AppDomain.Unload(domain);

                try
                {
                    Directory.Delete(cachePath, true);
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                try
                {
                    using (var file = new StreamWriter(@"Stacktrace.log", true))
                    {
                        file.WriteLine();
                        file.WriteLine("====================================");
                        file.WriteLine("BEGIN EXCEPTION REPORT");
                        file.WriteLine(DateTime.UtcNow);
                        file.WriteLine("====================================");
                        file.WriteLine();
                        file.WriteLine("Exception: {0}", ex.Message);
                        file.WriteLine("Occured at: {0}", ex.Source);
                        file.WriteLine();
                        file.Write("Immediate stack trace: {0}", ex.TargetSite);
                        file.WriteLine(ex.StackTrace);

                        if (ex.InnerException != null)
                            file.WriteLine("Inner Exception: {0}", ex.InnerException);

                        file.WriteLine();

                        file.WriteLine("====================================");
                        file.WriteLine("END EXCEPTION REPORT");
                        file.WriteLine("====================================");
                        file.Flush();
                    }
                }
                catch (IOException)
                {
                }
            }
        }
    }
}