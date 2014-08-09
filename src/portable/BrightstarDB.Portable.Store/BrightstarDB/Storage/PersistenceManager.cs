using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Portable.Compatibility;
using Windows.Storage;

namespace BrightstarDB.Storage
{
    public class PersistenceManager : IPersistenceManager
    {
        private static readonly StorageFolder AppDataLocation = ApplicationData.Current.LocalFolder;

        public bool FileExists(string pathName)
        {
            try
            {
                var t = AppDataLocation.GetFileAsync(pathName).AsTask();
                var result = t.Result;
                return result != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void DeleteFile(string pathName)
        {
            var fileTask = AppDataLocation.GetFileAsync(pathName).AsTask();
            var deleteTask = fileTask.Result.DeleteAsync().AsTask();
            deleteTask.Wait();
        }

        public bool DirectoryExists(string pathName)
        {
            try
            {
                var t = AppDataLocation.GetFolderAsync(pathName).AsTask();
                return t.Result != null;
            }
            catch (AggregateException agg)
            {
                if (agg.InnerExceptions.Any(ex => ex is FileNotFoundException))
                {
                    return false;
                }
                throw;
            }
        }

        public void CreateDirectory(string dirName)
        {
            var t = AppDataLocation.CreateFolderAsync(dirName).AsTask();
            var result = t.Result;
        }

        public void DeleteDirectory(string dirName)
        {
            var t = AppDataLocation.GetFolderAsync(dirName).AsTask();
            var folder = t.Result;
            var deleteTask = folder.DeleteAsync();
            deleteTask.AsTask().Wait();
        }

        public void CreateFile(string pathName)
        {
            var task = AppDataLocation.CreateFileAsync(pathName).AsTask();
            var result = task.Result;
        }

        public Stream GetOutputStream(string pathName, FileMode mode)
        {
            bool throwOnNotExist = false, throwOnExist = false;
            switch (mode)
            {
                case FileMode.Append:
                case FileMode.Create:
                case FileMode.OpenOrCreate:
                    break;
                case FileMode.CreateNew:
                    throwOnExist = true;
                    break;
                case FileMode.Open:
                case FileMode.Truncate:
                    throwOnNotExist = true;
                    break;
            }
            StorageFile file;

            try
            {
                file = AppDataLocation.GetFileAsync(pathName).AsTask().Result;
                if (throwOnExist)
                {
                    throw new IOException(String.Format("File {0} already exists", pathName));
                }
            }
            catch (AggregateException agg)
            {
                if (agg.InnerExceptions.Any(x => x is FileNotFoundException))
                {
                    if (throwOnNotExist)
                    {
                        throw new FileNotFoundException(pathName);
                    }
                    file = AppDataLocation.CreateFileAsync(pathName).AsTask().Result;
                }
                else
                {
                    throw;
                }
            }

            var stream = file.OpenAsync(FileAccessMode.ReadWrite).AsTask().Result.AsStreamForWrite();
            if (mode == FileMode.Append)
            {
                stream.Seek(0, SeekOrigin.End);
            }
            if (mode == FileMode.Truncate || mode == FileMode.Create)
            {
                stream.SetLength(0);
            }
            return stream;
        }

        public Stream GetInputStream(string pathName)
        {
            var file = AppDataLocation.GetFileAsync(pathName).AsTask().Result;
            return file.OpenAsync(FileAccessMode.Read).AsTask().Result.AsStreamForRead();
        }

        public long GetFileLength(string pathName)
        {
            IStorageFile file;
            try
            {
                file = AppDataLocation.GetFileAsync(pathName).AsTask().Result;
            }
            catch (AggregateException agg)
            {
                if (agg.InnerExceptions.Any(x => x is FileNotFoundException))
                {
                    return 0;
                }
                throw;
            }
            var properties = file.GetBasicPropertiesAsync().AsTask().Result;
            return (long) properties.Size;
        }

        public IEnumerable<string> ListSubDirectories(string dirName)
        {
            try
            {
                var folder = AppDataLocation.GetFolderAsync(dirName).AsTask().Result;
                return folder.GetFoldersAsync().AsTask().Result.Select(f => f.Name).ToList();
            }
            catch (AggregateException agg)
            {
                if (agg.InnerExceptions.Any(x => x is FileNotFoundException))
                {
                    throw new FileNotFoundException("The system cannot find the file specified.", dirName);
                }
                throw;
            }
        }

        public void RenameFile(string sourceFileName, string destinationFileName)
        {
            var t = AppDataLocation.GetFileAsync(sourceFileName).AsTask()
                                    .ContinueWith(x => x.Result.RenameAsync(destinationFileName));
            t.RunSynchronously();
        }

        public void CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite)
        {
            var destinationFolderPath = System.IO.Path.GetDirectoryName(destinationFilePath);
            var destinationFileName = System.IO.Path.GetFileName(destinationFilePath);
            var destinationFolder = AppDataLocation.GetFolderAsync(destinationFolderPath);
            var t = AppDataLocation.GetFileAsync(sourceFilePath).AsTask()
                                   .ContinueWith(
                                       x =>
                                       x.Result.CopyAsync(destinationFolder.GetResults(), destinationFileName,
                                                          overwrite
                                                              ? NameCollisionOption.ReplaceExisting
                                                              : NameCollisionOption.FailIfExists));
            t.RunSynchronously();
        }
    }
}
