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
            var useID = (chanType != Models.ChannelType.pm || chanType != Models.ChannelType.pub);
            var path = buildPathString(CurrentCharacter, Title, ID, useID);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var workingPath = Path.Combine(path, SETTINGS_FILE_NAME);

            if (!File.Exists(workingPath))
            { // make a new XML settings document
                var newSettings = new Models.ChannelSettingsModel(chanType == Models.ChannelType.pm ? true : false);
                SerializeObjectToXML(newSettings, workingPath);
            }
        }

        /// <summary>
        /// Returns either the channel settings that already exist or a new settings file
        /// </summary>
        public static Models.ChannelSettingsModel GetChannelSettings(string CurrentCharacter, string Title, String ID, Models.ChannelType chanType)
        {
            makeSettingsFileIfNotExist(CurrentCharacter, Title, ID, chanType);
            var workingPath = buildPathString(CurrentCharacter, Title, ID, (chanType != Models.ChannelType.pm || chanType != Models.ChannelType.pub));
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
        public static void SerializeObjectToXML(object toSerialize, string fileName)
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

            root = null;
        }

        /// <summary>
        /// Return type T from a specified XML file, using reflection
        /// </summary>
        public static T ReadObjectFromXML<T>(string fileName)
            where T : new()
        {
            var toReturn = new T();
            Type type = toReturn.GetType();
            var propertyList = type.GetProperties(); // reflect property names

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
        /// Updates our settings XML file with newSettingsModel
        /// </summary>
        public static void UpdateSettingsFile(object newSettingsModel, string CurrentCharacter, string Title, string ID, Models.ChannelType chanType)
        {
            var workingPath = buildPathString(CurrentCharacter, Title, ID, (chanType != Models.ChannelType.pm || chanType != Models.ChannelType.pub));
            workingPath = Path.Combine(workingPath, SETTINGS_FILE_NAME);

            SerializeObjectToXML(newSettingsModel, workingPath);
        }

        public static bool HasChannelSettings(string CurrentCharacter, string Title, string ID, Models.ChannelType chanType)
        {
            var useID = (chanType != Models.ChannelType.pm || chanType != Models.ChannelType.pub);
            var path = buildPathString(CurrentCharacter, Title, ID, useID);

            return Directory.Exists(Path.Combine(path, SETTINGS_FILE_NAME));
        }

        private static string buildPathString(string CurrentCharacter, string Title, string ID, bool useID)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderName;

            if (!useID)
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
