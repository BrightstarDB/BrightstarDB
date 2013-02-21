using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;

namespace BrightstarDB.SdShare.Client
{
    public class FeedSourceProcessor
    {
        private readonly List<ISdShareClientAdaptor> _adaptors;
        private readonly FeedSource _feedSource;        
        private readonly string _feedSourceLastUpdatedFile;
        private Boolean _processing;

        public FeedSourceProcessor(List<ISdShareClientAdaptor> adaptors, FeedSource feedSource)
        {
            _adaptors = adaptors;
            _feedSource = feedSource;
            _feedSourceLastUpdatedFile = ConfigurationReader.Configuration.LastUpdatedStorageLocation +
                                         Path.DirectorySeparatorChar + feedSource.Name;
        }

        public void Start(object state)
        {
            // dont start processing if we are still working
            if (_processing) return;
            _processing = true;
            SyncWithChangesFeed();
            _processing = false;
        }

        private DateTime GetLastUpdatedTime()
        {
            try
            {
                var fileInfo = new FileInfo(_feedSourceLastUpdatedFile);
                if (!fileInfo.Exists) return DateTime.MinValue.ToUniversalTime();

                // try and read the file
                using (var fs = new StreamReader(new FileStream(_feedSourceLastUpdatedFile, FileMode.Open)))
                {
                    var lastUpdated = fs.ReadLine();
                    if (string.IsNullOrEmpty(lastUpdated)) return DateTime.MinValue.ToUniversalTime();
                    var dt = DateTime.Parse(lastUpdated);
                    return dt;
                }
            } catch(Exception ex)
            {
                Logging.LogError(1, "Unable to read last updated time for feed {0} {1} {2}", _feedSource.Name, ex.Message, ex.StackTrace);
                return DateTime.MinValue.ToUniversalTime();
            }
        }

        private void StoreLastUpdatedTime(DateTime lastUpdated)
        {
            try
            {
                using (var fs = new StreamWriter(new FileStream(_feedSourceLastUpdatedFile, FileMode.Create)))
                {
                    fs.WriteLine(lastUpdated.ToString("s"));
                }
            } catch (Exception ex)
            {
                Logging.LogError(1, "Unable to store last updated time for feed {0} {1} {2}", _feedSource.Name, ex.Message, ex.StackTrace);                
            }
        }

        private static SyndicationFeed GetFeed(Uri feedUri)
        {
            var xmlreader = XmlReader.Create(feedUri.ToString(), new XmlReaderSettings() { CloseInput = true});            
            var feed = SyndicationFeed.Load(xmlreader);                            
            xmlreader.Close();
            return feed;
        }

        private Uri GetChangesFeedUri()
        {
            var collectionFeed = GetFeed(_feedSource.CollectionUri);

            var l =
                collectionFeed.Items.SelectMany(
                    syndicationItem =>
                    syndicationItem.Links.Where(
                        link => link.RelationshipType != null && (link.RelationshipType.Equals("http://www.egovpt.org/sdshare/fragmentsfeed") 
                                || link.RelationshipType.Equals("http://www.sdshare.org/2012/core/fragmentsfeed")))).
                    FirstOrDefault();

            if (l.Uri.IsAbsoluteUri)
            {
                return l.Uri;                
            } else
            {
                return MakeAbsoluteUri(_feedSource.CollectionUri.AbsoluteUri,  l.Uri.ToString());
            }
        }

        private static Uri MakeAbsoluteUri(string baseUri, string relativeUri)
        {
            var index = baseUri.LastIndexOf('/');
            var uri = new Uri(baseUri.Substring(0, index + 1) + relativeUri);
            return uri;
        }

        public void SyncWithChangesFeed()
        {
            try
            {
                var changesFeedUri = GetChangesFeedUri();

                Logging.LogInfo("Starting Sync with Changes Feed " + changesFeedUri);
                var since = GetLastUpdatedTime();

                // get the changes feed and add since parameter
                var changesSinceUri = changesFeedUri.ToString();
                if (changesSinceUri.Contains("?"))
                {
                    changesSinceUri += "&since=" + since.ToString("s");
                }
                else
                {
                    changesSinceUri += "?since=" + since.ToString("s");
                }

                Logging.LogInfo("Changes URL is " + changesSinceUri);
                
                // need to record the most recent updated item so that can be used in the since check 
                // next time.
                var mostRecent = since;
                
                SyndicationFeed changesFeed = null;
                do
                {
                    changesFeed = GetChangesFeed(changesSinceUri);
                    if (changesFeed == null)
                    {
                        Logging.LogWarning(0, "No changes feed at url {0}", changesSinceUri);
                        return;
                    }
                    mostRecent = ProcessFeedItems(changesFeed, mostRecent);
                    changesSinceUri = GetNextPageLink(changesFeed);
                } while (HasNextPage(changesFeed));
            
                Logging.LogInfo("Store last updated time");
                StoreLastUpdatedTime(mostRecent);
            }
            catch (Exception ex)
            {
                Logging.LogError(1, "Exception occurred syncing feed {0} {1}", ex.Message, ex.StackTrace);
            }
        }

        private static SyndicationFeed GetChangesFeed(string uri)
        {
            var changesFeed = GetFeed(new Uri(uri));
            if (changesFeed == null)
            {
                Logging.LogWarning(0, "No change feed found at URL");
                return null;
            }

            if (changesFeed.BaseUri == null)
            {
                var index = uri.LastIndexOf('/');
                changesFeed.BaseUri = new Uri(uri.Substring(0, index + 1));
                Logging.LogInfo("Setting Base Uri to " + changesFeed.BaseUri);
            }

            return changesFeed;
        }

        private static bool HasNextPage(SyndicationFeed feed)
        {
            return feed.Links.Where(l => l.RelationshipType.Equals("next") && l.MediaType.Equals("application/atom+xml")).Count() == 1;
        }

        private static string GetNextPageLink(SyndicationFeed feed)
        {
            var link =
                feed.Links.Where(l => l.RelationshipType.Equals("next") && l.MediaType.Equals("application/atom+xml")).
                    FirstOrDefault();

            if (link == null) return null;
            if (link.Uri.IsAbsoluteUri) return link.Uri.ToString();
            return feed.BaseUri + "/" + link.Uri;
        }

        private DateTime ProcessFeedItems(SyndicationFeed changesFeed, DateTime mostRecent)
        {
            foreach (var fragmentItem in changesFeed.Items)
            {                
                // store most recent
                if (fragmentItem.LastUpdatedTime.DateTime > mostRecent)
                    mostRecent = fragmentItem.LastUpdatedTime.DateTime;

                // get the link from item to the fragment
                var fragmentLink = GetFragmentLink(fragmentItem);
                Logging.LogInfo("Fragment link is " + fragmentLink);

                if (!fragmentLink.IsAbsoluteUri)
                {
                    fragmentLink = new Uri(changesFeed.BaseUri.ToString() + fragmentLink);
                }
                Logging.LogInfo("Fragment link is " + fragmentLink);

                // get the topicSI
                var resourceUri = GetResourceUri(fragmentItem);
                Logging.LogInfo("ResourceUri is " + resourceUri);
                if (resourceUri == null)
                {
                    Logging.LogInfo("No ResourceUri so skipping item");
                    continue;
                }

                // get resource data
                var resourceDescription = GetResourceXml(fragmentLink);
                Logging.LogDebug("Applying fragment {0} {1}", resourceUri, resourceDescription.ToString());
                foreach (var adaptor in _adaptors)
                {
                    adaptor.ApplyFragment(resourceUri, resourceDescription);
                }
            }

            return mostRecent;
        }

        private static string GetResourceUri(SyndicationItem item)
        {
            // backwards compatability
            ICollection<string> props = item.ElementExtensions.ReadElementExtensions<string>("TopicSI", "http://www.egovpt.org/sdshare");
            if (props.Count == 1) return props.ElementAt(0);

            // new spec 
            props = item.ElementExtensions.ReadElementExtensions<string>("ResourceUri", "http://www.sdshare.org/2012/core");
            return props.Count == 1 ? props.ElementAt(0) : null;
        }

        private static XDocument GetResourceXml(Uri uri)
        {
            try
            {
                var wc = new WebClient();
                var data = wc.DownloadString(uri.ToString());                
                return XDocument.Parse(data);
            }
            catch (Exception e)
            {
                Logging.LogError(1, "Error fetching Resource Xml for resource {0} {1} {2}", uri, e.Message, e.StackTrace);
                return null;
            }
        }

        private static Uri GetFragmentLink(SyndicationItem fragmentItem)
        {
            return (from link in fragmentItem.Links
                    where link.MediaType != null && link.MediaType.Contains("application/rdf+xml")
                    select link.Uri).FirstOrDefault();
        }
    }
}
