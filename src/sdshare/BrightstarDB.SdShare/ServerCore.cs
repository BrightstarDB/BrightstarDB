using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;

namespace BrightstarDB.SdShare
{
    public class ServerCore
    {
        public const string SdShareNamespace = "http://www.sdshare.org/2012/core/";

        public ServerCore()
        {
        }

        public Atom10FeedFormatter GetCollectionsFeed()
        {
            var serverConfiguration = ConfigurationReader.Configuration;
            IList<SyndicationItem> feedItems = new List<SyndicationItem>();
            foreach (var collectionProvider in serverConfiguration.CollectionProviders)
            {
                var item = new SyndicationItem
                {
                    Title = new TextSyndicationContent(collectionProvider.Name),
                    Summary = new TextSyndicationContent(collectionProvider.Description),
                    LastUpdatedTime = DateTime.UtcNow                    
                };

                item.Links.Add(new SyndicationLink(new Uri("collections/" + collectionProvider.Name, UriKind.Relative)) { RelationshipType =  SdShareNamespace + "collectionfeed", MediaType = "application/atom+xml" });
                item.Links.Add(new SyndicationLink(new Uri("collections/" + collectionProvider.Name, UriKind.Relative)) { RelationshipType = "alternate", MediaType = "application/atom+xml" });

                feedItems.Add(item);
            }

            var feed = new SyndicationFeed(feedItems)
            {
                Title = new TextSyndicationContent("Published Collections"),
                LastUpdatedTime = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString()            
            };            

            return new Atom10FeedFormatter(feed);
        }
 
        public Atom10FeedFormatter GetCollectionFeed(string collectionId)
        {
            var collectionProvider = ConfigurationReader.Configuration.CollectionProviders.Where(cp => cp.Name.Equals(collectionId)).FirstOrDefault();
            if (collectionProvider == null) return null;

            IList<SyndicationItem> feedItems = new List<SyndicationItem>();

            // create snapshotfeed link            
            var item = new SyndicationItem
            {
                Title = new TextSyndicationContent("Snapshot feed for " + collectionId),
                Id = Guid.NewGuid().ToString(),
                LastUpdatedTime = DateTime.UtcNow
            };

            var sl = new SyndicationLink(new Uri(collectionId + "/snapshots", UriKind.Relative))
            {
                RelationshipType = SdShareNamespace + "snapshotsfeed",
                MediaType = "application/atom+xml"
            };
            item.Links.Add(sl);
            item.Links.Add(new SyndicationLink(new Uri(collectionId + "/snapshots", UriKind.Relative)) { RelationshipType = "alternate", MediaType = "application/atom+xml" });
            feedItems.Add(item);

            // create fragments link
            item = new SyndicationItem
            {
                Title = new TextSyndicationContent("Fragments feed for " + collectionId),
                Id = Guid.NewGuid().ToString(),
                LastUpdatedTime = DateTime.UtcNow
            };

            var fl = new SyndicationLink(new Uri(collectionId + "/fragments", UriKind.Relative))
            {
                RelationshipType = SdShareNamespace + "fragmentsfeed",
                MediaType = "application/atom+xml"
            };
            item.Links.Add(fl);
            item.Links.Add(new SyndicationLink(new Uri(collectionId + "/fragments", UriKind.Relative)) { RelationshipType = "alternate", MediaType = "application/atom+xml" });
            feedItems.Add(item);

            var feed = new SyndicationFeed(feedItems)
            {
                Title = new TextSyndicationContent(collectionId + " Feed"),
                LastUpdatedTime = DateTime.UtcNow
            };

            return new Atom10FeedFormatter(feed);            
        }

        public Atom10FeedFormatter GetSnapshotsFeed(string collectionId)
        {
            var collectionProvider = ConfigurationReader.Configuration.CollectionProviders.Where(cp => cp.Name.Equals(collectionId)).FirstOrDefault();

            // todo: should not return null here.
            if (collectionProvider == null) return null;

            IList<SyndicationItem> feedItems = new List<SyndicationItem>();
            foreach (var snapshot in collectionProvider.GetSnapshots())
            {
                // create snapshot item
                var xmlSnapshotUri = new Uri("snapshots/" + snapshot.Id + "?format=xml", UriKind.Relative);
                var ntSnapshotUri = new Uri("snapshots/" + snapshot.Id + "?format=nt", UriKind.Relative);

                var item = new SyndicationItem
                {
                    Title = new TextSyndicationContent(snapshot.Name + " (snapshot)"),
                    Id = "snapshots/" + snapshot.Id,
                    LastUpdatedTime = snapshot.PublishedDate
                };

                var sl = new SyndicationLink(ntSnapshotUri)
                {
                    RelationshipType = SdShareNamespace + "snapshot",
                    MediaType = "text/plain"
                };
                item.Links.Add(sl);
                item.Links.Add(new SyndicationLink(ntSnapshotUri) { RelationshipType = "alternate", MediaType = "text/plain" });

                // links for xml
                item.Links.Add(new SyndicationLink(xmlSnapshotUri) { RelationshipType = SdShareNamespace + "snapshot", MediaType = "application/rdf+xml" });
                item.Links.Add(new SyndicationLink(xmlSnapshotUri) { RelationshipType = "alternate", MediaType = "application/rdf+xml" });

                feedItems.Add(item);                
            }

            var feed = new SyndicationFeed(feedItems)
            {
                Title = new TextSyndicationContent(collectionId + " Snapshots"),
                LastUpdatedTime = DateTime.UtcNow
            };

            return new Atom10FeedFormatter(feed);
        }

        public Atom10FeedFormatter GetFragmentsFeed(string collectionId, DateTime since, DateTime before, int page = 1)
        {
            Logging.LogInfo("GetFragments Feed {0} {1}", page, since);
            var collectionProvider = ConfigurationReader.Configuration.CollectionProviders.Where(cp => cp.Name.Equals(collectionId)).FirstOrDefault();
            if (collectionProvider == null) return null;

            const int pageSize = 1000;

            IList<SyndicationItem> feedItems = new List<SyndicationItem>();
            IEnumerable<IFragment> fragments = collectionProvider.GetFragments(since, before);
            
            var skip = (page-1)*pageSize;
            var fragmentWritten = false; 

            foreach (var fragment in fragments.Skip(skip).Take(pageSize))
            {
                fragmentWritten = true;

                // create snapshot item
                var fragmentUri = new Uri("fragment?id=" + fragment.ResourceId, UriKind.Relative);

                var item = new SyndicationItem
                {
                    Title = new TextSyndicationContent(fragment.ResourceName),
                    Id = fragmentUri.ToString(),
                    LastUpdatedTime = fragment.PublishDate
                };

                var sl = new SyndicationLink(new Uri(fragmentUri + "&format=xml", UriKind.Relative))
                {
                    RelationshipType = "http://www.sdshare.org/2012/core/fragment",
                    MediaType = "application/rdf+xml"
                };
                item.Links.Add(sl);

                sl = new SyndicationLink(new Uri(fragmentUri + "&format=xml", UriKind.Relative))
                {
                    RelationshipType = "alternate",
                    MediaType = "application/rdf+xml"
                };
                item.Links.Add(sl);

                item.ElementExtensions.Add("TopicSI", "http://www.egovpt.org/sdshare", fragment.ResourceUri);
                item.ElementExtensions.Add("ResourceUri", SdShareNamespace, fragment.ResourceUri);
                feedItems.Add(item);
            }

            var feed = new SyndicationFeed(feedItems)
            {
                Title = new TextSyndicationContent(collectionId + " Fragments"),
                LastUpdatedTime = DateTime.UtcNow
            };

            // make feed paging links
            var firstPageLink = new SyndicationLink(new Uri("fragments?page=1", UriKind.Relative))
            {
                RelationshipType = "first",
                MediaType = "application/atom+xml"
            };
            feed.Links.Add(firstPageLink);

            if (fragmentWritten)
            {
                var nextPageLink = new SyndicationLink(new Uri("fragments?page=" + (page + 1), UriKind.Relative))
                {
                    RelationshipType = "next",
                    MediaType = "application/atom+xml"
                };
                feed.Links.Add(nextPageLink);
            }

            if (page > 1)
            {
                var prevPageLink = new SyndicationLink(new Uri("fragments?page=" + (page - 1), UriKind.Relative))
                {
                    RelationshipType = "prev",
                    MediaType = "application/atom+xml"
                };
                feed.Links.Add(prevPageLink);
            }

            return new Atom10FeedFormatter(feed);
        }

        public Stream GetSnapshot(string collectionId, string snapshotId, string format)
        {
            var collectionProvider = ConfigurationReader.Configuration.CollectionProviders.Where(cp => cp.Name.Equals(collectionId)).FirstOrDefault();
            if (collectionProvider == null) return null;
            return collectionProvider.GetSnapshot(snapshotId, format);
        }

        public Stream GetFragment(string collectionId, string resourceId, string format)
        {
            Logging.LogInfo("GetFragment {0} {1} {2}", collectionId, resourceId, format);
            var collectionProvider = ConfigurationReader.Configuration.CollectionProviders.Where(cp => cp.Name.Equals(collectionId)).FirstOrDefault();
            if (collectionProvider == null) return null;
            return collectionProvider.GetFragment(resourceId, format);
        }
    }
}
