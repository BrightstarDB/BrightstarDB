using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Compatibility;
using BrightstarDB.Portable.Adaptation;

namespace BrightstarDB.Portable.Compatibility
{
    public static class Path
    {
        public static readonly char DirectorySeparatorChar = '\\';
        public static readonly char AltDirectorySeparatorChar = '/';
        public static readonly char VolumeSeparatorChar = ':';


        static Path()
        {
            var provider = PlatformAdapter.Resolve<IPathSeparatorProvider>();
            if (provider != null)
            {
                DirectorySeparatorChar = provider.DirectorySeparator;
                AltDirectorySeparatorChar = provider.AltDirectorySeparator;
                VolumeSeparatorChar = provider.VolumeSeparator;
            }
        }

        public static string GetFileName(string path)
        {
            var ix = path.LastIndexOf(DirectorySeparatorChar) + 1;
            if (ix < path.Length)
            {
                return path.Substring(ix);
            }
            return null;
        }

        public static string GetExtension(string path)
        {
            var fileName = GetFileName(path);
            if (fileName != null && fileName.Contains("."))
            {
                return fileName.Substring(fileName.LastIndexOf('.'));
            }
            return null;
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            var fileName = GetFileName(path);
            if (fileName != null && fileName.Contains("."))
            {
                return fileName.Substring(0, fileName.LastIndexOf('.'));
            }
            return null;
        }

        public static string GetDirectoryName(string path)
        {
            var ix = path.LastIndexOfAny(new char[] { DirectorySeparatorChar, AltDirectorySeparatorChar });
            if (ix > 0)
            {
                return path.Substring(0, ix);
            }
            return null;
        }

        public static string Combine(string path1, string path2)
        {
            if (path2[0] == DirectorySeparatorChar || path2[0] == AltDirectorySeparatorChar ||
                (path2.Length > 1 && path2[1] == VolumeSeparatorChar))
            {
                // path2 is absolute so just return path2
                return path2;
            }
            return path1 + DirectorySeparatorChar + path2;
        }
    }
}
