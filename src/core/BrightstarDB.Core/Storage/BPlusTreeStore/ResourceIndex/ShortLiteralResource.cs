using System;
using System.Text;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class ShortLiteralResource : IResource
    {
        private readonly string _value;
        private readonly ulong _dataTypeId;
        private readonly ulong _langCodeId;

        public ShortLiteralResource(string value, ulong dataTypeId, ulong langCodeId)
        {
            _value = value;
            _dataTypeId = dataTypeId;
            _langCodeId = langCodeId;
        }

        public ShortLiteralResource(byte[] btreeValue)
        {
            _langCodeId = BitConverter.ToUInt64(btreeValue, 1);
            _dataTypeId = BitConverter.ToUInt64(btreeValue, 9);
            var valueLength = btreeValue[17];
            _value = Encoding.UTF8.GetString(btreeValue, 18, valueLength);
        }

        #region Implementation of IResource

        public byte[] GetData()
        {
            var buff = new byte[64];
            buff[0] = (byte) (ResourceHeaderFlags.IsLiteral | ResourceHeaderFlags.IsShort);
            BitConverter.GetBytes(_langCodeId).CopyTo(buff, 1);
            BitConverter.GetBytes(_dataTypeId).CopyTo(buff, 9);
            var valueBytes = Encoding.UTF8.GetBytes(_value);
            buff[17] = (byte) valueBytes.Length;
            valueBytes.CopyTo(buff, 18);
            return buff;
        }

        public bool Matches(string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId)
        {
            return isLiteral && dataTypeId.Equals(_dataTypeId) && langCodeId.Equals(_langCodeId) &&
                   resourceValue.Equals(_value);
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
            get { return _value; }
        }

        /// <summary>
        /// Get the full value string for the resource
        /// </summary>
        public string Value
        {
            get { return _value; }
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