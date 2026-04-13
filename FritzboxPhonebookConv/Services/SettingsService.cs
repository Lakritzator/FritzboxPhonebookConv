using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using FritzboxPhonebookConv.Models;

namespace FritzboxPhonebookConv.Services
{
    /// <summary>
    /// Application-level settings that are persisted between sessions.
    /// Note: the Fritz.Box password is intentionally NOT persisted for security reasons.
    /// </summary>
    [XmlRoot("Settings")]
    public class AppSettings
    {
        public string Host { get; set; } = "fritz.box";
        public int Port { get; set; } = 49000;
        public string Username { get; set; } = string.Empty;
        public List<XsltProfile> XsltProfiles { get; set; } = new List<XsltProfile>();
        public string LastOutputDirectory { get; set; } = string.Empty;
    }

    /// <summary>
    /// Loads and saves <see cref="AppSettings"/> to
    /// <c>%APPDATA%\FritzboxPhonebookConv\settings.xml</c>.
    /// </summary>
    public static class SettingsService
    {
        private static readonly string SettingsFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FritzboxPhonebookConv");

        private static readonly string SettingsFilePath =
            Path.Combine(SettingsFolder, "settings.xml");

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(AppSettings));

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return new AppSettings();

                using (var stream = File.OpenRead(SettingsFilePath))
                {
                    return (AppSettings)Serializer.Deserialize(stream);
                }
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(SettingsFolder);
            using (var stream = File.Create(SettingsFilePath))
            {
                Serializer.Serialize(stream, settings);
            }
        }
    }
}
