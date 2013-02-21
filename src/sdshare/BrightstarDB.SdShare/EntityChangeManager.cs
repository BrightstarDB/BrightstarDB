using System.Collections.Generic;
using System.Threading;

namespace BrightstarDB.SdShare
{
    /// <summary>
    /// Class that schedules checks against data that has no last updated date
    /// </summary>
    public class EntityChangeManager
    {
        /// <summary>
        /// Maintains a list of references to started jobs.
        /// </summary>
        private readonly List<Timer> _timers = new List<Timer>();

        private static EntityChangeManager _instance;

        public static EntityChangeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EntityChangeManager();
                }
                return _instance;
            }
        }

        public void AddJob(BaseDataSourceManager manager, TimerCallback callback, int dueTime, int period)
        {
            Logging.LogInfo("Adding DataSourceManager");
            var timer = new Timer(callback, null, 0, period);
            _timers.Add(timer);
        }

        public void Start()
        {
            foreach (var collectionProvider in ConfigurationReader.Configuration.CollectionProviders)
            {
                if (collectionProvider is OdbcCollectionProvider)
                {
                    var odbcCollectionProvider = collectionProvider as OdbcCollectionProvider;
                    foreach (var resourcePublishingDefinition in odbcCollectionProvider.PublishingDefinitions)
                    {
                        if (resourcePublishingDefinition.NoTimeStampInData)
                        {
                            // create a repeating task to check and update the hash value for this data set.
                            var dsm = new DataSourceManager(odbcCollectionProvider, resourcePublishingDefinition);
                            resourcePublishingDefinition.DataSourceManager = dsm;
                            Logging.LogInfo("Starting DataSourceManager for " + resourcePublishingDefinition.HashValueTable);
                            var timer = new Timer(dsm.ProcessDataSource, null, 0, int.Parse(resourcePublishingDefinition.ValueCheckInterval));
                            _timers.Add(timer);
                        }
                    }
                }
            } 
        }
    }
}
