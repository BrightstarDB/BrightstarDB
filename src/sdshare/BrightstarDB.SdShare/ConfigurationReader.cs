using System;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using BrightstarDB.SdShare.Client;

namespace BrightstarDB.SdShare
{
    public class ConfigurationReader
    {
        private static ServerConfiguration _configuration;

        public static ServerConfiguration Configuration
        {            
            get
            {
                if (_configuration == null)
                {
                    try
                    {
                        var configLocation = ConfigurationManager.AppSettings["SdShare.ConfigurationLocation"];
                        if (configLocation == null) throw new Exception("No SdShare.ConfigurationLocation specified in AppSettings.");

                        var fileInfo = new FileInfo(configLocation);
                        if (!fileInfo.Exists) { throw new Exception("SdShare.ConfigurationLocation references a file that does not exist."); }

                        _configuration = ReadConfiguration(configLocation);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Error processing configuration {0} in {1}", ex.Message, ex.StackTrace));
                    }
                }
                return _configuration;
            }
        }

        private static ServerConfiguration ReadConfiguration(string fileName)
        {
            if (!File.Exists(fileName)) throw new Exception("No file found of name " + fileName);
            var sconfig =  new ServerConfiguration();
            XDocument config = null;
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    config = XDocument.Load(fs);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to read configuration file " + ex.Message + " " + ex.StackTrace);
            }

            // get logging location
            sconfig.LoggingLocation = GetElementValue(config.Root, "LoggingLocation");
            var dirInfo = new DirectoryInfo(sconfig.LoggingLocation);
            if (!dirInfo.Exists) dirInfo.Create();

            // get hashvaluelocation
            sconfig.HashValueStorageLocation = GetElementValue(config.Root, "HashValueStorageLocation");
            dirInfo = new DirectoryInfo(sconfig.HashValueStorageLocation);
            if (!dirInfo.Exists) dirInfo.Create();

            // location for client updating
            sconfig.LastUpdatedStorageLocation = GetElementValue(config.Root, "LastUpdatedStorageLocation");
            dirInfo = new DirectoryInfo(sconfig.LastUpdatedStorageLocation);
            if (!dirInfo.Exists) dirInfo.Create();

            // get all the providers, create an instance and pass through the element to it to process.
            foreach (var element in config.Descendants("CollectionProvider"))
            {
                try
                {
                    var typeNameAttribute = element.Attribute("Type");
                    var assemblyNameAttribute = element.Attribute("Assembly");
                    if (typeNameAttribute != null && assemblyNameAttribute != null)
                    {
                        var typeName = typeNameAttribute.Value;
                        var assemblyName = assemblyNameAttribute.Value;
                        var type = Type.GetType(typeName + ", " + assemblyName);
                        var collectionProvider = Activator.CreateInstance(type) as ICollectionProvider;
                        if (collectionProvider == null) throw new Exception("Unable to instantaite " + assemblyName + " : "  + typeName);
                        collectionProvider.Initialize(element);
                        sconfig.AddCollectionProvider(collectionProvider);
                    }
                } catch (Exception ex)
                {
                    // log error initialising collection provider
                    Logging.LogError(1, "Unable to initialise collection provider {0} {1}", ex.Message, ex.StackTrace);
                    throw;
                }
            }

            // get all feed sources
            foreach (var element in config.Descendants("FeedSource"))
            {
                try
                {
                    var fs = new FeedSource();
                    fs.Initialize(element);
                    sconfig.AddFeedSource(fs);
                }
                catch (Exception ex)
                {
                    // log error initialising collection provider
                    Logging.LogError(1, "Unable to initialise feed source {0} {1}", ex.Message, ex.StackTrace);
                    throw;
                }
            }

            // client adaptors
            foreach (var element in config.Descendants("ClientAdaptor"))
            {
                try
                {
                    var typeNameAttribute = element.Attribute("Type");
                    var assemblyNameAttribute = element.Attribute("Assembly");
                    if (typeNameAttribute != null && assemblyNameAttribute != null)
                    {
                        var typeName = typeNameAttribute.Value;
                        var assemblyName = assemblyNameAttribute.Value;
                        var type = Type.GetType(typeName);
                        var clientAdaptor = Activator.CreateInstance(type) as ISdShareClientAdaptor;
                        if (clientAdaptor == null) throw new Exception("Unable to instantaite " + assemblyName + " : " + typeName);
                        clientAdaptor.Initialize(element);
                        sconfig.AddClientAdaptor(clientAdaptor);
                    }
                }
                catch (Exception ex)
                {
                    // log error initialising collection provider
                    Logging.LogError(1, "Unable to initialise client adaptor {0} {1}", ex.Message, ex.StackTrace);
                    throw;
                }
            }
           
            return sconfig;
        }

        protected static string GetElementValue(XElement parent, string name)
        {
            var elem = parent.Elements(name).FirstOrDefault();
            if (elem == null) throw new Exception("Missing element " + name);
            return elem.Value;
        }
    }
}
