using System;
using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal interface IStoreManager2 : IStoreManager
    {
        void StoreObjects(IEnumerable<IPersistable> objects, ObjectLocationManager objectLocationManager, string storeLocation);

        TObjType ReadObject<TObjType>(Stream dataStream, ulong offset) where TObjType : class, IStorable;        
        TObjType ReadObject<TObjType>(string fileName, ulong offset) where TObjType : class, IStorable;
        // TObjType ReadObject<TObjType>(Store store, string fileName, ulong offset) where TObjType : class, IPersistable;

        /// <summary>
        /// Adds the specified commit point to the master file
        /// </summary>
        /// <param name="storeLocation">The path to the store directory</param>
        /// <param name="commitPoint">The commit point to add</param>
        /// <param name="overwrite">Specifies if the master file should be overwritten with just this commit point. Defaults to false.</param>
        /// <remarks>The <paramref name="overwrite"/> parameter is provided to enable store consolidation operations.</remarks>
        void UpdateMasterFile(string storeLocation, CommitPoint commitPoint, bool overwrite = false);

        Stream GetInputStream(string storeLocation);
        void ConsolidateStore(Store store, string storeLocation, Guid jobId);
    }
}
