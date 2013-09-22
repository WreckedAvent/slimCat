// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsDaemon.cs" company="Justin Kadrovach">
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
//   The settings daemon.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    using Slimcat.Models;
    using Slimcat.Utilities;

    /// <summary>
    ///     The settings daemon.
    /// </summary>
    public static class SettingsDaemon
    {
        #region Constants

        private const string SettingsFileName = "!settings.xml";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns either the channel settings that already exist or a new settings file
        /// </summary>
        /// <param name="currentCharacter">
        /// The Current Character.
        /// </param>
        /// <param name="title">
        /// The Title.
        /// </param>
        /// <param name="id">
        /// The ID.
        /// </param>
        /// <param name="chanType">
        /// The chan Type.
        /// </param>
        /// <returns>
        /// The <see cref="ChannelSettingsModel"/>.
        /// </returns>
        public static ChannelSettingsModel GetChannelSettings(
            string currentCharacter, string title, string id, ChannelType chanType)
        {
            MakeSettingsFileIfNotExist(currentCharacter, title, id, chanType);
            var workingPath = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);
            workingPath = Path.Combine(workingPath, SettingsFileName);

            try
            {
                return ReadObjectFromXml(
                    workingPath, new ChannelSettingsModel(chanType == ChannelType.PrivateMessage));

                // try and parse the XML file
            }
            catch
            {
                return new ChannelSettingsModel(chanType == ChannelType.PrivateMessage);

                // return a default if it's not legible
            }
        }

        /// <summary>
        /// The has channel settings.
        /// </summary>
        /// <param name="currentCharacter">
        /// The current character.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool HasChannelSettings(string currentCharacter, string title, string id)
        {
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);

            return Directory.Exists(Path.Combine(path, SettingsFileName));
        }

        /// <summary>
        /// Updates the application settings from file
        /// </summary>
        /// <param name="currentCharacter">
        /// The current Character.
        /// </param>
        public static void ReadApplicationSettingsFromXml(string currentCharacter)
        {
            MakeGlobalSettingsFileIfNotExist(currentCharacter);

            var type = typeof(ApplicationSettings);
            var propertyList = type.GetProperties();
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");
            path = Path.Combine(path, SettingsFileName);

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var workingElement = XElement.Load(fs);
                    foreach (var element in workingElement.Descendants())
                    {
                        var element1 = element;
                        foreach (var property in propertyList
                            .Where(property => string.Equals(property.Name, element1.Name.ToString(), StringComparison.Ordinal))
                            .Where(property => !string.IsNullOrWhiteSpace(element1.Value)))
                        {
                            if (!element.HasElements)
                            {
                                var setter = Convert.ChangeType(element.Value, property.PropertyType);
                                property.SetValue(null, setter, null);
                                break;
                            }

                            var collection = ApplicationSettings.SavedChannels;

                            if (property.Name.Equals("interested", StringComparison.OrdinalIgnoreCase))
                            {
                                collection = ApplicationSettings.Interested;
                            }
                            else if (property.Name.Equals("notinterested", StringComparison.OrdinalIgnoreCase))
                            {
                                collection = ApplicationSettings.NotInterested;
                            }

                            collection.Clear();
                            foreach (var item in element.Elements())
                            {
                                collection.Add(item.Value);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Return type T from a specified XML file, using reflection
        /// </summary>
        /// <param name="fileName">
        /// The file Name.
        /// </param>
        /// <param name="baseObject">
        /// The base Object.
        /// </param>
        /// <param name="decrypt">
        /// The decrypt.
        /// </param>
        /// <returns>
        /// The <see cref="T"/>.
        /// </returns>
        public static T ReadObjectFromXml<T>(string fileName, T baseObject, bool decrypt = false) where T : new()
        {
            var type = baseObject.GetType();
            var propertyList = type.GetProperties(); // reflect property names

            if (decrypt)
            {
                File.Decrypt(fileName);
            }

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                // open our file
                var workingElement = XElement.Load(fs);
                foreach (var element in workingElement.Descendants())
                {
                    // iterate through each element
                    foreach (var property in propertyList)
                    {
                        // check if the element is one of our properties
                        if (!string.Equals(property.Name, element.Name.ToString(), StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var setter = Convert.ChangeType(element.Value, property.PropertyType);
                        property.SetValue(baseObject, setter, null);
                        break;
                    }
                }
            }

            return baseObject; // return it
        }

        /// <summary>
        /// Updates the application settings file from memory
        /// </summary>
        /// <param name="currentCharacter">
        /// The current Character.
        /// </param>
        public static void SaveApplicationSettingsToXml(string currentCharacter)
        {
            var root = new XElement("settings");
            var fileName = Path.Combine(
                StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global"), SettingsFileName);

            foreach (var property in typeof(ApplicationSettings).GetProperties())
            {
                if (property.PropertyType != typeof(IList<string>)
                    && property.PropertyType != typeof(IEnumerable<string>))
                {
                    root.Add(new XElement(property.Name, property.GetValue(null, null)));
                }
                else
                {
                    if (!property.Name.ToLower().Contains("list"))
                    {
                        var toAdd = new XElement(property.Name);
                        foreach (var item in property.GetValue(null, null) as IEnumerable<string>)
                        {
                            var label = "item";
                            if (property.Name.ToLower().Contains("channel"))
                            {
                                label = "channel";
                            }
                            else if (property.Name.ToLower().Contains("interested"))
                            {
                                label = "character";
                            }

                            toAdd.Add(new XElement(label, item));
                        }

                        root.Add(toAdd);
                    }
                }
            }

            File.Delete(fileName);
            using (var fs = File.OpenWrite(fileName))
            {
                root.Save(fs);
            }
        }

        /// <summary>
        /// Serialize and object to XML through reflection
        /// </summary>
        /// <param name="toSerialize">
        /// The to Serialize.
        /// </param>
        /// <param name="fileName">
        /// The file Name.
        /// </param>
        /// <param name="encrypt">
        /// The encrypt.
        /// </param>
        public static void SerializeObjectToXml(object toSerialize, string fileName, bool encrypt = false)
        {
            var type = toSerialize.GetType();
            var checkTerms = new[] { "command", "is", "enumerable" };
            var root = new XElement("settings");

            foreach (var property in type.GetProperties()
                .Where(property => 
                    !checkTerms.Any(term => property.Name.ToLower().Contains(term))))
            {
                root.Add(new XElement(property.Name, property.GetValue(toSerialize, null)));
            }

            File.Delete(fileName);
            using (var fs = File.OpenWrite(fileName))
            {
                root.Save(fs);
            }

            if (encrypt)
            {
                File.Encrypt(fileName);
            }
        }

        /// <summary>
        /// Updates our settings XML file with newSettingsModel
        /// </summary>
        /// <param name="newSettingsModel">
        /// The new Settings Model.
        /// </param>
        /// <param name="currentCharacter">
        /// The Current Character.
        /// </param>
        /// <param name="title">
        /// The Title.
        /// </param>
        /// <param name="id">
        /// The ID.
        /// </param>
        public static void UpdateSettingsFile(object newSettingsModel, string currentCharacter, string title, string id)
        {
            string workingPath = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);
            workingPath = Path.Combine(workingPath, SettingsFileName);

            SerializeObjectToXml(newSettingsModel, workingPath);
        }

        #endregion

        #region Methods

        private static void MakeGlobalSettingsFileIfNotExist(string currentCharacter)
        {
            string path = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string workingPath = Path.Combine(path, SettingsFileName);

            if (!File.Exists(workingPath))
            {
                SaveApplicationSettingsToXml(currentCharacter);
            }
        }

        private static void MakeSettingsFileIfNotExist(
            string currentCharacter, string title, string id, ChannelType chanType)
        {
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var workingPath = Path.Combine(path, SettingsFileName);

            if (File.Exists(workingPath))
            {
                return;
            }

            // make a new XML settings document
            var newSettings = new ChannelSettingsModel(chanType == ChannelType.PrivateMessage);
            SerializeObjectToXml(newSettings, workingPath);
        }

        #endregion
    }
}