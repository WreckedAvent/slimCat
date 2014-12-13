#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Models;
    using Utilities;

    #endregion

    /// <summary>
    ///     Allows interaction with persistant settings.
    /// </summary>
    public static class SettingsService
    {
        #region Constants

        private const string SettingsFileName = "!settings.xml";

        private const string ProfileCacheFileName = "{0}.xml";

        private const string GlobalFolderName = "Global";

        private const string DefaultsFolderName = "!Defaults";

        private const string ProfileCacheFolderName = "!Profiles";

        private const string SearchTermsFileName = "!search.xml";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Returns either the channel settings that already exist or a new settings file
        /// </summary>
        public static ChannelSettingsModel GetChannelSettings(
            string currentCharacter, string title, string id, ChannelType chanType)
        {
            Log("Reading settings for " + id);
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
                Log("Settings for {0} could not be read".FormatWith(id));
                return new ChannelSettingsModel(chanType == ChannelType.PrivateMessage);

                // return a default if it's not legible
            }
        }

        public static void SaveProfile(string targetCharacter, ProfileData profileData)
        {
            var workingPath = StaticFunctions.MakeSafeFolderPath(ProfileCacheFolderName, string.Empty, string.Empty);

            if (!Directory.Exists(workingPath))
                Directory.CreateDirectory(workingPath);

            var fileName = Path.Combine(workingPath, ProfileCacheFileName.FormatWith(targetCharacter));

            try
            {
                using (var streamWriter = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    var serializer = new XmlSerializer(typeof (ProfileData));
                    serializer.Serialize(streamWriter, profileData);
                }

                Log("Saved profile cache for " + targetCharacter);
            }
            catch
            {
            }
        }

        public static ProfileData RetrieveProfile(string targetCharacter)
        {
            var workingPath = StaticFunctions.MakeSafeFolderPath(ProfileCacheFolderName, string.Empty, string.Empty);

            if (!Directory.Exists(workingPath))
                return null;

            var fileName = Path.Combine(workingPath, ProfileCacheFileName.FormatWith(targetCharacter));

            if (!File.Exists(fileName))
                return null;

            try
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var serializer = new XmlSerializer(typeof (ProfileData));
                    var toReturn = serializer.Deserialize(stream) as ProfileData;
                    if (toReturn == null) return null;

                    return toReturn.LastRetrieved.AddDays(7) < DateTime.Now ? null : toReturn;
                }
            }
            catch
            {
                return null;
            }
        }

        public static SearchTermsModel RetrieveTerms(string currentCharacter)
        {
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");
            if (!Directory.Exists(path))
                return null;

            path = Path.Combine(path, SearchTermsFileName);

            if (!File.Exists(path))
                return null;

            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var serializer = new XmlSerializer(typeof(SearchTermsModel));
                    return serializer.Deserialize(stream) as SearchTermsModel;
                }
            }
            catch
            {
                return null;
            }
        }

        public static void SaveSearchTerms(string currentCharacter, SearchTermsModel data)
        {
            var workingPath = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");

            if (!Directory.Exists(workingPath))
                Directory.CreateDirectory(workingPath);

            var fileName = Path.Combine(workingPath, SearchTermsFileName);

            try
            {
                using (var streamWriter = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    var serializer = new XmlSerializer(typeof(SearchTermsModel));
                    serializer.Serialize(streamWriter, data);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        ///     Updates the application settings from file
        /// </summary>
        public static void ReadApplicationSettingsFromXml(string currentCharacter, ICharacterManager cm)
        {
            Log("Reading global settings");
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
                                    var setter = property.PropertyType.IsEnum
                                        ? Enum.Parse(property.PropertyType, element.Value)
                                        : Convert.ChangeType(element.Value, property.PropertyType);

                                    property.SetValue(null, setter, null);
                                }
                                catch
                                {
                                }

                                continue;
                            }

                            var collection = ApplicationSettings.SavedChannels;

                            if (property.Name.Equals("recentChannels", StringComparison.OrdinalIgnoreCase))
                                collection = ApplicationSettings.RecentChannels;
                            else if (property.Name.Equals("recentCharacters", StringComparison.OrdinalIgnoreCase))
                                collection = ApplicationSettings.RecentCharacters;

                            if (property.Name.Equals("interested", StringComparison.OrdinalIgnoreCase))
                                cm.Set(element.Elements().Select(x => x.Value), ListKind.Interested);
                            else if (property.Name.Equals("notinterested", StringComparison.OrdinalIgnoreCase))
                                cm.Set(element.Elements().Select(x => x.Value), ListKind.NotInterested);
                            else if (property.Name.Equals("ignoreupdates", StringComparison.OrdinalIgnoreCase))
                                cm.Set(element.Elements().Select(x => x.Value), ListKind.IgnoreUpdates);
                            else
                            {
                                collection.Clear();
                                foreach (var item in element.Elements())
                                    collection.Add(item.Value);
                            }
                        }
                    }
                }

                Log("Global settings read");
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        ///     Return type T from a specified XML file, using reflection
        /// </summary>
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

                        try
                        {
                            var setter = Convert.ChangeType(element.Value, property.PropertyType);
                            property.SetValue(baseObject, setter, null);
                        }
                        catch {}

                        break;
                    }
                }
            }

            return baseObject; // return it
        }

        /// <summary>
        ///     Updates the application settings file from memory
        /// </summary>
        public static void SaveApplicationSettingsToXml(string currentCharacter)
        {
            Log("Saving global settings");
            var root = new XElement("settings");
            var fileName = Path.Combine(
                StaticFunctions.MakeSafeFolderPath(currentCharacter, GlobalFolderName, GlobalFolderName), SettingsFileName);

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
                            else if (property.Name.ToLower().Contains("interested") ||
                                     property.Name.ToLower().Contains("character"))
                                label = "character";

                            toAdd.Add(new XElement(label, item));
                        }
                    }

                    root.Add(toAdd);
                }
            }

            if (File.Exists(fileName))
            {
                var old = fileName + ".old";
                if (File.Exists(old))
                {
                    File.Delete(old);
                }

                File.Move(fileName, old);
                File.Delete(fileName);
            }

            using (var fs = File.OpenWrite(fileName))
                root.Save(fs);

            if (!ApplicationSettings.TemplateCharacter.Equals(currentCharacter)) return;

            var workingPath = StaticFunctions.MakeSafeFolderPath(DefaultsFolderName, GlobalFolderName,
                GlobalFolderName);
            if (!Directory.Exists(workingPath))
                Directory.CreateDirectory(workingPath);

            workingPath = Path.Combine(workingPath, SettingsFileName);

            File.Copy(fileName, workingPath, true);
        }

        /// <summary>
        ///     Serialize and object to XML through reflection
        /// </summary>
        public static void SerializeObjectToXml(object toSerialize, string fileName, bool encrypt = false,
            string rootName = "settings")
        {
            var type = toSerialize.GetType();
            var checkTerms = new[] {"command", "is", "enumerable"};
            var root = new XElement(rootName);

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
        public static void UpdateSettingsFile(object newSettingsModel, string currentCharacter, string title, string id)
        {
            Log("Updating settings for " + id);
            var workingPath = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);
            workingPath = Path.Combine(workingPath, SettingsFileName);

            SerializeObjectToXml(newSettingsModel, workingPath);

            if (!ApplicationSettings.TemplateCharacter.Equals(currentCharacter))
                return;

            workingPath = StaticFunctions.MakeSafeFolderPath(DefaultsFolderName, title, id);

            if (!Directory.Exists(workingPath))
                Directory.CreateDirectory(workingPath);

            workingPath = Path.Combine(workingPath, SettingsFileName);
            SerializeObjectToXml(newSettingsModel, workingPath);
        }

        #endregion

        #region Methods

        private static void MakeGlobalSettingsFileIfNotExist(string currentCharacter)
        {
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, GlobalFolderName, GlobalFolderName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var workingPath = Path.Combine(path, SettingsFileName);

            if (File.Exists(workingPath))
                return;

            if (CopyDefaultGlobalSettingsIfExist(currentCharacter))
                return;

            Log("Global settings could not be restored; regenerating");
            SaveApplicationSettingsToXml(currentCharacter);
        }

        private static ChannelSettingsModel GetDefaultSettings(string title, string id, bool isPm)
        {
            var path = StaticFunctions.MakeSafeFolderPath(DefaultsFolderName, title, id);
            var baseObj = new ChannelSettingsModel(isPm);

            if (!Directory.Exists(path))
                return baseObj;

            var workingPath = Path.Combine(path, SettingsFileName);

            if (!File.Exists(workingPath))
                return baseObj;

            return ReadObjectFromXml(workingPath, new ChannelSettingsModel(isPm));
        }

        private static bool CopyDefaultGlobalSettingsIfExist(string currentCharacter)
        {            
            var destPath =
                Path.Combine(StaticFunctions.MakeSafeFolderPath(currentCharacter, GlobalFolderName, GlobalFolderName),
                    SettingsFileName);


            var backup = Path.Combine(
                StaticFunctions.MakeSafeFolderPath(currentCharacter, GlobalFolderName, GlobalFolderName), (SettingsFileName + ".old"));

            if (File.Exists(backup))
            {
                Log("Restoring global settings from backup");
                File.Copy(backup, destPath);
                return true;
            }

            var path = StaticFunctions.MakeSafeFolderPath(DefaultsFolderName, GlobalFolderName, GlobalFolderName);

            if (!Directory.Exists(path))
                return false;

            var sourcePath = Path.Combine(path, SettingsFileName);

            if (!File.Exists(sourcePath))
                return false;

            Log("Restoring global settings from default settings");
            File.Copy(sourcePath, destPath);

            return true;
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
            var newSettings = GetDefaultSettings(title, id, chanType == ChannelType.PrivateMessage);
            Log("Making new settings for " + id);
            SerializeObjectToXml(newSettings, workingPath);
        }
        private static void Log(string log)
        {
            Logging.LogLine(log, "setting serv");
        }

        #endregion
    }
}