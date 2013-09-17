using System;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Class that wraps the different store configuration options we support
    /// </summary>
    public class StoreConfiguration 
#if !SILVERLIGHT
        : ICloneable
#endif
    {

#if !SILVERLIGHT // Not required for SL as it must always use Isolated Storage
        /// <summary>
        /// Boolean flag indicating if the store should be created in / read from 
        /// Isolated Storage rather than the normal file store
        /// </summary>
        public bool UseIsolatedStorage { get; set; }
#endif
        
        /// <summary>
        /// The implementation of the abstract persistence layer used by the store.
        /// </summary>
        public IPersistenceManager PersistenceManager { get; set; }

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

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public object Clone()
        {
            return new StoreConfiguration
                {
#if !SILVERLIGHT
                    UseIsolatedStorage = this.UseIsolatedStorage,
#endif
                    PersistenceManager = this.PersistenceManager,
                    StoreManagerType = this.StoreManagerType,
                    PersistenceType = this.PersistenceType,
                    DisableBackgroundWrites = this.DisableBackgroundWrites
                };
        }
    }
}
