using System.Collections.Generic;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Cache that maps resource strings to resource ids. 
    /// </summary>
    internal class ResourceIdCache
    {
        private readonly Dictionary<string, ulong> _entries;

        public ResourceIdCache()
        {
            _entries = new Dictionary<string, ulong>();
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public int CacheEntryCount
        {
            get { return _entries.Count; }
        }

        public bool TryGetValue(string key, out ulong val)
        {
            return _entries.TryGetValue(key, out val);
        }

        public void Add(string key, ulong val)
        {
            _entries.Add(key, val);
        }
    }
}
