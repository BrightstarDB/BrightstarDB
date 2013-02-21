using System;
using System.Security.Cryptography.X509Certificates;
using BrightstarDB.Storage.BTreeStore;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Class that wraps the different store configuration options we support
    /// </summary>
    public class StoreConfiguration
    {

#if !SILVERLIGHT // Not required for SL as it must always use Isolated Storage
        /// <summary>
        /// Boolean flag indicating if the store should be created in / read from 
        /// Isolated Storage rather than the normal file store
        /// </summary>
        public bool UseIsolatedStorage { get; set; }
#endif
        /// <summary>
        /// Get or set the <see cref="System.Type"/> of the 
        /// <see cref="IStoreManager"/> instance to be used by the store.
        /// </summary>
        public Type StoreManagerType { get; set; }

        /// <summary>
        /// Get or set the type of persistence to be used by store data files
        /// </summary>
        public PersistenceType PersistenceType { get; set; }

        /// <summary>
        /// Disables the background writer thread for the append-only store
        /// </summary>
        /// <remarks>Disabling the background writer will ensure that pages are only
        /// ever written once and in-order. This is currently a requirement on Azure</remarks>
        public bool DisableBackgroundWrites { get; set; }

        /// <summary>
        /// Get the default store configuration to be used when no configuration options are passed in
        /// by the caller.
        /// </summary>
        public static readonly StoreConfiguration DefaultStoreConfiguration = new StoreConfiguration();
    }
}
