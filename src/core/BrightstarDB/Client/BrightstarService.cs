using BrightstarDB.Caching;
using BrightstarDB.Server;
using System;


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
            ServerCoreManager.Shutdown(allowJobsToConclude);                    
        }
#if !SILVERLIGHT && !PORTABLE && !__MonoCS__

        internal  static  IBrightstarService GetRestClient(ConnectionString connectionString)
        {
            var accountId = connectionString.Account;
            var key = connectionString.Key;
            return new BrightstarRestClient(connectionString.ServiceEndpoint, accountId, key);
        }

#endif

        ///<summary>
        /// Returns a client for the embededd stores in the specified location
        ///</summary>
        ///<param name="baseLocation">The base directory for stores accessed by this client</param>
        ///<returns>A new Brightstar service client</returns>
        internal static IBrightstarService GetEmbeddedClient(string baseLocation)
        {
            return new EmbeddedBrightstarService(baseLocation);
        }

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
                case ConnectionType.Embedded:
                    return new EmbeddedBrightstarService(connectionString.StoresDirectory);
#if !SILVERLIGHT && !PORTABLE && !__MonoCS__
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
                case ConnectionType.Embedded:
                    return new EmbeddedDataObjectContext(connectionString);
                default:
                    throw new BrightstarClientException("Unable to create valid context with connection string " +
                                                        connectionString.Value);
            }
        }

    }
}
