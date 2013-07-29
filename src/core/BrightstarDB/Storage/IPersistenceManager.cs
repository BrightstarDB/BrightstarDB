using System.Collections.Generic;
using System.IO;

#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Interface that exposes the minimum set of operations required by
    /// the persistence store backing an IStoreManager implementation
    /// </summary>
    public interface IPersistenceManager
    {
        /// <summary>
        /// Returns true if <paramref name="pathName"/> represents
        /// the path to an existing "file" in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        bool FileExists(string pathName);

        /// <summary>
        /// Removes a file from the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        void DeleteFile(string pathName);

        /// <summary>
        /// Returns true if <paramref name="pathName"/> represents
        /// the path to an existing "directory" in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        bool DirectoryExists(string pathName);

        /// <summary>
        /// Creates a new directory in the persistence store.
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        void CreateDirectory(string dirName);

        /// <summary>
        /// Removes a directory and any files or subdirectories within it
        /// from the persistence store
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        void DeleteDirectory(string dirName);

        /// <summary>
        /// Create a new empty file at the specified path location
        /// </summary>
        /// <param name="pathName"></param>
        void CreateFile(string pathName);

        /// <summary>
        /// Opens a stream to write to a file in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="mode">The mode used to open the file</param>
        /// <returns></returns>
        Stream GetOutputStream(string pathName, FileMode mode);

        /// <summary>
        /// Opens a stream to read from a file in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        Stream GetInputStream(string pathName);

        /// <summary>
        /// Returns the length of the specified file in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        long GetFileLength(string pathName);

        /// <summary>
        /// Returns a list of the names of subdirectories of the specified directory.
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        IEnumerable<string> ListSubDirectories(string dirName);

        /// <summary>
        /// Renames a the specified file
        /// </summary>
        /// <param name="sourceFileName">The name of the file to be renamed</param>
        /// <param name="destinationFileName">The new file name</param>
        void RenameFile(string sourceFileName, string destinationFileName);
    }
}
