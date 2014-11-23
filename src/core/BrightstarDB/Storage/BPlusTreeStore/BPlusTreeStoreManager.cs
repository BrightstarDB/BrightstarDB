using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Storage.Statistics;
using BrightstarDB.Storage.TransactionLog;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class BPlusTreeStoreManager : IStoreManager
    {
        private readonly StoreConfiguration _storeConfiguration;
        private readonly IPersistenceManager _persistenceManager;

        public const string DataFileName = "data.bs";
        public const string ConsolidateFileName = "consolidate.bs";
        public const string ResourceFileName = "resources.bs";
        internal const int MasterfileHeaderLongCount = 32; // number of long values that comprise the header.
        internal const int MasterfileHeaderSize = MasterfileHeaderLongCount * 8;

        internal const int PageSize = 4096; // 4kB pages

        public BPlusTreeStoreManager(StoreConfiguration configuration, IPersistenceManager persistenceManager)
        {
            _storeConfiguration = configuration;
            _persistenceManager = persistenceManager;
        }

        #region Implementation of IStoreManager

        public IEnumerable<string> ListStores(string baseLocation)
        {
            foreach(var directory in _persistenceManager.ListSubDirectories(baseLocation))
            {
#if SILVERLIGHT || PORTABLE
                // Silverlight does not have a Path.Combine that takes three params
                var path = Path.Combine(Path.Combine(baseLocation, directory), MasterFile.MasterFileName);
#else
                var path = Path.Combine(baseLocation, directory, MasterFile.MasterFileName);
#endif
                if (_persistenceManager.FileExists(path))
                {
                    yield return directory;
                }
            }
        }

        public IStore CreateStore(string storeLocation, bool readOnly)
        {
            return CreateStore(storeLocation, _storeConfiguration.PersistenceType, readOnly);
        }

        public IStore CreateStore(string storeLocation, PersistenceType storePersistenceType, bool readOnly)
        {
            Logging.LogInfo("Create Store {0} with persistence type {1}", storeLocation, storePersistenceType);
            if (_persistenceManager.DirectoryExists(storeLocation))
            {
                throw new StoreManagerException(storeLocation, "Store already exists");
            }

            _persistenceManager.CreateDirectory(storeLocation);

            var dataFilePath = Path.Combine(storeLocation, DataFileName);
            _persistenceManager.CreateFile(dataFilePath);

            var resourceFilePath = Path.Combine(storeLocation, ResourceFileName);
            _persistenceManager.CreateFile(resourceFilePath);

            var targetStoreConfiguration = _storeConfiguration.Clone() as StoreConfiguration;
            targetStoreConfiguration.PersistenceType = storePersistenceType;
            MasterFile.Create(_persistenceManager, storeLocation, targetStoreConfiguration, Guid.NewGuid());
            IPageStore dataPageStore = null;
            switch (storePersistenceType)
            {
                case PersistenceType.AppendOnly:
                    dataPageStore = new AppendOnlyFilePageStore(_persistenceManager, dataFilePath, PageSize, false, _storeConfiguration.DisableBackgroundWrites);
                    break;
                case PersistenceType.Rewrite:
                    dataPageStore = new BinaryFilePageStore(_persistenceManager, dataFilePath, PageSize, false, 0, 1, _storeConfiguration.DisableBackgroundWrites);
                    break;
            }
            IPageStore resourcePageStore = new AppendOnlyFilePageStore(_persistenceManager, resourceFilePath, PageSize, false, _storeConfiguration.DisableBackgroundWrites);
            var resourceTable = new ResourceTable(resourcePageStore);
            using (var store = new Store(storeLocation, dataPageStore, resourceTable))
            {
                store.Commit(Guid.Empty);
            }

            Logging.LogInfo("Store created at {0}", storeLocation);
            return OpenStore(storeLocation, readOnly);
        }

        public IStore OpenStore(string storeLocation, bool readOnly)
        {
            Logging.LogInfo("Open Store {0}", storeLocation);
            var masterFile = MasterFile.Open(_persistenceManager, storeLocation);
            var latestCommitPoint = masterFile.GetLatestCommitPoint();
            var dataFilePath = Path.Combine(storeLocation, DataFileName);
            var resourceFilePath = Path.Combine(storeLocation, ResourceFileName);
            if (_persistenceManager.FileExists(dataFilePath))
            {
                IPageStore dataPageStore = null;
                switch (masterFile.PersistenceType)
                {
                        case PersistenceType.AppendOnly:
                        dataPageStore = new AppendOnlyFilePageStore(_persistenceManager, dataFilePath, PageSize, readOnly, _storeConfiguration.DisableBackgroundWrites);
                        break;
                        case PersistenceType.Rewrite:
                        dataPageStore = new BinaryFilePageStore(_persistenceManager, dataFilePath, PageSize, readOnly, 
                            latestCommitPoint.CommitNumber, latestCommitPoint.NextCommitNumber, _storeConfiguration.DisableBackgroundWrites);
                        break;
                }
                var resourcePageStore = new AppendOnlyFilePageStore(_persistenceManager, resourceFilePath, PageSize, readOnly, _storeConfiguration.DisableBackgroundWrites);
                var resourceTable = new ResourceTable(resourcePageStore);
                var store = new Store(storeLocation, dataPageStore, resourceTable, latestCommitPoint.LocationOffset, null);
                Logging.LogInfo("Store {0} opened successfully", storeLocation);
                return store;
            }
            throw new StoreManagerException(storeLocation, "Data file not found");
        }

        /// <summary>
        /// Opens the store at the specified commit point (always read-only)
        /// </summary>
        /// <param name="storeLocation"></param>
        /// <param name="commitPointId"></param>
        /// <returns></returns>
        public IStore OpenStore(string storeLocation, ulong commitPointId)
        {
            if (_storeConfiguration.PersistenceType == PersistenceType.Rewrite)
            {
                throw new InvalidOperationException("Rewrite page store does not support opening at a previous commit point");
            }
            Logging.LogInfo("Open Store {0} at commit point {1}", storeLocation, commitPointId);
            var dataFilePath = Path.Combine(storeLocation, DataFileName);
            var resourceFilePath = Path.Combine(storeLocation, ResourceFileName);
            var commitPoint = GetMasterFile(storeLocation).GetCommitPoints().FirstOrDefault(cp => cp.LocationOffset == commitPointId);
            if (commitPoint != null)
            {
                if (!_persistenceManager.FileExists(resourceFilePath))
                {
                    throw new StoreManagerException(storeLocation, "Resource file not found");
                }
                if (!_persistenceManager.FileExists(dataFilePath))
                {
                    throw new StoreManagerException(storeLocation, "Data file not found");
                }
                var pageStore = new AppendOnlyFilePageStore(_persistenceManager, dataFilePath, PageSize, true, _storeConfiguration.DisableBackgroundWrites);
                var resourceStore = new AppendOnlyFilePageStore(_persistenceManager, resourceFilePath, PageSize, true, _storeConfiguration.DisableBackgroundWrites);
                var resourceTable = new ResourceTable(resourceStore);
                var store = new Store(storeLocation, pageStore, resourceTable, commitPointId, null);
                return store;

            }
            throw new StoreManagerException(storeLocation, "Commit point not found");
        }

        public bool DoesStoreExist(string storeLocation)
        {
            return _persistenceManager.DirectoryExists(storeLocation) &&
                   _persistenceManager.FileExists(Path.Combine(storeLocation, MasterFile.MasterFileName));
        }

        public void DeleteStore(string storeLocation)
        {
            if (_persistenceManager.DirectoryExists(storeLocation))
            {
                _persistenceManager.DeleteDirectory(storeLocation);
            }
            else
            {
                throw new StoreManagerException(storeLocation, "Store does not exist");
            }
        }

        public virtual ITransactionLog GetTransactionLog(string storeLocation)
        {
            return new PersistentTransactionLog(_persistenceManager, storeLocation);
        }

        public virtual IStoreStatisticsLog GetStatisticsLog(string storeLocation)
        {
            return new PersistentStatisticsLog(_persistenceManager, storeLocation);
        }

        public MasterFile GetMasterFile(string storeLocation)
        {
            return MasterFile.Open(_persistenceManager, storeLocation);
        }

        public IPageStore CreateConsolidationStore(string storeLocation)
        {
            var masterFile = GetMasterFile(storeLocation);
            var storePath = Path.Combine(storeLocation, ConsolidateFileName);
            if (_persistenceManager.FileExists(storePath))
            {
                _persistenceManager.DeleteFile(storePath);
            }
            _persistenceManager.CreateFile(storePath);
            
            switch (masterFile.PersistenceType)
            {
                case PersistenceType.AppendOnly:
                    return new AppendOnlyFilePageStore(_persistenceManager, storePath, PageSize, false, _storeConfiguration.DisableBackgroundWrites);
                case PersistenceType.Rewrite:
                    return new BinaryFilePageStore(_persistenceManager, storePath, PageSize, false, 0, 1, _storeConfiguration.DisableBackgroundWrites);
                default:
                    throw new NotImplementedException(String.Format("No support for creating consolidated store with persistence type {0}", _storeConfiguration.PersistenceType));
            }
        }

        public void ActivateConsolidationStore(string storeLocation)
        {
#if WINDOWS_PHONE || PORTABLE
            var tempFileName = Path.Combine(storeLocation, Guid.NewGuid().ToString("N"));            
#else
            var tempFileName = Path.Combine(storeLocation, Path.GetRandomFileName());
#endif
            var consolidateDataPath = Path.Combine(storeLocation, ConsolidateFileName);
            var storeDataPath = Path.Combine(storeLocation, DataFileName);
            PageCache.Instance.Clear(storeDataPath);
            _persistenceManager.RenameFile(storeDataPath, tempFileName);
            try
            {
                _persistenceManager.RenameFile(consolidateDataPath, storeDataPath);
            }
            catch (Exception)
            {
                _persistenceManager.RenameFile(tempFileName, storeDataPath);
                throw;
            }
            _persistenceManager.DeleteFile(tempFileName);
        }


        public void CreateSnapshot(string srcStoreLocation, string destStoreLocation,
                                   PersistenceType storePersistenceType, ulong commitPointId = StoreConstants.NullUlong)
        {
            Logging.LogInfo("Snapshot store {0} to new store {1} with persistence type {2}", srcStoreLocation,
                            destStoreLocation, storePersistenceType);
            if (_persistenceManager.DirectoryExists(destStoreLocation))
            {
                throw new StoreManagerException(destStoreLocation, "Store already exists");
            }

            // Open the source store for reading
            using (IStore srcStore = commitPointId == StoreConstants.NullUlong
                                         ? OpenStore(srcStoreLocation, true)
                                         : OpenStore(srcStoreLocation, commitPointId))
            {

                // Create the directory for the destination store
                _persistenceManager.CreateDirectory(destStoreLocation);

                // Create empty data file
                var dataFilePath = Path.Combine(destStoreLocation, DataFileName);
                _persistenceManager.CreateFile(dataFilePath);

                // Create initial master file
                var destStoreConfiguration = _storeConfiguration.Clone() as StoreConfiguration;
                destStoreConfiguration.PersistenceType = storePersistenceType;
                var destMasterFile = MasterFile.Create(_persistenceManager, destStoreLocation, destStoreConfiguration,
                                                       Guid.NewGuid());

                // Copy resource files from source store
                var resourceFilePath = Path.Combine(destStoreLocation, ResourceFileName);
                _persistenceManager.CopyFile(Path.Combine(srcStoreLocation, ResourceFileName), resourceFilePath, true);

                // Initialize data page store
                IPageStore destPageStore = null;
                switch (storePersistenceType)
                {
                    case PersistenceType.AppendOnly:
                        destPageStore = new AppendOnlyFilePageStore(_persistenceManager, dataFilePath, PageSize, false,
                                                                    _storeConfiguration.DisableBackgroundWrites);
                        break;
                    case PersistenceType.Rewrite:
                        destPageStore = new BinaryFilePageStore(_persistenceManager, dataFilePath, PageSize, false, 0, 1, _storeConfiguration.DisableBackgroundWrites);
                        break;
                    default:
                        throw new BrightstarInternalException("Unrecognized target store type: " + storePersistenceType);
                }
                
                // Copy Data
                ulong destStorePageId = srcStore.CopyTo(destPageStore, 1ul);

                destPageStore.Close();

                destMasterFile.AppendCommitPoint(
                    new CommitPoint(destStorePageId, 1ul, DateTime.UtcNow, Guid.Empty), true);
            }
        }

        public ulong GetDataSize(string storeLocation)
        {
            ulong ret = 0;
            var dataFilePath = Path.Combine(storeLocation, DataFileName);
            var resourceFilePath = Path.Combine(storeLocation, ResourceFileName);
            if (_persistenceManager.FileExists(dataFilePath))
            {
                ret += (ulong)_persistenceManager.GetFileLength(dataFilePath);
            }
            if (_persistenceManager.FileExists(resourceFilePath))
            {
                ret += (ulong)_persistenceManager.GetFileLength(resourceFilePath);
            }
            return ret;
        }

        #endregion

    }
}
