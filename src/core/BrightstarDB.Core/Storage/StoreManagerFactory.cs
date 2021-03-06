﻿using System;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.Persistence;
#if BTREESTORE
using BrightstarDB.Storage.BTreeStore;
#endif
#if PORTABLE
using BrightstarDB.Portable.Adaptation;
#endif

namespace BrightstarDB.Storage
{
    internal static class StoreManagerFactory
    {
        public static IStoreManager GetStoreManager(StoreConfiguration storeConfiguration = null)
        {
            if (storeConfiguration == null)
            {
                storeConfiguration = StoreConfiguration.DefaultStoreConfiguration;
            }
            if (storeConfiguration.StoreManagerType != null)
            {
                return
                    Activator.CreateInstance(storeConfiguration.StoreManagerType, storeConfiguration) as IStoreManager;
            }
#if BTREESTORE
#if SILVERLIGHT
            return new IsolatedStorageStoreManager(storeConfiguration);
#else
            return storeConfiguration.UseIsolatedStorage ? (IStoreManager) new IsolatedStorageStoreManager(storeConfiguration) : new FileStoreManager(storeConfiguration);
#endif
#else
#if WINDOWS_PHONE
            return  new BPlusTreeStore.BPlusTreeStoreManager(storeConfiguration, new IsolatedStoragePersistanceManager());
#elif PORTABLE
            storeConfiguration.DisableBackgroundWrites = true;
            return new BPlusTreeStoreManager(storeConfiguration, PlatformAdapter.Resolve<IPersistenceManager>());
#else
            // TODO: See if there is a way to reinstate the old IsolatedStoragePersistenceManager
#if NETSTANDARD16
            if (storeConfiguration.UseIsolatedStorage)
            {
                throw new PlatformNotSupportedException("IsolatedStorage is not supported in .NET Standard 1.x");
            }
            return new BPlusTreeStoreManager(storeConfiguration, new FilePersistenceManager());
#else
            return storeConfiguration.UseIsolatedStorage ? new BPlusTreeStore.BPlusTreeStoreManager(storeConfiguration, new IsolatedStoragePersistanceManager()) : new BPlusTreeStore.BPlusTreeStoreManager(storeConfiguration, new FilePersistenceManager());
#endif       
#endif
#endif


        }
    }
}
