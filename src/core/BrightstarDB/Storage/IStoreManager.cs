using System.Collections.Generic;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage
{
    internal interface IStoreManager
    {
        IEnumerable<string> ListStores(string baseLocation);
        IStore CreateStore(string storeLocation, bool readOnly = false);
        /// <summary>
        /// Create a new store with a specific persistence type
        /// </summary>
        /// <param name="storeLocation">The path to the directory that will contain the store</param>
        /// <param name="persistenceType">The persistence type to use for the main store indexes</param>
        /// <param name="readOnly">Specifies whether the created store should be returned open for read/write or read only</param>
        /// <returns>The newly created store, opened for read or read/write depending on the setting of the <paramref name="readOnly"/> parameter</returns>
        IStore CreateStore(string storeLocation, PersistenceType persistenceType, bool readOnly = false);

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
    }
}
