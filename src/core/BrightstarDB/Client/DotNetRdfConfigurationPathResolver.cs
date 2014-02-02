using System.IO;
using VDS.RDF.Configuration;

namespace BrightstarDB.Client
{
    internal class DotNetRdfConfigurationPathResolver : IPathResolver
    {
        private readonly string _configurationPath;

        public DotNetRdfConfigurationPathResolver(string configurationPath)
        {
#if PORTABLE
            _configurationPath = Path.GetDirectoryName(configurationPath);
#else
            _configurationPath = Path.GetDirectoryName(Path.GetFullPath(configurationPath));
#endif
        }
        public string ResolvePath(string path)
        {
            return Path.Combine(_configurationPath, path);
        }
    }
}