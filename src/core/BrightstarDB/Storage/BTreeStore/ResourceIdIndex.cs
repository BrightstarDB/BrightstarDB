using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Profiling;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BTreeStore
{
    /// <summary>
    /// Wraps a BTree to provide a set of operations for managing a Resouce index.
    /// </summary>
    internal class ResourceIdIndex
    {

        private const ulong NullUlong = 0;

        /// <summary>
        /// The index btree
        /// </summary>
        private readonly PersistentBTree<Bucket> _index;

        /// <summary>
        /// In memory cache of resource ids. The string value is the same as the hashable resource string.
        /// </summary>
        private readonly Dictionary<string, ulong> _resourceIdCache;

        /// <summary>
        /// The store owning this index.
        /// </summary>
        private readonly Store _store;

        /// <summary>
        /// Create a new resource id index.
        /// </summary>
        public ResourceIdIndex(Store store, PersistentBTree<Bucket> index)
        {
            _store = store;
            _index = index;
            _resourceIdCache = new Dictionary<string, ulong>();
        }


        public ulong AssertResourceInIndex(string resourceValue, bool isLiteral = false, string dataType = null, string langCode = null, bool cache = true, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("AssertResourceInIndex"))
            {
                try
                {
                    // Normalize langCode to null if it is an empty string
                    if (String.Empty.Equals(langCode)) langCode = null;

                    ulong dataTypeId = NullUlong;
                    if (dataType != null)
                    {
                        dataTypeId = AssertResourceInIndex(dataType);
                    }

                    Entry<Bucket> nodeEntry;
                    Node<Bucket> node;
                    var hashableResourceString = isLiteral
                                                     ? GetHashableResourceString(resourceValue, dataType, langCode)
                                                     : resourceValue;

                    // see if we have it already
                    ulong resourceId;
                    if (_resourceIdCache.TryGetValue(hashableResourceString, out resourceId))
                    {
                        profiler.Step("Cache Hit");
                        return resourceId;
                    }

                    var hashCode = hashableResourceString.GetBrightstarHashCode();
                    if (!_index.LookupEntry(hashCode, out nodeEntry, out node))
                    {
                        // create a new bucket where several resources might exist
                        var resource = MakeNewResource(MakeId(hashCode, 0), resourceValue, isLiteral, dataTypeId,
                                                       langCode);
                        var resources = new List<Resource> {resource};
                        var bucket = new Bucket
                                         {
                                             Resources = resources,
                                         };

                        // insert into the lexical index
                        _index.Insert(new Entry<Bucket>(hashCode, bucket));

                        // add to cache
                        if (cache) _resourceIdCache.Add(hashableResourceString, resource.Rid);

                        return resource.Rid;
                    }
                    else
                    {
                        var bucket = nodeEntry.Value;
                        var matches =
                            bucket.Resources.Where(
                                r =>
                                r.LexicalValue.Equals(resourceValue) && r.IsLiteral == isLiteral &&
                                r.DataTypeResourceId == dataTypeId & r.LanguageCode == langCode);

                        if (matches.Count() == 0)
                        {
                            resourceId = MakeId(hashCode, (uint) bucket.Resources.Count);
                            var resource = MakeNewResource(resourceId, resourceValue, isLiteral, dataTypeId, langCode);
                            bucket.Resources.Add(resource);
                            _store.AddToCommitList(node);

                            // add to cache
                            if (cache) _resourceIdCache.Add(hashableResourceString, resource.Rid);

                            return resource.Rid;
                        }

                        return matches.Select(r => r.Rid).First();
                    }
                }
                catch (Exception ex)
                {
                    // todo: fix this logging
                    Console.WriteLine("Error with Resource " + resourceValue);
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        public ulong GetResourceId(string resourceValue, bool isLiteral = false, string dataType = null, string langCode = null, bool cache = true)
        {
            Entry<Bucket> nodeEntry;
            Node<Bucket> node;

            // Normalize langCode to null if it is an empty string.
            if (String.Empty.Equals(langCode)) langCode = null;

            var hashableResourceString = isLiteral
                                             ? GetHashableResourceString(resourceValue, dataType, langCode)
                                             : resourceValue;

            // see if we have it already
            ulong resourceId;
            if (_resourceIdCache.TryGetValue(hashableResourceString, out resourceId))
            {
                return resourceId;
            }

            // get the resource id for the dataType if it isnt null.
            var dataTypeId = NullUlong;
            if (dataType != null)
            {
                dataTypeId = GetResourceId(dataType);

                // if there is no id for the data type then there cant be a resource so return null.
                if (dataTypeId == NullUlong) return NullUlong;
            }

            var hashCode = hashableResourceString.GetBrightstarHashCode();
            _index.LookupEntry(hashCode, out nodeEntry, out node);

            if (nodeEntry != null)
            {
                var bucket = nodeEntry.Value;
                var matches =
                    bucket.Resources.Where(
                        r =>
                        r.LexicalValue.Equals(resourceValue) && r.IsLiteral == isLiteral &&
                        r.DataTypeResourceId == dataTypeId &&
                        r.LanguageCode == langCode);
                
                if (matches.Count() == 1)
                {
                    resourceId = matches.Select(r => r.Rid).First();
                    // cache it
                    // todo: I think this could be unsafe when multiple threads are reading from the same store. 
                    // todo: Making this cache a concurrent dictionary would be good.
                    _resourceIdCache.Add(hashableResourceString, resourceId);

                    return resourceId;
                }
            }

            return NullUlong;
        }

        public Resource GetResource(ulong resourceId)
        {
            var hashCode = GetResourceIdHashCode(resourceId);
            var bucketOffset = GetResourceIdBucketOffset(resourceId);

            Entry<Bucket> nodeEntry;
            Node<Bucket> node;
            _index.LookupEntry(hashCode, out nodeEntry, out node);

            if (nodeEntry != null)
            {
                var bucket = nodeEntry.Value;
                return bucket.Resources[(int)bucketOffset];
            }

            return null;
        }

        private static Resource MakeNewResource(ulong resourceId, string resourceValue, bool isLiteral, ulong dataTypeId = NullUlong, string langCode = null)
        {
            Resource resource;

            if (isLiteral)
            {
                resource = new Resource
                {
                    LexicalValue = resourceValue,
                    LanguageCode = langCode,
                    DataTypeResourceId = dataTypeId,
                    IsLiteral = true,
                    Rid = resourceId
                };
            }
            else
            {
                resource = new Resource
                {
                    LexicalValue = resourceValue,
                    Rid = resourceId
                };
            }

            return resource;
        }

        #region Utilities

        private static string GetHashableResourceString(string resourceValue, string dataType = null, string langCode = null)
        {
            var sb = new StringBuilder(resourceValue.Length + 520);
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

        public static ulong MakeId(uint hashCode, uint bucketPosition)
        {
            return hashCode + (((ulong)bucketPosition) << 32);
        }

        public static uint GetResourceIdHashCode(ulong resourceId)
        {
            return (uint)resourceId & uint.MaxValue;
        }

        public static uint GetResourceIdBucketOffset(ulong resourceId)
        {
            return (uint)(resourceId >> 32);
        }

        #endregion
    }
}
