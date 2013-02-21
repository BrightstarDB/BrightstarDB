using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using BrightstarDB.Polaris.Messages;
using GalaSoft.MvvmLight.Messaging;

namespace BrightstarDB.Polaris.Configuration
{
    [XmlRoot("configuration", Namespace = "http://brightstardb.com/schemas/2011/polarisConfiguration")]
    public class PolarisConfigurationModel
    {

        [XmlArray("connectionStrings")]
        [XmlArrayItem("connectionString")]
        public List<NamedConnectionString> ConnectionStrings { get; set; }

        [XmlElement(typeof(PrefixConfiguration), ElementName = "prefix")]
        public List<PrefixConfiguration> Prefixes { get; set; }

        [XmlElement(typeof(NamedSparqlQuery), ElementName = "query")]
        public List<NamedSparqlQuery> SavedQueries { get; set; }

        public PolarisConfigurationModel()
        {
            ConnectionStrings = new List<NamedConnectionString>();
            Prefixes = new List<PrefixConfiguration>();
            SavedQueries = new List<NamedSparqlQuery>();
        }

        private static string GetLegacyConfigurationDirectoryPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "NetworkedPlanet\\Brightstar\\Polaris");
        }

        private static string GetConfigurationPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "BrightstarDB\\Polaris\\configuration.xml");
        }

        public static bool Exists { get { return File.Exists(GetConfigurationPath()); } }
        public static bool LegacyPathExists { get { return Directory.Exists(GetLegacyConfigurationDirectoryPath()); } }
        /// <summary>
        /// Loads the configuration from the default configuration file path
        /// </summary>
        /// <returns></returns>
        public static PolarisConfigurationModel Load()
        {
            var configPath = GetConfigurationPath();
            if (File.Exists(configPath))
            {
                return Load(configPath);
            }
            return new PolarisConfigurationModel();
        }

        public static PolarisConfigurationModel ImportLegacyConfiguration()
        {
            try
            {
                var targetConfigurationFile = new FileInfo(GetConfigurationPath());
                var targetConfigurationDir = targetConfigurationFile.Directory.FullName;
                if (!Directory.Exists(targetConfigurationDir))
                {
                    Directory.CreateDirectory(targetConfigurationDir);
                }
                foreach (var legacyFile in Directory.EnumerateFiles(GetLegacyConfigurationDirectoryPath()))
                {
                    File.Move(legacyFile, Path.Combine(targetConfigurationDir, Path.GetFileName(legacyFile)));
                }
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var oldDirectoryPath= Path.Combine(appDataPath, "NetworkedPlanet\\Brightstar");
                Directory.Delete(oldDirectoryPath, true);
            }
            catch (Exception)
            {
                // If file move fails, just fall through and load the default configuration
            }
            return Load();
        }

        /// <summary>
        /// Loads the configuration from the specified configuration file
        /// </summary>
        /// <param name="path">The full path to the file to load</param>
        /// <returns></returns>
        public static PolarisConfigurationModel Load(string path)
        {
            var ser = new XmlSerializer(typeof (PolarisConfigurationModel));
            using (var inputStream = File.OpenRead(path))
            {
                return ser.Deserialize(inputStream) as PolarisConfigurationModel;
            }
        }

        /// <summary>
        /// Writes the configuration to the default configuration file path
        /// </summary>
        public void Save()
        {
            var configPath = GetConfigurationPath();
            Save(configPath);
        }

        /// <summary>
        /// Writes the configuration to the specified file
        /// </summary>
        /// <param name="configPath">The full path to the configuration file to be written</param>
        public void Save(string configPath)
        {
            if (!File.Exists(configPath))
            {
                var configDir = Path.GetDirectoryName(configPath);
                if (configDir != null)
                {
                    if (!Directory.Exists(configDir))
                    {
                        Directory.CreateDirectory(configDir);
                    }
                }
            }
            var ser = new XmlSerializer(typeof (PolarisConfigurationModel));
            using(var outputStream = File.Open(configPath, FileMode.Create, FileAccess.Write))
            {
                ser.Serialize(outputStream, this);
            }
        }
    }
}
