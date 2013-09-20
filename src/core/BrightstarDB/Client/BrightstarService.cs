using BrightstarDB.Caching;
#if !REST_CLIENT
using BrightstarDB.Server;
#endif
using System;
#if !SILVERLIGHT && !PORTABLE
using System.Xml;
using System.ServiceModel;
#endif


namespace BrightstarDB.Client
{
    /// <summary>
    /// Provides access to different clients for interacting with Brightstar stores.
    /// </summary>
    public static class BrightstarService
    {

        private static ICache _configuredCache;
        /// <summary>
        /// This method should be called when using an embedded Brightstar clients when a program is about to terminate. This ensures
        /// all job threads are properly terminated and the application can exit. 
        /// </summary>
        /// <param name="allowJobsToConclude">If true all stores will complete any registered jobs. If false, only the current 
        /// running job is allowed to conclude and all other queued jobs are lost.</param>
        public static void Shutdown(bool allowJobsToConclude=true)
        {
#if !REST_CLIENT
            ServerCoreManager.Shutdown(allowJobsToConclude);                    
#endif
        }


#if !SILVERLIGHT && !PORTABLE

#if !REST_CLIENT
        /// <summary>
        /// Initialises and returns a new HTTP service client. This client should be used when the client is on a separate machine from the service and
        /// firewall or other constrains prohibit the use of the TcpNet client.
        /// </summary>
        /// <param name="endpointUri">The uri where the HTTP endpoint is running. By default this is http://{machinename}:8090/brightstar</param>
        /// <param name="queryCache">OPTIONAL : the cache to use for query results</param>
        /// <returns>A new brightstar service client. It is important to call dispose on the client after use.</returns>
        internal static IBrightstarService GetHttpClient(Uri endpointUri, ICache queryCache = null)
        {
            var binding = new BasicHttpContextBinding
            {
                MaxReceivedMessageSize = Int32.MaxValue,
                SendTimeout = TimeSpan.FromMinutes(30),
                TransferMode = TransferMode.StreamedResponse,
                ReaderQuotas = XmlDictionaryReaderQuotas.Max
            };
            var endpointAddress = new EndpointAddress(endpointUri);
            var client = new BrightstarServiceClient(new BrightstarWcfServiceClient(binding, endpointAddress), queryCache);
            return client;
        }

        /// <summary>
        /// Initialises and returns a new NetTcp service client. This client should be used when the client is on a separate machine from the service.
        /// </summary>
        /// <param name="endpointUri">The uri where the NetTcp endpoint is running. By default this is net.tcp://{machinename}:8095/brightstar</param>
        /// <param name="queryCache">OPTIONAL : the cache to use for query results</param>
        /// <returns>A new brightstar service client. It is important to call dispose on the client after use.</returns>
        internal static IBrightstarService GetNetTcpClient(Uri endpointUri, ICache queryCache = null)
        {
            var binding = new NetTcpContextBinding
            {
                MaxReceivedMessageSize = Int32.MaxValue,
                SendTimeout = TimeSpan.FromMinutes(30),
                TransferMode = TransferMode.StreamedResponse,
                ReaderQuotas = XmlDictionaryReaderQuotas.Max
            };
            var endpointAddress = new EndpointAddress(endpointUri);
            var client = new BrightstarServiceClient(new BrightstarWcfServiceClient(binding, endpointAddress), queryCache);
            return client;
        }

                /// <summary>
        /// Initialises and returns a new NamedPipe service client. This client should be used when the client is on the same machine as the server.
        /// </summary>
        /// <param name="endpointUri">The uri where the Named Pipe endpoint is running. By default this is net.pipe://{machinename}/brightstar</param>
        /// <param name="queryCache">OPTIONAL : the cache to use for query results</param>
        /// <returns>A new brightstar service client. It is important to call dispose on the client after use.</returns>
        internal static IBrightstarService GetNamedPipeClient(Uri endpointUri, ICache queryCache = null)
        {
            var binding = new NetNamedPipeBinding
            {
                MaxReceivedMessageSize = Int32.MaxValue,
                SendTimeout = TimeSpan.FromMinutes(30),
                TransferMode = TransferMode.StreamedResponse,
                ReaderQuotas = XmlDictionaryReaderQuotas.Max
            };

            var endpointAddress = new EndpointAddress(endpointUri);
            var client = new BrightstarServiceClient(new BrightstarWcfServiceClient(binding, endpointAddress), queryCache);
            return client;
        }
#endif
        internal  static  IBrightstarService GetRestClient(ConnectionString connectionString)
        {
            var accountId = connectionString.Account;
            var key = connectionString.Key;
            return new BrightstarRestClient(connectionString.ServiceEndpoint, accountId, key);
        }

#endif
#if !REST_CLIENT
        ///<summary>
        /// Returns a client for the embededd stores in the specified location
        ///</summary>
        ///<param name="baseLocation">The base directory for stores accessed by this client</param>
        ///<returns>A new Brightstar service client</returns>
        internal static IBrightstarService GetEmbeddedClient(string baseLocation)
        {
            return new EmbeddedBrightstarService(baseLocation);
        }
#endif
        ///<summary>
        /// Gets a client based on the connection string specified in the configuration.
        ///</summary>
        ///<returns>A new Brightstar service client</returns>
        public static IBrightstarService GetClient()
        {
            var configuration = Configuration.ConnectionString;
            if(String.IsNullOrEmpty(configuration))
            {
                throw new BrightstarClientException(Strings.BrightstarServiceClient_NoConnectionStringConfiguration);
            }
            // use connection string in config
            return GetClient(new ConnectionString(Configuration.ConnectionString));
        }

        ///<summary>
        /// Gets a client based on the connection string parameter
        ///</summary>
        ///<param name="connectionString">The connection string</param>
        ///<returns>A new Brightstar service client</returns>
        public static IBrightstarService GetClient(string connectionString)
        {
            return GetClient(new ConnectionString(connectionString));
        }

        ///<summary>
        /// Gets a client based on the connection string parameter
        ///</summary>
        ///<param name="connectionString">The connection string</param>
        ///<returns>A new Brightstar service client</returns>
        public static IBrightstarService GetClient(ConnectionString connectionString)
        {
            switch (connectionString.Type)
            {
#if !REST_CLIENT
                case ConnectionType.Embedded:
                    return new EmbeddedBrightstarService(connectionString.StoresDirectory);
#endif
#if !SILVERLIGHT && !PORTABLE
#if !REST_CLIENT
                case ConnectionType.Http:
                    return GetHttpClient(new Uri(connectionString.ServiceEndpoint), GetConfiguredCache());
                case ConnectionType.Tcp:
                    return GetNetTcpClient(new Uri(connectionString.ServiceEndpoint), GetConfiguredCache());
                case ConnectionType.NamedPipe:
                    return GetNamedPipeClient(new Uri(connectionString.ServiceEndpoint), GetConfiguredCache());
#endif
                case ConnectionType.Rest:
                    return GetRestClient(connectionString);
#endif
                default:
                    throw new BrightstarClientException("Unable to create valid context with connection string " +
                                                        connectionString.Value);
            }
        }

        /// <summary>
        /// Returns a cache object representing the cache options specified in the app.config file
        /// </summary>
        /// <returns></returns>
        private static ICache GetConfiguredCache()
        {
            return _configuredCache ?? (_configuredCache = Configuration.QueryCache);
        }

        /// <summary>
        /// Creates a data object context from a connection string defined in the application configuration
        ///</summary>
        ///<returns>The new data object context</returns>
        public static IDataObjectContext GetDataObjectContext()
        {
            return GetDataObjectContext(new ConnectionString(Configuration.ConnectionString));
        }

        ///<summary>
        /// Creates a data object context from a connection string 
        ///</summary>
        ///<param name="connectionString">The connection string that specifies how to connect to a Brightstar server</param>
        ///<returns>The new data object context</returns>
        public static IDataObjectContext GetDataObjectContext(string connectionString)
        {
            return GetDataObjectContext(new ConnectionString(connectionString));            
        }

        ///<summary>
        /// Creates a data object context from a connection string 
        ///</summary>
        ///<param name="connectionString">The <see cref="ConnectionString"/> that specifies how to connect to a Brightstar server.</param>
        ///<returns>The new data object context</returns>
        public static IDataObjectContext GetDataObjectContext(ConnectionString connectionString)
        {
            switch (connectionString.Type)
            {
#if !REST_CLIENT
                case ConnectionType.Embedded:
                    return new EmbeddedDataObjectContext(connectionString);
#if !SILVERLIGHT && !PORTABLE
                case ConnectionType.Http:
                    return new HttpDataObjectContext(connectionString);
                case ConnectionType.Tcp:
                    return new NetTcpDataObjectContext(connectionString);
                case ConnectionType.NamedPipe:
                    return new NamedPipeDataObjectContext(connectionString);
#endif
#endif
                default:
                    throw new BrightstarClientException("Unable to create valid context with connection string " +
                                                        connectionString.Value);
            }
        }

    }
}
