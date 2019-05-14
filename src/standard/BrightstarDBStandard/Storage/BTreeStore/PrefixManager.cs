using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class PrefixManager : IStorable, IPrefixManager
    {
        // maps prefix to short value
        private readonly Dictionary<string, string> _prefixMappings;

        // maps short value to prefix
        private readonly Dictionary<string, string> _shortValueMappings;

        public PrefixManager()
        {
            _prefixMappings = new Dictionary<string, string>();
            _shortValueMappings = new Dictionary<string, string>();
        }

        public string MakePrefixedUri(string uri)
        {
            var pos = uri.LastIndexOf("/");
            if (pos < 0) pos = uri.LastIndexOf('#');

            // no match then no prefix
            if (pos < 0 || pos == uri.Length-1) return uri;
            var start = uri.Substring(0, pos + 1);
            var rest = uri.Substring(pos + 1);

            string match;
            if (_prefixMappings.TryGetValue(start, out match))
            {
                return match + ":" + rest;
            } else
            {
                var prefix = "bs" + _prefixMappings.Count;
                _prefixMappings.Add(start, prefix);
                _shortValueMappings.Add(prefix, start);
                return prefix + ":" + rest;
            }
        }

        public string ResolvePrefixedUri(string uri)
        {
            if (!uri.StartsWith("bs")) return uri;
            var pos = uri.IndexOf(':');
            if (pos < 0) throw new BrightstarInternalException("Invalid shortened uri " + uri);

            var shortValue = uri.Substring(0, pos);
            var rest = uri.Substring(pos+1);

            string prefix;
            if (_shortValueMappings.TryGetValue(shortValue, out prefix))
            {
                return prefix + rest;
            } else
            {
                throw new BrightstarInternalException("No match for short prefix");
            }
        }

        public int Save(BinaryWriter dataStream, ulong offset)
        {
            var count = SerializationUtils.WriteVarint(dataStream, (ulong)_shortValueMappings.Count);
            foreach (var shortValueMapping in _shortValueMappings)
            {
                // write out short value to full prefix
                count += SerializationUtils.WriteString(dataStream, shortValueMapping.Key);
                count += SerializationUtils.WriteString(dataStream, shortValueMapping.Value);
            }
            return count;
        }

        public void Read(BinaryReader dataStream)
        {
            var count = SerializationUtils.ReadVarint(dataStream);
            for (ulong i = 0; i < count;i++)
            {
                var shortPrefix = SerializationUtils.ReadString(dataStream);
                var prefix = SerializationUtils.ReadString(dataStream);
                _shortValueMappings.Add(shortPrefix, prefix);
                _prefixMappings.Add(prefix, shortPrefix);
            }
        }
    }
}
