using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using FileMode = BrightstarDB.Portable.Compatibility.FileMode;

namespace BrightstarDB.Storage
{
    public class PersistenceManager : IPersistenceManager
    {
        public bool FileExists(string pathName)
        {
            return File.Exists(pathName);
        }

        public void DeleteFile(string pathName)
        {
            WrapSharingViolations(()=>File.Delete(pathName));
        }

        public bool DirectoryExists(string pathName)
        {
            return Directory.Exists(pathName);
        }

        public void CreateDirectory(string dirName)
        {
            Directory.CreateDirectory(dirName);
        }

        public void DeleteDirectory(string dirName)
        {
            Directory.Delete(dirName, true);
        }

        public void CreateFile(string pathName)
        {
            WrapSharingViolations(()=>File.Create(pathName).Close());
        }

        public Stream GetOutputStream(string pathName, FileMode mode)
        {
            switch (mode)
            {
                case FileMode.Append:
                    return new FileStream(pathName, System.IO.FileMode.Append, FileAccess.Write, FileShare.Read);
                case FileMode.Create:
                    return new FileStream(pathName, System.IO.FileMode.Create, FileAccess.Write, FileShare.Read);
                case FileMode.CreateNew:
                    return new FileStream(pathName, System.IO.FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                case FileMode.Open:
                    return new FileStream(pathName, System.IO.FileMode.OpenOrCreate, FileAccess.Write,
                        FileShare.Read);
                case FileMode.OpenOrCreate:
                    return new FileStream(pathName, System.IO.FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                case FileMode.Truncate:
                    return new FileStream(pathName, System.IO.FileMode.Truncate, FileAccess.Write, FileShare.Read);
                default:
                    throw new ArgumentException("Invalid file mode", "mode");
            }
        }

        public Stream GetInputStream(string pathName)
        {
            return new FileStream(pathName, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

        public void RenameFile(string sourceFileName, string destinationFileName)
        {
            var fileInfo = new FileInfo(sourceFileName);
            if (fileInfo.Exists)
            {
                WrapSharingViolations(() => fileInfo.MoveTo(destinationFileName), retryCount: 50, waitTime: 500);
            }
        }

        public void CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite)
        {
            var fileInfo = new FileInfo(sourceFilePath);
            WrapSharingViolations(() => fileInfo.CopyTo(destinationFilePath, overwrite), retryCount: 50, waitTime: 500);
        }

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
                return (int)ioe.GetType()
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