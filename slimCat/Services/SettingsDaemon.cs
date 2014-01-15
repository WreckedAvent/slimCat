#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsDaemon.cs">
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

namespace Slimcat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Models;
    using Utilities;

    #endregion

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
        ///     Returns either the channel settings that already exist or a new settings file
        /// </summary>
        /// <param name="currentCharacter">
        ///     The Current Character.
        /// </param>
        /// <param name="title">
        ///     The Title.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        /// <param name="chanType">
        ///     The chan Type.
        /// </param>
        /// <returns>
        ///     The <see cref="ChannelSettingsModel" />.
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
        ///     The has channel settings.
        /// </summary>
        /// <param name="currentCharacter">
        ///     The current character.
        /// </param>
        /// <param name="title">
        ///     The title.
        /// </param>
        /// <param name="id">
        ///     The id.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool HasChannelSettings(string currentCharacter, string title, string id)
        {
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);

            return Directory.Exists(Path.Combine(path, SettingsFileName));
        }

        /// <summary>
        ///     Updates the application settings from file
        /// </summary>
        /// <param name="currentCharacter">
        ///     The current Character.
        /// </param>
        public static void ReadApplicationSettingsFromXml(string currentCharacter, ICharacterManager cm)
        {
            MakeGlobalSettingsFileIfNotExist(currentCharacter);

            var type = typeof (ApplicationSettings);
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
                            .Where(
                                property =>
                                    string.Equals(property.Name, element1.Name.ToString(), StringComparison.Ordinal))
                            .Where(property => !string.IsNullOrWhiteSpace(element1.Value)))
                        {
                            if (!element.HasElements)
                            {
                                try
                                {
                                    var setter = Convert.ChangeType(element.Value, property.PropertyType);
                                    property.SetValue(null, setter, null);
                                }
                                catch
                                {
                                }

                                continue;
                            }

                            var collection = ApplicationSettings.SavedChannels;

                            if (property.Name.Equals("interested", StringComparison.OrdinalIgnoreCase))
                                cm.Set(element.Elements().Select(x => x.Value), ListKind.Interested);
                            else if (property.Name.Equals("notinterested", StringComparison.OrdinalIgnoreCase))
                                cm.Set(element.Elements().Select(x => x.Value), ListKind.NotInterested);
                            else
                            {
                                collection.Clear();
                                foreach (var item in element.Elements())
                                    collection.Add(item.Value);
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        ///     Return type T from a specified XML file, using reflection
        /// </summary>
        /// <param name="fileName">
        ///     The file Name.
        /// </param>
        /// <param name="baseObject">
        ///     The base Object.
        /// </param>
        /// <param name="decrypt">
        ///     The decrypt.
        /// </param>
        /// <returns>
        ///     The <see cref="T" />.
        /// </returns>
        public static T ReadObjectFromXml<T>(string fileName, T baseObject, bool decrypt = false) where T : new()
        {
            var type = baseObject.GetType();
            var propertyList = type.GetProperties(); // reflect property names

            if (decrypt)
                File.Decrypt(fileName);

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
                            continue;

                        var setter = Convert.ChangeType(element.Value, property.PropertyType);
                        property.SetValue(baseObject, setter, null);
                        break;
                    }
                }
            }

            return baseObject; // return it
        }

        /// <summary>
        ///     Updates the application settings file from memory
        /// </summary>
        /// <param name="currentCharacter">
        ///     The current Character.
        /// </param>
        public static void SaveApplicationSettingsToXml(string currentCharacter)
        {
            var root = new XElement("settings");
            var fileName = Path.Combine(
                StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global"), SettingsFileName);

            foreach (var property in typeof (ApplicationSettings).GetProperties())
            {
                if (property.PropertyType != typeof (IList<string>)
                    && property.PropertyType != typeof (IEnumerable<string>))
                    root.Add(new XElement(property.Name, property.GetValue(null, null)));
                else
                {
                    if (property.Name.ToLower().Contains("list")) continue;

                    var toAdd = new XElement(property.Name);
                    var items = property.GetValue(null, null) as IList<string>;

                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            var label = "item";
                            if (property.Name.ToLower().Contains("channel"))
                                label = "channel";
                            else if (property.Name.ToLower().Contains("interested"))
                                label = "character";

                            toAdd.Add(new XElement(label, item));
                        }
                    }

                    root.Add(toAdd);
                }
            }

            File.Delete(fileName);
            using (var fs = File.OpenWrite(fileName))
                root.Save(fs);
        }

        /// <summary>
        ///     Serialize and object to XML through reflection
        /// </summary>
        /// <param name="toSerialize">
        ///     The to Serialize.
        /// </param>
        /// <param name="fileName">
        ///     The file Name.
        /// </param>
        /// <param name="encrypt">
        ///     The encrypt.
        /// </param>
        public static void SerializeObjectToXml(object toSerialize, string fileName, bool encrypt = false)
        {
            var type = toSerialize.GetType();
            var checkTerms = new[] {"command", "is", "enumerable"};
            var root = new XElement("settings");

            foreach (var property in type.GetProperties()
                .Where(property =>
                    !checkTerms.Any(term => property.Name.ToLower().Contains(term))))
                root.Add(new XElement(property.Name, property.GetValue(toSerialize, null)));

            File.Delete(fileName);
            using (var fs = File.OpenWrite(fileName))
                root.Save(fs);

            if (encrypt)
                File.Encrypt(fileName);
        }

        /// <summary>
        ///     Updates our settings XML file with newSettingsModel
        /// </summary>
        /// <param name="newSettingsModel">
        ///     The new Settings Model.
        /// </param>
        /// <param name="currentCharacter">
        ///     The Current Character.
        /// </param>
        /// <param name="title">
        ///     The Title.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        public static void UpdateSettingsFile(object newSettingsModel, string currentCharacter, string title, string id)
        {
            var workingPath = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);
            workingPath = Path.Combine(workingPath, SettingsFileName);

            SerializeObjectToXml(newSettingsModel, workingPath);
        }

        #endregion

        #region Methods

        private static void MakeGlobalSettingsFileIfNotExist(string currentCharacter)
        {
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var workingPath = Path.Combine(path, SettingsFileName);

            if (!File.Exists(workingPath))
                SaveApplicationSettingsToXml(currentCharacter);
        }

        private static void MakeSettingsFileIfNotExist(
            string currentCharacter, string title, string id, ChannelType chanType)
        {
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var workingPath = Path.Combine(path, SettingsFileName);

            if (File.Exists(workingPath))
                return;

            // make a new XML settings document
            var newSettings = new ChannelSettingsModel(chanType == ChannelType.PrivateMessage);
            SerializeObjectToXml(newSettings, workingPath);
        }

        #endregion
    }
}