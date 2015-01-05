using System.Collections.Generic;
using BrightstarDB.Caching;
using BrightstarDB.Config;
using BrightstarDB.Storage;

namespace BrightstarDB.Server
{
    internal class ServerCoreManager
    {
        private static readonly object UpdateLock;
        private static readonly Dictionary<string, ServerCore> ServerCores;
        private static readonly ICache QueryCache;
        private static readonly PersistenceType PersistenceType;

        static ServerCoreManager()
        {
            UpdateLock = new object();
            ServerCores = new Dictionary<string, ServerCore>();
            QueryCache = Configuration.QueryCache;
            PersistenceType = Configuration.PersistenceType;
        }

        public static void Shutdown(bool completeJobs=true)
        {
            lock (UpdateLock)
            {
                foreach (var serverCore in ServerCores.Values)
                {
                    serverCore.Shutdown(completeJobs);
                }
                ServerCores.Clear();
            }
        }

        public static ServerCore GetServerCore(string baseLocation, EmbeddedServiceConfiguration serviceConfiguration)
        {
            lock (UpdateLock)
            {
                if (!ServerCores.ContainsKey(baseLocation))
                {
                    var serverCore = new ServerCore(baseLocation, QueryCache, PersistenceType, serviceConfiguration.EnableTransactionLoggingOnNewStores);
                    if (serviceConfiguration != null && serviceConfiguration.PreloadConfiguration != null)
                    {
                        serverCore.Warmup(serviceConfiguration.PreloadConfiguration);
                    }
                    ServerCores.Add(baseLocation, serverCore);
                }
                return ServerCores[baseLocation];
            }
        }
    }
}
