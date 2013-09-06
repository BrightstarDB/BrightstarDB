#if !PORTABLE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BrightstarDB.Storage.Persistence
{
    internal class FilePersistenceManager: IPersistenceManager
    {
        #region Implementation of IPersistenceManager

        /// <summary>
        /// Returns true if <paramref name="pathName"/> represents
        /// the path to an existing "file" in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public bool FileExists(string pathName)
        {
            return File.Exists(pathName);
        }

        /// <summary>
        /// Removes a file from the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        public void DeleteFile(string pathName)
        {
            WrapSharingViolations(()=>File.Delete(pathName));
        }

        /// <summary>
        /// Create a new empty file at the specified path location
        /// </summary>
        /// <param name="pathName"></param>
        public void CreateFile(string pathName)
        {
            WrapSharingViolations(()=>File.Create(pathName).Close());
        }

        /// <summary>
        /// Returns true if <paramref name="pathName"/> represents
        /// the path to an existing "directory" in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public bool DirectoryExists(string pathName)
        {
            return Directory.Exists(pathName);
        }

        /// <summary>
        /// Creates a new directory in the persistence store.
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public void CreateDirectory(string dirName)
        {
            Directory.CreateDirectory(dirName);
        }

        /// <summary>
        /// Removes a directory and any files or subdirectories within it
        /// from the persistence store
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public void DeleteDirectory(string dirName)
        {
            Directory.Delete(dirName, true);
        }

        /// <summary>
        /// Opens a stream to write to a file in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="mode">The mode used to open the file</param>
        /// <returns></returns>
        public Stream GetOutputStream(string pathName, FileMode mode)
        {
            return new FileStream(pathName, mode, FileAccess.Write, FileShare.Read, 4096);
        }

        /// <summary>
        /// Opens a stream to read from a file in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public Stream GetInputStream(string pathName)
        {
            return new FileStream(pathName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096);
        }

        public long GetFileLength(string pathName)
        {
            var fileInfo = new FileInfo(pathName);
            return !fileInfo.Exists ? 0 : fileInfo.Length;
        }

        public IEnumerable<string> ListSubDirectories(string dirName)
        {
            var directoryInfo = new DirectoryInfo(dirName);
            return directoryInfo.GetDirectories().Select(d => d.Name);
        }

        public void RenameFile(string storeConsolidateFile, string storeDataFile)
        {
            var fileInfo = new FileInfo(storeConsolidateFile);
            if (fileInfo.Exists)
            {
                WrapSharingViolations(() => fileInfo.MoveTo(storeDataFile), retryCount:50, waitTime:500);
            }
        }

        public void CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite)
        {
            var fileInfo = new FileInfo(sourceFilePath);
            WrapSharingViolations(()=> fileInfo.CopyTo(destinationFilePath, overwrite));
        }

        #endregion

        private static void WrapSharingViolations(WrapSharingViolationsCallback action, WrapSharingViolationsExceptionsCallback exceptionsCallback = null, int retryCount = 10, int waitTime = 100)
        {
            if (action == null) throw new ArgumentNullException("action");
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException ioe)
                {
                    if ((IsSharingViolation(ioe)) && (i < (retryCount - 1)))
                    {
                        bool wait = true;
                        if (exceptionsCallback != null)
                        {
                            wait = exceptionsCallback(ioe, i, retryCount, waitTime);
                        }
                        if (wait)
                        {
                            System.Threading.Thread.Sleep(waitTime);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private delegate void WrapSharingViolationsCallback();

        private delegate bool WrapSharingViolationsExceptionsCallback(
            IOException ioe, int retry, int retryCount, int waitTime);

        static bool IsSharingViolation(IOException ioe)
        {
            if (ioe == null) throw new ArgumentNullException("ioe");
            int hr = GetHResult(ioe, 0);
            return (hr == -2147024864); // 0x80070020 ERROR_SHARING_VIOLATION
        }

        static int GetHResult(IOException ioe, int defaultValue)
        {
            if (ioe == null) throw new ArgumentNullException("ioe");
            try
            {
                return (int) ioe.GetType()
                                 .GetProperty("HResult", BindingFlags.NonPublic | BindingFlags.Instance)
                                 .GetValue(ioe, null);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
#endif