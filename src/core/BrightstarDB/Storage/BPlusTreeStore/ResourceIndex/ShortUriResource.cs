using System.Text;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class ShortUriResource : IResource
    {
        private readonly string _value;

        public ShortUriResource(string uri)
        {
            _value = uri;
        }

        public ShortUriResource(byte[] btreeValue)
        {
            var length = btreeValue[1];
            _value = Encoding.UTF8.GetString(btreeValue, 2, length);
        }

        #region Implementation of IResource

        public byte[] GetData()
        {
            var buff = new byte[64];
            buff[0] = (byte) ResourceHeaderFlags.IsShort;
            var valueBytes = Encoding.UTF8.GetBytes(_value);
            buff[1] = (byte) valueBytes.Length;
            valueBytes.CopyTo(buff, 2);
            return buff;
        }

        public bool Matches(string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId)
        {
            return !isLiteral && resourceValue.Equals(_value);
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
