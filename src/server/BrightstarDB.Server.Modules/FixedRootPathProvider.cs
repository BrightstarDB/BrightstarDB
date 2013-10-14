using System.IO;
using Nancy;

namespace BrightstarDB.Server.Modules
{
    public class FixedRootPathProvider : IRootPathProvider
    {
        private readonly string _rootPath;

        public FixedRootPathProvider(string rootPath)
        {
            _rootPath = rootPath == null ? null : Path.GetFullPath(rootPath);
        }

        public string GetRootPath()
        {
            return _rootPath;
        }
    }
}