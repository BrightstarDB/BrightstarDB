using System;
using System.Text;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class LongLiteralResource : IResource
    {
        private string _value;
        private readonly ulong _valuePage;
        private readonly byte _valueSegment;
        private readonly string _prefix;
        private readonly ulong _dataTypeId;
        private readonly ulong _langCodeId;
        private readonly IResourceTable _resourceTable;
        private const int MaxPrefixBytes = 37;

        public LongLiteralResource(string value, ulong dataTypeId, ulong langCodeId, ulong valuePage, byte valueSeg)
        {
            _value = value;
            _dataTypeId = dataTypeId;
            _langCodeId = langCodeId;
            int prefixLength = Math.Min(MaxPrefixBytes, value.Length); // value length may be less than MaxPrefixBytes if the string contains multi-byte chars
            while (Encoding.UTF8.GetByteCount(value.Substring(0, prefixLength)) > MaxPrefixBytes)
            {
                prefixLength--;
            }
            _prefix = value.Substring(0, prefixLength);
            _value = value;
            _valuePage = valuePage;
            _valueSegment = valueSeg;
        }

        public LongLiteralResource(IResourceTable resourceTable, byte[] data)
        {
            _langCodeId = BitConverter.ToUInt64(data, 1);
            _dataTypeId = BitConverter.ToUInt64(data, 9);
            _valuePage = BitConverter.ToUInt64(data, 17);
            _valueSegment = data[25];
            var prefixLength = (int) data[26];
            _prefix = Encoding.UTF8.GetString(data, 27, prefixLength);
            _resourceTable = resourceTable;
        }

        #region Implementation of IResource

        public byte[] GetData()
        {
            var buff = new byte[64];
            buff[0] = (byte)(ResourceHeaderFlags.IsLiteral);
            BitConverter.GetBytes(_langCodeId).CopyTo(buff, 1);
            BitConverter.GetBytes(_dataTypeId).CopyTo(buff, 9);
            BitConverter.GetBytes(_valuePage).CopyTo(buff, 17);
            buff[25] = _valueSegment;
            buff[26] = (byte)_prefix.Length;
            Encoding.UTF8.GetBytes(_prefix).CopyTo(buff, 27);
            return buff;
        }

        public bool Matches(string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId)
        {
            if ( isLiteral && dataTypeId == _dataTypeId && langCodeId == _langCodeId && resourceValue.StartsWith(_prefix))
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
            get { return true; }
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
            get { return _dataTypeId; }
        }

        /// <summary>
        /// Gets the resource ID for the language code
        /// </summary>
        public ulong LanguageCodeId
        {
            get { return _langCodeId; }
        }

        #endregion
    }
}
