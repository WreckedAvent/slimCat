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

namespace Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    using Models;

    /// <summary>
    ///     The settings daemon.
    /// </summary>
    public static class SettingsDaemon
    {
        #region Constants

        private const string SETTINGS_FILE_NAME = "!settings.xml";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns either the channel settings that already exist or a new settings file
        /// </summary>
        /// <param name="CurrentCharacter">
        /// The Current Character.
        /// </param>
        /// <param name="Title">
        /// The Title.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        /// <param name="chanType">
        /// The chan Type.
        /// </param>
        /// <returns>
        /// The <see cref="ChannelSettingsModel"/>.
        /// </returns>
        public static ChannelSettingsModel GetChannelSettings(
            string CurrentCharacter, string Title, string ID, ChannelType chanType)
        {
            makeSettingsFileIfNotExist(CurrentCharacter, Title, ID, chanType);
            string workingPath = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, Title, ID);
            workingPath = Path.Combine(workingPath, SETTINGS_FILE_NAME);

            try
            {
                return ReadObjectFromXML(
                    workingPath, new ChannelSettingsModel(chanType == ChannelType.pm ? true : false));

                // try and parse the XML file
            }
            catch
            {
                return new ChannelSettingsModel(chanType == ChannelType.pm ? true : false);

                // return a default if it's not legible
            }
        }

        /// <summary>
        /// The has channel settings.
        /// </summary>
        /// <param name="CurrentCharacter">
        /// The current character.
        /// </param>
        /// <param name="Title">
        /// The title.
        /// </param>
        /// <param name="ID">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool HasChannelSettings(string CurrentCharacter, string Title, string ID)
        {
            string path = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, Title, ID);

            return Directory.Exists(Path.Combine(path, SETTINGS_FILE_NAME));
        }

        /// <summary>
        /// Updates the application settings from file
        /// </summary>
        /// <param name="currentCharacter">
        /// The current Character.
        /// </param>
        public static void ReadApplicationSettingsFromXML(string currentCharacter)
        {
            makeGlobalSettingsFileIfNotExist(currentCharacter);

            Type type = typeof(ApplicationSettings);
            PropertyInfo[] propertyList = type.GetProperties();
            string path = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");
            path = Path.Combine(path, SETTINGS_FILE_NAME);

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    XElement workingElement = XElement.Load(fs);
                    foreach (XElement element in workingElement.Descendants())
                    {
                        foreach (PropertyInfo property in propertyList)
                        {
                            if (string.Equals(property.Name, element.Name.ToString(), StringComparison.Ordinal))
                            {
                                if (string.IsNullOrWhiteSpace(element.Value))
                                {
                                    continue; // fix a bad issue with the parser
                                }

                                if (!element.HasElements)
                                {
                                    object setter = Convert.ChangeType(element.Value, property.PropertyType);
                                    property.SetValue(null, setter, null);
                                    break;
                                }
                                else
                                {
                                    IList<string> collection = ApplicationSettings.SavedChannels;

                                    if (property.Name.Equals("interested", StringComparison.OrdinalIgnoreCase))
                                    {
                                        collection = ApplicationSettings.Interested;
                                    }
                                    else if (property.Name.Equals("notinterested", StringComparison.OrdinalIgnoreCase))
                                    {
                                        collection = ApplicationSettings.NotInterested;
                                    }

                                    collection.Clear();
                                    foreach (XElement item in element.Elements())
                                    {
                                        collection.Add(item.Value);
                                    }
                                }
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
        public static T ReadObjectFromXML<T>(string fileName, T baseObject, bool decrypt = false) where T : new()
        {
            Type type = baseObject.GetType();
            PropertyInfo[] propertyList = type.GetProperties(); // reflect property names

            if (decrypt)
            {
                File.Decrypt(fileName);
            }

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                // open our file
                XElement workingElement = XElement.Load(fs);
                foreach (XElement element in workingElement.Descendants())
                {
                    // iterate through each element
                    foreach (PropertyInfo property in propertyList)
                    {
                        // check if the element is one of our properties
                        if (string.Equals(property.Name, element.Name.ToString(), StringComparison.Ordinal))
                        {
                            object setter = Convert.ChangeType(element.Value, property.PropertyType);
                            property.SetValue(baseObject, setter, null);
                            break;
                        }
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
        public static void SaveApplicationSettingsToXML(string currentCharacter)
        {
            var root = new XElement("settings");
            string fileName = Path.Combine(
                StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global"), SETTINGS_FILE_NAME);

            foreach (PropertyInfo property in typeof(ApplicationSettings).GetProperties())
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
                        foreach (string item in property.GetValue(null, null) as IEnumerable<string>)
                        {
                            string label = "item";
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
            using (FileStream fs = File.OpenWrite(fileName)) root.Save(fs);

            root = null;
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
        public static void SerializeObjectToXML(object toSerialize, string fileName, bool encrypt = false)
        {
            Type type = toSerialize.GetType();
            var checkTerms = new[] { "command", "is", "enumerable" };
            var root = new XElement("settings");

            foreach (PropertyInfo property in type.GetProperties())
            {
                if (!checkTerms.Any(term => property.Name.ToLower().Contains(term)))
                {
                    root.Add(
                        new XElement(property.Name, property.GetValue(toSerialize, null))
                        
                        
                        // reflect its name and value, then write
                        );
                }
            }

            File.Delete(fileName);
            using (FileStream fs = File.OpenWrite(fileName)) root.Save(fs);

            if (encrypt)
            {
                File.Encrypt(fileName);
            }

            root = null;
        }

        /// <summary>
        /// Updates our settings XML file with newSettingsModel
        /// </summary>
        /// <param name="newSettingsModel">
        /// The new Settings Model.
        /// </param>
        /// <param name="CurrentCharacter">
        /// The Current Character.
        /// </param>
        /// <param name="Title">
        /// The Title.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        public static void UpdateSettingsFile(object newSettingsModel, string CurrentCharacter, string Title, string ID)
        {
            string workingPath = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, Title, ID);
            workingPath = Path.Combine(workingPath, SETTINGS_FILE_NAME);

            SerializeObjectToXML(newSettingsModel, workingPath);
        }

        #endregion

        #region Methods

        private static void makeGlobalSettingsFileIfNotExist(string currentCharacter)
        {
            string path = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string workingPath = Path.Combine(path, SETTINGS_FILE_NAME);

            if (!File.Exists(workingPath))
            {
                SaveApplicationSettingsToXML(currentCharacter);
            }
        }

        private static void makeSettingsFileIfNotExist(
            string CurrentCharacter, string Title, string ID, ChannelType chanType)
        {
            string path = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, Title, ID);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string workingPath = Path.Combine(path, SETTINGS_FILE_NAME);

            if (!File.Exists(workingPath))
            {
                // make a new XML settings document
                var newSettings = new ChannelSettingsModel(chanType == ChannelType.pm ? true : false);
                SerializeObjectToXML(newSettings, workingPath);
            }
        }

        #endregion
    }
}