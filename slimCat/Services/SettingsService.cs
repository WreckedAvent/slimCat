#region Copyright

// <copyright file="SettingsService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Models;
    using Utilities;

    #endregion

    /// <summary>
    ///     Allows interaction with persistent settings.
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

        private const string PreferencesFileName = "!preferences.xml";

        private static UserPreferences preferences;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Returns either the channel settings that already exist or a new settings file
        /// </summary>
        public static ChannelSettingsModel GetChannelSettings(
            string currentCharacter, string title, string id, ChannelType chanType)
        {
            Log("Reading settings for " + id);
            var workingPath = StringExtensions.MakeSafeFolderPath(currentCharacter, title, id);
            workingPath = Path.Combine(workingPath, SettingsFileName);

            // if we don't have a settings file, assume we're using defaults
            if (!File.Exists(workingPath))
                return new ChannelSettingsModel(chanType == ChannelType.PrivateMessage);

            try
            {
                // try and parse the XML file
                return ReadObjectFromXml(
                    workingPath, new ChannelSettingsModel(chanType == ChannelType.PrivateMessage));
            }
            catch
            {
                // return a default if it's not legible
                Log($"Settings for {id} could not be read");
                return new ChannelSettingsModel(chanType == ChannelType.PrivateMessage);
            }
        }

        public static void SaveProfile(string targetCharacter, ProfileData profileData)
        {
            var workingPath = StringExtensions.MakeSafeFolderPath(ProfileCacheFolderName, string.Empty, string.Empty);

            var fileName = Path.Combine(workingPath, string.Format(ProfileCacheFileName, targetCharacter));
            Serialize(fileName, profileData);
        }

        public static ProfileData RetrieveProfile(string targetCharacter)
        {
            var workingPath = StringExtensions.MakeSafeFolderPath(ProfileCacheFolderName, string.Empty, string.Empty);

            var fileName = Path.Combine(workingPath, string.Format(ProfileCacheFileName, targetCharacter));

            var toReturn = Deserialize<ProfileData>(fileName);
            if (toReturn == null) return null;

            return toReturn.LastRetrieved.AddDays(7) < DateTime.Now ? null : toReturn;
        }

        public static SearchTermsModel RetrieveTerms(string currentCharacter)
        {
            var path = StringExtensions.MakeSafeFolderPath(currentCharacter, "Global", "Global");
            path = Path.Combine(path, SearchTermsFileName);

            return Deserialize<SearchTermsModel>(path);
        }

        public static void SaveSearchTerms(string currentCharacter, SearchTermsModel data)
        {
            var workingPath = StringExtensions.MakeSafeFolderPath(currentCharacter, "Global", "Global");

            var fileName = Path.Combine(workingPath, SearchTermsFileName);
            Serialize(fileName, data);
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
            var path = StringExtensions.MakeSafeFolderPath(currentCharacter, "Global", "Global");
            path = Path.Combine(path, SettingsFileName);

            // this clusterfuck is why you don't have static classes for settings
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
                        catch
                        {
                        }

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
                StringExtensions.MakeSafeFolderPath(currentCharacter, GlobalFolderName, GlobalFolderName),
                SettingsFileName);

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

            var workingPath = StringExtensions.MakeSafeFolderPath(DefaultsFolderName, GlobalFolderName,
                GlobalFolderName);
            if (!Directory.Exists(workingPath))
                Directory.CreateDirectory(workingPath);

            workingPath = Path.Combine(workingPath, SettingsFileName);

            File.Copy(fileName, workingPath, true);
        }

        public static UserPreferences Preferences
        {
            get
            {
                if (preferences != null) return preferences;

                var path = Path.Combine(GeneralExtensions.BaseFolderPath, PreferencesFileName);
                preferences = Deserialize<UserPreferences>(path) ?? new UserPreferences();

                return preferences;
            }
            set
            {
                preferences = value;
                var path = Path.Combine(GeneralExtensions.BaseFolderPath, PreferencesFileName);
                Serialize(path, value);
            }
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

            if (File.Exists(fileName)) File.Delete(fileName);

            try
            {
                using (var fs = File.OpenWrite(fileName))
                    root.Save(fs);
            }
            catch
            {
                Thread.Sleep(250);
                using (var fs = File.OpenWrite(fileName))
                    root.Save(fs);
            }

            if (encrypt)
                File.Encrypt(fileName);
        }

        /// <summary>
        ///     Updates our settings XML file with newSettingsModel
        /// </summary>
        public static void UpdateSettingsFile(object newSettingsModel, string currentCharacter, string title, string id)
        {
            title = title.TrimEnd(' ');
            id = id.TrimEnd(' ');
            Log("Updating settings for " + id);
            var workingPath = StringExtensions.MakeSafeFolderPath(currentCharacter, title, id);

            if (!Directory.Exists(workingPath))
                Directory.CreateDirectory(workingPath);

            workingPath = Path.Combine(workingPath, SettingsFileName);

            SerializeObjectToXml(newSettingsModel, workingPath);

            if (!ApplicationSettings.TemplateCharacter.Equals(currentCharacter))
                return;

            workingPath = StringExtensions.MakeSafeFolderPath(DefaultsFolderName, title, id);

            if (!Directory.Exists(workingPath))
                Directory.CreateDirectory(workingPath);

            workingPath = Path.Combine(workingPath, SettingsFileName);
            SerializeObjectToXml(newSettingsModel, workingPath);
        }

        #endregion

        #region Methods

        private static void MakeGlobalSettingsFileIfNotExist(string currentCharacter)
        {
            var path = StringExtensions.MakeSafeFolderPath(currentCharacter, GlobalFolderName, GlobalFolderName);

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
            var path = StringExtensions.MakeSafeFolderPath(DefaultsFolderName, title, id);
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
                Path.Combine(StringExtensions.MakeSafeFolderPath(currentCharacter, GlobalFolderName, GlobalFolderName),
                    SettingsFileName);


            var backup = Path.Combine(
                StringExtensions.MakeSafeFolderPath(currentCharacter, GlobalFolderName, GlobalFolderName),
                (SettingsFileName + ".old"));

            if (File.Exists(backup))
            {
                Log("Restoring global settings from backup");
                File.Copy(backup, destPath);
                return true;
            }

            var path = StringExtensions.MakeSafeFolderPath(DefaultsFolderName, GlobalFolderName, GlobalFolderName);

            if (!Directory.Exists(path))
                return false;

            var sourcePath = Path.Combine(path, SettingsFileName);

            if (!File.Exists(sourcePath))
                return false;

            Log("Restoring global settings from default settings");
            File.Copy(sourcePath, destPath);

            return true;
        }

        [Conditional("DEBUG")]
        private static void Log(string log) => Logging.LogLine(log, "setting serv");

        private static T Deserialize<T>(string path)
            where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;

                using (var stream = File.OpenRead(path))
                {
                    var serializer = new XmlSerializer(typeof (T));
                    return serializer.Deserialize(stream) as T;
                }
            }
            catch
            {
                return null;
            }
        }

        private static void Serialize<T>(string path, T toSave)
            where T : class
        {
            try
            {
                var folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                using (var streamWriter = new StreamWriter(path, false, Encoding.UTF8))
                {
                    var serializer = new XmlSerializer(typeof (T));
                    serializer.Serialize(streamWriter, toSave);
                }
            }
            catch
            {
            }
        }

        #endregion
    }
}