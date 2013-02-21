using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrightstarDB.SdShare.Client
{
    /// <summary>
    /// This is the start, this is the service component
    /// </summary>
    public class SdShareClientFeedProcessor
    {
        private readonly List<FeedSource> _feedSources;
        private readonly List<ISdShareClientAdaptor> _adaptors;
        private readonly List<Timer> _timers;

        public SdShareClientFeedProcessor(List<FeedSource> feedSources, List<ISdShareClientAdaptor> adaptors)
        {
            _feedSources = feedSources;
            _adaptors = adaptors;
            _timers = new List<Timer>();
        }

        public void Start()
        {
            // start a thread for each feed source
            foreach (var feedSource in _feedSources)
            {
                var source = feedSource;
                var adaptors = _adaptors.Where(a => a.FeedName.Equals(source.Name)).ToList();
                var feedSourceProcessor = new FeedSourceProcessor(adaptors, feedSource);
                var timer = new Timer(feedSourceProcessor.Start, null, 0, feedSource.CheckPeriod);
                _timers.Add(timer);
            }
        }
    }
}
