using BrightstarDB.Compatibility;

namespace BrightstarDB.Compatibility
{
    public class PathSeparatorProvider : IPathSeparatorProvider
    {
        public char DirectorySeparator { get { return System.IO.Path.DirectorySeparatorChar; } }
        public char AltDirectorySeparator { get { return System.IO.Path.AltDirectorySeparatorChar; } }
        public char VolumeSeparator { get { return System.IO.Path.VolumeSeparatorChar; } }
    }
}
