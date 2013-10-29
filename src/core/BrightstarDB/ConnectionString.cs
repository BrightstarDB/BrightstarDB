using System;
using System.Collections.Generic;

namespace BrightstarDB
{
    /// <summary>
    /// Represents a Brightstar connection string
    /// </summary>
    public class ConnectionString
    {
        private const string TypePropertyName = "type";
        private const string EndpointPropertyName = "endpoint";
        private const string StoresDirectoryPropertyName = "storesdirectory";
        private const string StoreNamePropertyName = "storename";
        private const string OptimisticLockingName = "optimisticlocking";
        private const string AccountPropertyName = "account";
        private const string KeyPropertyName = "key";

        private readonly Dictionary<string, string> _values;
        private readonly string _rawValue; 

        /// <summary>
        /// Parses the provided connection string
        /// </summary>
        /// <param name="connectionString">The connection string to be parsed</param>
        public ConnectionString(string connectionString)
        {    
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString", Strings.BrightstarConnectionString_MustMotBeNull);
            }
            if(String.Empty.Equals(connectionString))
            {
                throw new ArgumentException(Strings.BrightstarConnectionString_MustNotBeEmpty, "connectionString");
            }
            _values = new Dictionary<string, string>();
            ParseValues(connectionString);
            _rawValue = connectionString;
        }

        internal string Value { get { return _rawValue; } }

        private void ParseValues(string value)
        {
            var keyValuePairs = value.Split(';');
            foreach (var keyValuePair in keyValuePairs)
            {
                var ix = keyValuePair.IndexOf('=');
                if (ix > 0)
                {
                    var key = keyValuePair.Substring(0, ix).ToLowerInvariant().Trim();
                    var v = keyValuePair.Substring(ix + 1);
                    _values[key] = v;
                }
                /*
                var keyValue = keyValuePair.Split('=');
                if (keyValue.Count() >= 2)
                {
                    _values.Add(keyValue[0].ToLower(), keyValue[1]);
                }
                 */
            }
            if (!_values.ContainsKey(TypePropertyName))
            {
                throw new FormatException(
                    String.Format("The connection string '{0}' does not contain the required Type parameter.", value));
            }
            var type = _values[TypePropertyName].ToLowerInvariant();
            if (type.Equals("embedded"))
            {
                Type = ConnectionType.Embedded;
                AssertStoresDirectory();
            }
            else if (type.Equals("http") || type.Equals("tcp") || type.Equals("namedpipe"))
            {
                throw new FormatException(String.Format(Strings.BrightstarConnectionString_ObsoleteType, type));
            }
            else if (type.Equals("rest"))
            {
                Type = ConnectionType.Rest;
                AssertEndpoint();
            }
            else
            {
                throw new FormatException(String.Format(
                    "Unrecognized connection type '{0}' in connection string '{1}'", type, value));
            }

            if (_values.ContainsKey(OptimisticLockingName))
            {
                bool optLock;
                if (!Boolean.TryParse(_values[OptimisticLockingName], out optLock))
                {
                    throw new FormatException(String.Format(
                        "Unrecognized value for '{0}' property in connection string. Expected 'true' or 'false'", OptimisticLockingName));
                }
                OptimisticLocking = optLock;
            }
        }

        private void AssertStoresDirectory()
        {
            if (string.IsNullOrEmpty(StoresDirectory))
            {
                throw new FormatException("No StoresDirectory parameter found in the connection string.");
            }
        }

        private void AssertEndpoint()
        {
            if (String.IsNullOrEmpty(ServiceEndpoint))
            {
                throw new FormatException("No Endpoint parameter found in the connection string.");
            }
        }

        /// <summary>
        /// Returns the configured connection type
        /// </summary>
        public ConnectionType Type { get; private set; }

        /// <summary>
        /// Returns the configured service endpoint
        /// </summary>
        public string ServiceEndpoint { get { return GetValueOrDefault(EndpointPropertyName); } }

        
        /// <summary>
        /// Returns the configured store name
        /// </summary>
        public string StoreName { get { return GetValueOrDefault(StoreNamePropertyName); } }

        /// <summary>
        /// Returns the configured store directory
        /// </summary>
        public string StoresDirectory { get { return GetValueOrDefault(StoresDirectoryPropertyName); } }

        /// <summary>
        /// Get the boolean flag that indicates if optimistic locking is enabled
        /// for data objects and entity framework entities accessed through this connection
        /// </summary>
        public bool OptimisticLocking { get; private set; }

        /// <summary>
        /// Get the account ID used for signing REST operations
        /// </summary>
        public string Account { get { return GetValueOrDefault(AccountPropertyName); } }

        /// <summary>
        /// Get the shared key used for signing REST operations
        /// </summary>
        public string Key { get { return GetValueOrDefault(KeyPropertyName); } }

        /// <summary>
        /// Returns the string representation of this connection string.
        /// </summary>
        /// <returns>The connection string value</returns>
        public override string ToString()
        {
            if (Type == ConnectionType.Embedded)
            {
                return String.Format("type=embedded;storesDirectory={0}", StoresDirectory);
            }
            if (Type == ConnectionType.Rest)
            {
                return String.Format("type=rest;endpoint={0};accountId={1};key={2}",ServiceEndpoint, Account, Key);
            }
            throw new NotSupportedException(String.Format("Cannot serialize connection string for connection type {0}", Type));
        }

        private string GetValueOrDefault(string propertyName)
        {
            String ret;
            return _values.TryGetValue(propertyName, out ret) ? ret : null;
        }
    }
}
