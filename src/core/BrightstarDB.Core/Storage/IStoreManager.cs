using System.Collections.Generic;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage
{
    internal interface IStoreManager
    {
        IEnumerable<string> ListStores(string baseLocation);

        IStore CreateStore(string storeLocation, bool readOnly = false, bool withTransactionLog = true);

        /// <summary>
        /// Create a new store with a specific persistence type
        /// </summary>
        /// <param name="storeLocation">The path to the directory that will contain the store</param>
        /// <param name="persistenceType">The persistence type to use for the main store indexes</param>
        /// <param name="readOnly">Specifies whether the created store should be returned open for read/write or read only</param>
        /// <param name="withTransactionLog">Specifies whether the created store should have a transaction log file created for it</param>
        /// <returns>The newly created store, opened for read or read/write depending on the setting of the <paramref name="readOnly"/> parameter</returns>
        IStore CreateStore(string storeLocation, PersistenceType persistenceType, bool readOnly = false, bool withTransactionLog = true);

        /// <summary>
        /// Creates a new store with the default persistence type
        /// </summary>
        /// <param name="storeLocation">The path to the directory that will contain the store</param>
        /// <param name="readOnly">Specifies whether the created store should be returned open for read/write or read only</param>
        /// <returns>The newly created store, opened for read or read/write depending on the setting of the <paramref name="readOnly"/> parameter</returns>
        IStore OpenStore(string storeLocation, bool readOnly = false);

        /// <summary>
        /// Opens the store at the specified commit point (always read-only)
        /// </summary>
        /// <param name="storeLocation"></param>
        /// <param name="commitPointId"></param>
        /// <returns></returns>
        IStore OpenStore(string storeLocation, ulong commitPointId);
        bool DoesStoreExist(string storeLocation);
        void DeleteStore(string storeLocation);

        ITransactionLog GetTransactionLog(string storeLocation);
        MasterFile GetMasterFile(string storeLocation);
        IPageStore CreateConsolidationStore(string storeLocation);
        void ActivateConsolidationStore(string storeLocation);

        /// <summary>
        /// Returns the log of store statistics
        /// </summary>
        /// <param name="storeLocation">The path to the store directory</param>
        /// <returns></returns>
        IStoreStatisticsLog GetStatisticsLog(string storeLocation);


        /// <summary>
        /// Creates a consolidated copy of a store, optionally from a given commit point
        /// </summary>
        /// <param name="srcStoreLocation">The source store to be copied</param>
        /// <param name="destStoreLocation">The location where the destination store should be created</param>
        /// <param name="storePersistenceType">The persistence type to use for the data in the destination store</param>
        /// <param name="commitPointId">OPTIONAL: The commit point in the source store to copy from</param>
        void CreateSnapshot(string srcStoreLocation, string destStoreLocation,
                            PersistenceType storePersistenceType, ulong commitPointId = StoreConstants.NullUlong);

        /// <summary>
        /// Returns the size (in bytes) of the specified store's data.
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <returns>The total size of the index files in the store directory.</returns>
        ulong GetDataSize(string storeName);

    }
}
