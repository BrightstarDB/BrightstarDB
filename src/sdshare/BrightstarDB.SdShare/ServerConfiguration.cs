using System.Collections.Generic;
using BrightstarDB.SdShare.Client;

namespace BrightstarDB.SdShare
{
    public class ServerConfiguration
    {
        private readonly List<ICollectionProvider> _collectionProviders;
        public IEnumerable<ICollectionProvider> CollectionProviders { get { return _collectionProviders; } }

        private readonly List<FeedSource> _feedSources;
        public IEnumerable<FeedSource> FeedSources { get { return _feedSources; } }

        private readonly List<ISdShareClientAdaptor> _clientAdaptors;
        public IEnumerable<ISdShareClientAdaptor> ClientAdaptors { get { return _clientAdaptors; } }

        public string LoggingLocation { get; set; }
        public string HashValueStorageLocation { get; set; }
        public string LastUpdatedStorageLocation { get; set; }

        public ServerConfiguration()
        {
            _collectionProviders = new List<ICollectionProvider>();
            _feedSources = new List<FeedSource>();
            _clientAdaptors = new List<ISdShareClientAdaptor>();
        }

        public void AddCollectionProvider(ICollectionProvider collectionProvider)
        {
            _collectionProviders.Add(collectionProvider);
        }

        public void AddFeedSource(FeedSource feedSource)
        {
            _feedSources.Add(feedSource);
        }

        public void AddClientAdaptor(ISdShareClientAdaptor adaptor)
        {
            _clientAdaptors.Add(adaptor);
        }
    }
}
