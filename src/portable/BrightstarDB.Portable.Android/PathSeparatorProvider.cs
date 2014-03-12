namespace BrightstarDB.Compatibility
{
    /// <summary>
    /// Implementation of the IPathSeparatorProvider interface for Android platform
    /// </summary>
    public class PathSeparatorProvider : IPathSeparatorProvider
    {
        /// <summary>
        /// The default directory separator char (/)
        /// </summary>
        public char DirectorySeparator { get { return '/'; } }

        /// <summary>
        /// The alternate directory separator (\)
        /// </summary>
        public char AltDirectorySeparator { get { return '\\'; } }

        /// <summary>
        /// The default volume separator (:)
        /// </summary>
        public char VolumeSeparator { get { return ':'; } }
    }
}