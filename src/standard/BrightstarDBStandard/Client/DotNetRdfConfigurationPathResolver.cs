using VDS.RDF.Configuration;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#else
using System.IO;
#endif

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