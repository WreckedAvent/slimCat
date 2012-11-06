using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Services
{
    public static class SettingsDaemon
    {
        const string SETTINGS_FILE_NAME = "!settings.xml";

        private static void makeSettingsFileIfNotExist(string CurrentCharacter, string Title, string ID, Models.ChannelType chanType)
        {
            var path = buildPathString(CurrentCharacter, Title, ID);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var workingPath = Path.Combine(path, SETTINGS_FILE_NAME);

            if (!File.Exists(workingPath))
            { // make a new XML settings document
                var newSettings = new Models.ChannelSettingsModel(chanType == Models.ChannelType.pm ? true : false);
                SerializeObjectToXML(newSettings, workingPath);
            }
        }

        private static void makeGlobalSettingsFileIfNotExist(string currentCharacter)
        {
            var path = buildPathString(currentCharacter, "Global", "Global");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var workingPath = Path.Combine(path, SETTINGS_FILE_NAME);

            if (!File.Exists(workingPath))
                SaveApplicationSettingsToXML(currentCharacter);
        }

        /// <summary>
        /// Returns either the channel settings that already exist or a new settings file
        /// </summary>
        public static Models.ChannelSettingsModel GetChannelSettings(string CurrentCharacter, string Title, String ID, Models.ChannelType chanType)
        {
            makeSettingsFileIfNotExist(CurrentCharacter, Title, ID, chanType);
            var workingPath = buildPathString(CurrentCharacter, Title, ID);
            workingPath = Path.Combine(workingPath, SETTINGS_FILE_NAME);
            try
            {
                return ReadObjectFromXML<Models.ChannelSettingsModel>(workingPath); // try and parse the XML file
            }
            catch
            {
                return new Models.ChannelSettingsModel(chanType == Models.ChannelType.pm ? true : false); // return a default if it's not legible
            }
        }

        /// <summary>
        /// Serialize and object to XML through reflection
        /// </summary>
        public static void SerializeObjectToXML(object toSerialize, string fileName, bool encrypt = false)
        {
            Type type = toSerialize.GetType();
            string[] checkTerms = new string[] { "command", "is", "enumerable" };
            XElement root = new XElement("settings");

            foreach (var property in type.GetProperties())
            {
                if (!checkTerms.Any(term => property.Name.ToLower().Contains(term)))
                root.Add(
                    new XElement(property.Name, property.GetValue(toSerialize, null)) // reflect its name and value, then write
                    );
            }

            File.Delete(fileName);
            using (var fs = File.OpenWrite(fileName))
                root.Save(fs);

            if (encrypt)
                File.Encrypt(fileName);

            root = null;
        }

        /// <summary>
        /// Return type T from a specified XML file, using reflection
        /// </summary>
        public static T ReadObjectFromXML<T>(string fileName, bool decrypt = false)
            where T : new()
        {
            var toReturn = new T();
            Type type = toReturn.GetType();
            var propertyList = type.GetProperties(); // reflect property names

            if (decrypt)
                File.Decrypt(fileName);

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read)) // open our file
            {
                var workingElement = XElement.Load(fs);
                foreach (var element in workingElement.Descendants()) // iterate through each element
                {
                    foreach (var property in propertyList) // check if the element is one of our properties
                    {
                        if (string.Equals(property.Name, element.Name.ToString(), StringComparison.Ordinal))
                        {
                            var setter = Convert.ChangeType(element.Value, property.PropertyType);
                            property.SetValue(toReturn, setter, null);
                            break;
                        }
                    }
                }
            }

            return toReturn; // return it
        }

        /// <summary>
        /// Updates the application settings from file
        /// </summary>
        public static void ReadApplicationSettingsFromXML(string currentCharacter)
        {
            makeGlobalSettingsFileIfNotExist(currentCharacter);

            Type type = typeof(Models.ApplicationSettings);
            var propertyList = type.GetProperties();
            var path = buildPathString(currentCharacter, "Global", "Global");
            path = Path.Combine(path, SETTINGS_FILE_NAME);

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var workingElement = XElement.Load(fs);
                    foreach (var element in workingElement.Descendants())
                    {
                        foreach (var property in propertyList)
                        {
                            if (string.Equals(property.Name, element.Name.ToString(), StringComparison.Ordinal))
                            {
                                if (!element.HasElements)
                                {
                                    var setter = Convert.ChangeType(element.Value, property.PropertyType);
                                    property.SetValue(null, setter, null);
                                    break;
                                }
                                else
                                {
                                    var collection = Models.ApplicationSettings.SavedChannels;

                                    if (property.Name.Equals("interested", StringComparison.OrdinalIgnoreCase))
                                        collection = Models.ApplicationSettings.Interested;

                                    else if (property.Name.Equals("notinterested", StringComparison.OrdinalIgnoreCase))
                                        collection = Models.ApplicationSettings.NotInterested;

                                    collection.Clear();
                                    foreach (var item in element.Elements())
                                        collection.Add(item.Value);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Updates the application settings file from memory
        /// </summary>
        public static void SaveApplicationSettingsToXML(string currentCharacter)
        {
            XElement root = new XElement("settings");
            var fileName = Path.Combine(buildPathString(currentCharacter, "Global", "Global"), SETTINGS_FILE_NAME);

            foreach (var property in typeof(Models.ApplicationSettings).GetProperties())
            {
                if (property.PropertyType != typeof(IList<string>))
                    root.Add(
                        new XElement(property.Name, property.GetValue(null, null))
                        );
                else
                {
                    var toAdd = new XElement(property.Name);
                    foreach (var item in property.GetValue(null, null) as IEnumerable<string>)
                    {
                        var label = "item";
                        if (property.Name.ToLower().Contains("channel"))
                            label = "channel";
                        else if (property.Name.ToLower().Contains("interested"))
                            label = "character";
                        toAdd.Add(new XElement(label, item));
                    }
                    root.Add(toAdd);
                }
            }

            File.Delete(fileName);
            using (var fs = File.OpenWrite(fileName))
                root.Save(fs);

            root = null;
        }

        /// <summary>
        /// Updates our settings XML file with newSettingsModel
        /// </summary>
        public static void UpdateSettingsFile(object newSettingsModel, string CurrentCharacter, string Title, string ID)
        {
            var workingPath = buildPathString(CurrentCharacter, Title, ID);
            workingPath = Path.Combine(workingPath, SETTINGS_FILE_NAME);

            SerializeObjectToXML(newSettingsModel, workingPath);
        }

        public static bool HasChannelSettings(string CurrentCharacter, string Title, string ID)
        {
            var path = buildPathString(CurrentCharacter, Title, ID);

            return Directory.Exists(Path.Combine(path, SETTINGS_FILE_NAME));
        }

        private static string buildPathString(string CurrentCharacter, string Title, string ID)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderName;

            if (!Title.Equals(ID))
            {
                string safeTitle = Title;
                foreach (var c in Path.GetInvalidPathChars())
                    safeTitle = safeTitle.Replace(c.ToString(), "");

                folderName = safeTitle + ' ' + "(" + ID + ")";
            }
            else
                folderName = Title;

            return Path.Combine(basePath, "slimCat", CurrentCharacter, folderName);
        }
    }
}
