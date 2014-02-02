using System;
using VDS.RDF;
using VDS.RDF.Configuration;

namespace BrightstarDB.Client
{
    internal static class DotNetRdfConfigurationHelper
    {
#if PORTABLE
        public static IGraph LoadConfiguration(string configurationPath)
        {
            var pm = PlatformAdapter.Resolve<IPersistenceManager>();
            ConfigurationLoader.PathResolver = new DotNetRdfConfigurationPathResolver(configurationPath);
            using (var stream = pm.GetInputStream(configurationPath))
            {
                return ConfigurationLoader.LoadConfiguration(configurationPath, new Uri(configurationPath), stream);
            }
        }
#else
        public static IGraph LoadConfiguration(string configurationPath)
        {
            ConfigurationLoader.PathResolver = new DotNetRdfConfigurationPathResolver(configurationPath);
            return ConfigurationLoader.LoadConfiguration(configurationPath);
        }
#endif

        public static object GetConfigurationObject(IGraph configurationGraph, string id)
        {
            var configNode = configurationGraph.GetUriNode(new Uri(configurationGraph.BaseUri, id));
            return configNode == null ? null : ConfigurationLoader.LoadObject(configurationGraph, configNode);
        }
    }
}