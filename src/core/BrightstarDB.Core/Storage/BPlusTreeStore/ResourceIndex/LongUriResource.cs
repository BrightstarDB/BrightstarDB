using System;
using System.Text;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class LongUriResource : IResource
    {
        private readonly IResourceTable _resourceTable;
        private readonly string _prefix;
        private string _value;
        private readonly ulong _valuePage;
        private readonly byte _valueSegment;
        private const int MaxPrefixBytes = 53;

        public LongUriResource(string value, ulong valuePage, byte valueSegment)
        {
            int prefixLength = Math.Min(MaxPrefixBytes, value.Length);
            while (Encoding.UTF8.GetByteCount(value.Substring(0, prefixLength)) > MaxPrefixBytes)
            {
                prefixLength--;
            }
            _prefix = value.Substring(0, prefixLength);
            _value = value;
            _valuePage = valuePage;
            _valueSegment = valueSegment;
        }

        public LongUriResource(IResourceTable resourceTable, byte[]data)
        {
            _resourceTable = resourceTable;
            var prefixLength = (int) data[1];
            _valuePage = BitConverter.ToUInt64(data, 2);
            _valueSegment = data[10];
            _prefix = Encoding.UTF8.GetString(data, 11, prefixLength);
        }

        #region Implementation of IResource

        public byte[] GetData()
        {
            var buff = new byte[64];
            buff[0] = 0;
            var prefixBytes = Encoding.UTF8.GetBytes(_prefix);
            buff[1] = (byte) prefixBytes.Length;
            BitConverter.GetBytes(_valuePage).CopyTo(buff, 2);
            buff[10] = _valueSegment;
            prefixBytes.CopyTo(buff, 11);
            return buff;
        }

        public bool Matches(string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId)
        {
            if (!isLiteral && resourceValue.StartsWith(_prefix))
            {
                return resourceValue.Equals(Value);
            }
            return false;
        }

        /// <summary>
        /// Get the flag that indicates if this resource represents an RDF literal (true) or a resource (false)
        /// </summary>
        public bool IsLiteral
        {
            get { return false; }
        }

        /// <summary>
        /// Get the embedded prefix string for the resource as stored in the BTree
        /// </summary>
        /// <remarks>Using this property rather than the <see cref="IResource.Value"/> property
        /// avoids de-referencing overhead for long values.</remarks>
        public string Prefix
        {
            get { return _prefix; }
        }

        /// <summary>
        /// Get the full value string for the resource
        /// </summary>
        public string Value
        {
            get
            {
                if (_value == null)
                {
                    _value = _resourceTable.GetResource(_valuePage, _valueSegment, null);
                }
                return _value;
            }
        }

        /// <summary>
        /// Gets the resource ID for the data type URI
        /// </summary>
        public ulong DataTypeId
        {
            get { return StoreConstants.NullUlong; }
        }

        /// <summary>
        /// Gets the resource ID for the language code
        /// </summary>
        public ulong LanguageCodeId
        {
            get { return StoreConstants.NullUlong; }
        }

        #endregion
    }
}
