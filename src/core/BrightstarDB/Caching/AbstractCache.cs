using System;
using System.Collections.Generic;
using System.IO;
#if SILVERLIGHT || PORTABLE || NETCORE
using Polenter.Serialization;
#else
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace BrightstarDB.Caching
{
    /// <summary>
    /// Abstract base class for Brightstar cache implementations
    /// </summary>
    public abstract class AbstractCache : ICache
    {
        /// <summary>
        /// The maximum cache size
        /// </summary>
        protected long CacheSize;
        private readonly long _lowwaterMark;
        private readonly long _highwaterMark;
        private readonly long _cacheMaxSize;
        /// <summary>
        /// The policy for cache eviction
        /// </summary>
        protected readonly ICacheEvictionPolicy CacheEvictionPolicy;
        private readonly object _cacheLock = new object();

        /// <summary>
        /// Creates a new cache instance
        /// </summary>
        /// <param name="cacheMaxSize">The maximum size of the cache in bytes</param>
        /// <param name="cacheEvictionPolicy">The policy used to maintain cache size</param>
        /// <param name="highwaterMark">The cache size at which the cache eviction policy will be run</param>
        /// <param name="lowwaterMark">The size that the cache eviction policy will attempt to achieve after it has run</param>
        protected AbstractCache(long cacheMaxSize, ICacheEvictionPolicy cacheEvictionPolicy, long highwaterMark = 0, long lowwaterMark = 0)
        {
            CacheSize = 0;
            _cacheMaxSize = cacheMaxSize;
            _highwaterMark = (long)(highwaterMark > 0 ? highwaterMark : cacheMaxSize*0.9);
            _lowwaterMark =
                (long)
                (lowwaterMark > 0
                     ? lowwaterMark
                     : (highwaterMark > 0 ? highwaterMark - (cacheMaxSize*0.25) : cacheMaxSize*0.65));
            if (_lowwaterMark <= 0) _lowwaterMark = highwaterMark;
            CacheEvictionPolicy = cacheEvictionPolicy;
        }

        /// <summary>
        /// Adds a new item to the cache
        /// </summary>
        /// <param name="key">The cache key for the item. Must not be null</param>
        /// <param name="data">The data to be stored as a byte array. Must not be null</param>
        /// <param name="cachePriority">The priority of the item in the cache</param>
        public void Insert(string key, byte[] data, CachePriority cachePriority)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (data == null) throw new ArgumentNullException("data");
            Remove(key);
            if (CacheSize + data.Length > _highwaterMark)
            {
                CacheEvictionPolicy.Run(this, CacheSize - _lowwaterMark);
                if (CacheSize + data.Length > _cacheMaxSize)
                {
                    // If even after cache eviction there is not enough space, return without caching the object
                    return;
                }
            }
            var newEntry = AddEntry(key, data, cachePriority);
            if (newEntry != null)
            {
                CacheEvictionPolicy.NotifyInsert(key, data.Length, cachePriority);
                lock (_cacheLock)
                {
                    CacheSize += newEntry.Size;
                }
            }
        }

        /// <summary>
        /// Looks for an item in the cache and returns the bytes for that item
        /// </summary>
        /// <param name="key">The cache key of the item</param>
        /// <returns>The bytes for the cached item or null if the item is not found in the cache</returns>
        public byte[] Lookup(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            var cacheEntry = GetEntry(key);
            if (cacheEntry == null) return null;
            CacheEvictionPolicy.NotifyLookup(key);
            return cacheEntry.GetBytes();
        }


        /// <summary>
        /// Removes an entry from the cache
        /// </summary>
        /// <param name="key">The key of the cache entry to be removed</param>
        public void Remove(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            long entrySize = RemoveEntry(key);
            lock(_cacheLock)
            {
                CacheSize -= entrySize;
            }
            CacheEvictionPolicy.NotifyRemove(key);
        }

        /// <summary>
        /// Determines if the cache contains an entry under a given key
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <returns>True if an entry with this key is in the cache, false otherwise</returns>
        public bool ContainsKey(string key)
        {
            if (key == null) throw new ArgumentNullException(key);
            return GetEntry(key) != null;
        }

        /// <summary>
        /// Provides an enumeration over the entries in the cache.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<AbstractCacheEntry> ListEntries(); 

        /// <summary>
        /// Implemented in derived classes to add a new entry to the cache
        /// </summary>
        /// <param name="key">The key for the new entry</param>
        /// <param name="data">The data for the new entry</param>
        /// <param name="cachePriority">The entry priority</param>
        /// <returns>The newly created cache entry</returns>
        protected abstract AbstractCacheEntry AddEntry(string key, byte[] data, CachePriority cachePriority);

        /// <summary>
        /// Implemented in dervied classes to retrieve an entry from the cache
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <returns>The cache entry found or null if there was no match on <paramref name="key"/></returns>
        protected abstract AbstractCacheEntry GetEntry(string key);
        
        /// <summary>
        /// Called by the eviction policy to remove an item from the cache
        /// </summary>
        /// <param name="key">The key of the item to remove from the cache</param>
        /// <returns>The number of bytes removed from the cache by this eviction</returns>
        /// <remarks>This method calls the protected RemoveEntry method and then updates the local cache size counter</remarks>
        public long EvictEntry(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            long bytesEvicted = RemoveEntry(key);
            lock (_cacheLock)
            {
                CacheSize -= bytesEvicted;
            }
            return bytesEvicted;
        }

        /// <summary>
        /// Removes the entry with the specified key from the cache
        /// </summary>
        /// <param name="key">The key of the entry to be removed</param>
        /// <returns>The number of bytes of data evicted from the cache as a result of this operation. May be 0 if the key was not found in the cache.</returns>
        protected abstract long RemoveEntry(string key);

        /// <summary>
        /// Deserialize an object from a byte array
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize</typeparam>
        /// <param name="buff">The byte array to deserialize from</param>
        /// <returns>The deserialized object</returns>
        protected virtual T Deserialize<T>(byte[] buff)
        {
            try
            {
#if SILVERLIGHT || PORTABLE
                var settings = new SharpSerializerBinarySettings(BinarySerializationMode.Burst);
                var ms = new MemoryStream(buff);
                var ser = new SharpSerializer(settings);
                var deserialized = ser.Deserialize(ms);
                return (T)deserialized;
#else
                if(typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
                {
                    var instance = Activator.CreateInstance<T>();
                    using (var srcStream = new MemoryStream(buff))
                    {
                        (instance as IBinarySerializable).Read(srcStream);
                    }
                    return instance;
                }
                var ms = new MemoryStream(buff);
                var bf = new BinaryFormatter();
                return (T) bf.Deserialize(ms);
#endif
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.CacheError, "Error deserializing cached object: {0}", ex);
                return default(T);
            }
        }

        /// <summary>
        /// Serialize an object to a byte array
        /// </summary>
        /// <param name="o">The object to be serialized</param>
        /// <returns>The serialized form of the object</returns>
        protected virtual byte[] Serialize(object o)
        {
            try
            {
#if SILVERLIGHT || PORTABLE
            var settings = new SharpSerializerBinarySettings(BinarySerializationMode.Burst);
            var ser = new SharpSerializer(settings);
            var ms = new MemoryStream();
            ser.Serialize(o, ms);
            ms.Close();
#if SILVERLIGHT
            return ms.GetBuffer();
#else
            return ms.ToArray();
#endif
#else
                var ms = new MemoryStream();
                var bf = new BinaryFormatter();
                bf.Serialize(ms, o);
                ms.Close();
                return ms.GetBuffer();
#endif
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.CacheError, "Error serializing object for cache: {0}", ex);
                return null;
            }
        }
    }
}
