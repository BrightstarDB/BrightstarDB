using System;
using System.Text;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    /// <summary>
    /// Manages the index of resource ID to resource value
    /// </summary>
    internal class ResourceIndex : BPlusTree, IPageStoreResourceIndex
    {
        private readonly IResourceCache _resourceCache;
        private readonly IResourceIdCache _resourceIdCache;
        private readonly IResourceStore _resourceStore;

        /// <summary>
        /// Creates a new empty resource index in the specified page store
        /// </summary>
        /// <param name="pageStore"></param>
        /// <param name="resourceTable"></param>
        public ResourceIndex(IPageStore pageStore, IResourceTable resourceTable)  : base(pageStore)
        {
            _resourceCache = new ConcurrentResourceCache();
            _resourceIdCache = new ConcurrentResourceIdCache();
            _resourceStore = new ResourceStore(resourceTable);
        }

        /// <summary>
        /// Opens an existing resource index from the specified page store
        /// </summary>
        /// <param name="pageStore"></param>
        /// <param name="resourceTable">The table used to store long resource strings</param>
        /// <param name="rootNodeId">The ID of the page that contains the root node of the resource index</param>
        public ResourceIndex(IPageStore pageStore, IResourceTable resourceTable, ulong rootNodeId) : base(pageStore, rootNodeId)
        {
            _resourceCache = new ConcurrentResourceCache();
            _resourceIdCache = new ConcurrentResourceIdCache();
            _resourceStore = new ResourceStore(resourceTable);
        }

        #region Implementation of IResourceIndex

        /// <summary>
        /// Ensures that the specified resource is in the resource index and returns its resource ID
        /// </summary>
        /// <param name="txnId">The ID of the current update transaction</param>
        /// <param name="resourceValue">The resource string value</param>
        /// <param name="isLiteral">Boolean flag indicating if the resource is a literal (true) or a URI (false)</param>
        /// <param name="dataType">The resource data-type URI</param>
        /// <param name="langCode">The resource language string</param>
        /// <param name="addToCache">Boolean flag indicating if newly indexed resources should be added to the local cache</param>
        /// <param name="profiler"></param>
        /// <returns>The resource ID for the resource</returns>
        public ulong AssertResourceInIndex(ulong txnId, string resourceValue, bool isLiteral = false, string dataType = null, string langCode = null, bool addToCache = true, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("AssertResourceInIndex"))
            {
                // Normalize language code to null if it is an empty string
                if (String.IsNullOrEmpty(langCode))
                {
                    langCode = null;
                }

                // Retrieve the resource ID for the datatype URI (if any)
                var dataTypeId = String.IsNullOrEmpty(dataType)
                                     ? StoreConstants.NullUlong
                                     : AssertResourceInIndex(txnId, dataType, profiler:profiler);

                var hashString = isLiteral ? MakeHashString(resourceValue, dataType, langCode) : resourceValue;

                ulong resourceId;
                if (_resourceIdCache.TryGetValue(hashString, out resourceId))
                {
                    return resourceId;
                }

                // Get a ulong resource ID for the language code string
                var langCodeId = String.IsNullOrEmpty(langCode)
                                     ? StoreConstants.NullUlong
                                     : AssertResourceInIndex(txnId, langCode, true, profiler:profiler);

                resourceId = AssertResourceInBTree(txnId, resourceValue, isLiteral, dataTypeId, langCodeId,
                                                   StringExtensions.GetBrightstarHashCode(hashString), profiler);
                if (addToCache)
                {
                    _resourceIdCache.Add(hashString, resourceId);
                }
                return resourceId;
            }
        }

        /// <summary>
        /// Retrieves the resource ID for the specified resource
        /// </summary>
        /// <param name="resourceValue">The resource string value</param>
        /// <param name="isLiteral">Boolean flag indicating if the resource is a literal (true) or a URI (false)</param>
        /// <param name="dataType">The resource data-type URI</param>
        /// <param name="langCode">The resource language string</param>
        /// <param name="addToCache">Boolean flag indicating if the retrieved resource ID should be added to the local cache</param>
        /// <param name="profiler"></param>
        /// <returns>The resource ID for the resource or Constants.NullUlong if there is no match</returns>
        public ulong GetResourceId(string resourceValue, bool isLiteral, string dataType, string langCode, bool addToCache, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("ResourceIndex.GetResourceId"))
            {
                ulong resourceId;
                var hashString = isLiteral ? MakeHashString(resourceValue, dataType, langCode) : resourceValue;

                if (_resourceIdCache.TryGetValue(hashString, out resourceId))
                {
                    return resourceId;
                }

                ulong dataTypeId = StoreConstants.NullUlong;
                if (isLiteral && !String.IsNullOrEmpty(dataType))
                {
                    dataTypeId = GetResourceId(dataType, false, null, null, true, profiler);
                    if (dataTypeId.Equals(StoreConstants.NullUlong))
                    {
                        return StoreConstants.NullUlong;
                    }
                }

                ulong langCodeId = StoreConstants.NullUlong;
                if (isLiteral && !String.IsNullOrEmpty(langCode))
                {
                    langCodeId = GetResourceId(langCode, true, null, null, true);
                    if (langCodeId.Equals(StoreConstants.NullUlong))
                    {
                        return StoreConstants.NullUlong;
                    }
                }

                resourceId = FindResourceInBTree(resourceValue, isLiteral, dataTypeId, langCodeId,
                                                 StringExtensions.GetBrightstarHashCode(hashString));
                if (resourceId != StoreConstants.NullUlong)
                {
                    _resourceIdCache.Add(hashString, resourceId);
                }

                return resourceId;
            }
        }


        /// <summary>
        /// Retrieves the resource with the specified resource ID
        /// </summary>
        /// <param name="resourceId">The resource ID to look for</param>
        /// <param name="addToCache">Boolean flag indicating if the returned resource structure should be cached for faster future lookups</param>
        /// <param name="profiler">OPTIONAL: Profiler to use to profile this call</param>
        /// <returns>The corresponding resource or null if no match is found</returns>
        public IResource GetResource(ulong resourceId, bool addToCache, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("ResourceIndex.GetResource"))
            {
                IResource resource;
                if (_resourceCache.TryGetValue(resourceId, out resource))
                {
                    return resource;
                }
                var buff = new byte[64];
                if (Search(resourceId, buff, profiler))
                {
                    resource = _resourceStore.FromBTreeValue(buff);
                    _resourceCache.Add(resourceId, resource);
                    return resource;
                }
                return null;
            }
        }

        #endregion

        #region Implementation of IPageStoreObject
        public new ulong Save(ulong transactionId, BrightstarProfiler profiler)
        {
            return base.Save(transactionId, profiler);
        }

        public ulong Write(IPageStore pageStore, ulong  transactionId, BrightstarProfiler profiler)
        {
            var builder = new BPlusTreeBuilder(pageStore, Configuration);
            return builder.Build(transactionId, Scan(profiler), profiler);
        }

        #endregion
        #region Private functions

        /// <summary>
        /// Converts the components of an RDF literal into a single hashable string
        /// </summary>
        /// <param name="resourceValue">The string value of the literal</param>
        /// <param name="dataType">The data-type URI for the literal (may be null)</param>
        /// <param name="langCode">The language code for the literal (may be null)</param>
        /// <returns>The hashable string representation of the literal</returns>
        private static string MakeHashString(string resourceValue, string dataType, string langCode)
        {
            var sb = new StringBuilder(resourceValue.Length + 200);
            sb.Append(resourceValue);
            if (!String.IsNullOrEmpty(dataType))
            {
                sb.Append("^^");
                sb.Append(dataType);
            }
            if (!String.IsNullOrEmpty(langCode))
            {
                sb.Append("@");
                sb.Append(langCode);
            }
            return sb.ToString();
        }


        private ulong AssertResourceInBTree(ulong txnId, string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId, uint hashCode, BrightstarProfiler profiler)
        {
            using (profiler.Step("AssertResourceInBTree"))
            {
                ulong rangeMin = MakeId(hashCode, 0), rangeMax = MakeId(hashCode, UInt32.MaxValue);
                ulong lastResourceId = StoreConstants.NullUlong;
                foreach (var indexEntry in this.Scan(rangeMin, rangeMax, profiler))
                {
                    var resource = _resourceStore.FromBTreeValue(indexEntry.Value);
                    if (resource.Matches(resourceValue, isLiteral, dataTypeId, langCodeId))
                    {
                        return indexEntry.Key;
                    }
                    lastResourceId = indexEntry.Key;
                }
                // If we reach here, there has been no match so we need to insert a new resource
                using (profiler.Step("Add Resource To BTree"))
                {
                    var newResource = _resourceStore.CreateNew(txnId, resourceValue, isLiteral, dataTypeId, langCodeId, profiler);
                    var resourceKey = MakeId(hashCode, GetBucketPosition(lastResourceId) + 1);
                    Insert(txnId, resourceKey, newResource.GetData());
                    return resourceKey;
                }
            }
        }

        private ulong FindResourceInBTree(string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId, uint hashCode)
        {
            ulong rangeMin = MakeId(hashCode, 0), rangeMax = MakeId(hashCode, UInt32.MaxValue);
            foreach(var indexEntry in Scan(rangeMin, rangeMax, null))
            {
                var resource = _resourceStore.FromBTreeValue(indexEntry.Value);
                if (resource.Matches(resourceValue, isLiteral, dataTypeId, langCodeId))
                {
                    return indexEntry.Key;
                }
            }
            return StoreConstants.NullUlong;
        }

        /// <summary>
        /// Composes a 4-byte hashcode and 4-byte list position into an 8-byte resource id
        /// </summary>
        /// <param name="hashcode"></param>
        /// <param name="bucketposition"></param>
        /// <returns></returns>
        private static ulong MakeId(uint hashcode, uint bucketposition)
        {
            return ((ulong)hashcode << 32) + bucketposition;
        }

        private static uint GetBucketPosition(ulong resourceId)
        {
            return (uint)resourceId & uint.MaxValue;
        }

        #endregion  
    }
}
