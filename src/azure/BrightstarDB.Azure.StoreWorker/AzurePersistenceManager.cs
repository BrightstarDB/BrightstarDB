using System;
using System.Collections.Generic;
using System.IO;
using BrightstarDB.Azure.Common;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.StoreWorker
{
    internal class AzurePersistenceManager : IPersistenceManager
    {
        private static readonly CloudStorageAccount StorageAccount;
        private static readonly AzureBlockStore BlockStore;

        static AzurePersistenceManager()
        {
            CloudStorageAccount.SetConfigurationSettingPublisher((key, publisher) => publisher(RoleEnvironment.GetConfigurationSettingValue(key)));
            StorageAccount = CloudStorageAccount.FromConfigurationSetting(AzureConstants.BlockStoreConnectionStringName);
            BlockStore = AzureBlockStore.Instance;
        }

        private CloudBlobClient GetBlobClient()
        {
            return StorageAccount.CreateCloudBlobClient();
        }

        #region Implementation of IPersistenceManager

        /// <summary>
        /// Returns true if <paramref name="pathName"/> represents
        /// the path to an existing "file" in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public bool FileExists(string pathName)
        {
            var client = GetBlobClient();
            var relativePath = AzureConstants.StoreContainerPrefix + pathName.Replace('\\', '/');
            var blobRef = client.GetPageBlobReference(relativePath);
            return blobRef.Exists();
        }

        /// <summary>
        /// Removes a file from the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        public void DeleteFile(string pathName)
        {
            var client = GetBlobClient();
            var relativePath = AzureConstants.StoreContainerPrefix + pathName.Replace('\\', '/');
            var blobRef = client.GetPageBlobReference(relativePath);
            blobRef.DeleteIfExists();
        }

        public void CreateFile(string pathName)
        {
            var client = GetBlobClient();
            var relativePath = AzureConstants.StoreContainerPrefix + pathName.Replace('\\', '/');
            var blobRef = client.GetPageBlobReference(relativePath);
            var containerRef = blobRef.Container;
            if (!containerRef.Exists()) containerRef.Create();
            blobRef.Create(AzureConstants.StoreBlobSize);
            blobRef.Metadata[AzureConstants.BlobDataLengthPropertyName] = "0";
            blobRef.SetMetadata();
        }

        /// <summary>
        /// Returns true if <paramref name="pathName"/> represents
        /// the path to an existing "directory" in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public bool DirectoryExists(string pathName)
        {
            var client = GetBlobClient();
            var container = client.GetContainerReference(AzureConstants.StoreContainerPrefix + pathName);
            return container.Exists();
        }

        /// <summary>
        /// Creates a new directory in the persistence store.
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public void CreateDirectory(string dirName)
        {
            var client = GetBlobClient();
            var container = client.GetContainerReference(AzureConstants.StoreContainerPrefix + dirName);
            container.Create();
        }

        /// <summary>
        /// Removes a directory and any files or subdirectories within it
        /// from the persistence store
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public void DeleteDirectory(string dirName)
        {
            var client = GetBlobClient();
            var container = client.GetContainerReference(AzureConstants.StoreContainerPrefix + dirName);
            if (container.Exists())
            {
                container.Delete();
            }
            BlockStore.InvalidateBlocks(dirName);
        }

        /// <summary>
        /// Opens a stream to write to a file in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="mode">The mode used to open the file</param>
        /// <returns></returns>
        public Stream GetOutputStream(string pathName, FileMode mode)
        {
            return new BlockProviderStream(BlockStore, AzureConstants.StoreContainerPrefix + pathName, mode);
        }

        /// <summary>
        /// Opens a stream to read from a file in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public Stream GetInputStream(string pathName)
        {
            return new BlockProviderStream(BlockStore, AzureConstants.StoreContainerPrefix + pathName);
        }

        /// <summary>
        /// Returns the length of the specified file in the persistence store
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public long GetFileLength(string pathName)
        {
            return BlockStore.GetLength(AzureConstants.StoreContainerPrefix + pathName);
        }

        /// <summary>
        /// Returns a list of the names of subdirectories of the specified directory.
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public IEnumerable<string> ListSubDirectories(string dirName)
        {
            throw new NotImplementedException();
        }

        public void RenameFile(string storeConsolidateFile, string storeDataFile)
        {
            throw new NotImplementedException();
        }

        #endregion
    }


}