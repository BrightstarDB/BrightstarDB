using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BrightstarDB.SDShare.Server;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Threading;

namespace BrightstarDB.SdShare.Test
{
    [TestClass]
    public class ProducerTests
    {
        [TestInitialize]
        public void StartServer()
        {
            var task = new Task(Program.StartService);
            task.Start();
        }

        private static SyndicationFeed GetFeed(Uri feedUri)
        {
            return SyndicationFeed.Load(XmlReader.Create(feedUri.ToString()));
        }

        private static string MakeAbsolute(string baseUri, Uri uri)
        {
            if (uri.IsAbsoluteUri) return uri.AbsoluteUri;
            var index = baseUri.LastIndexOf('/');
            return baseUri.Substring(0, index + 1) + uri;
        }

        [TestMethod]
        public void TestEmptyFeed()
        {
            DataManagementHelper.ClearUpdateLogs();
            var feed = GetFeed(new Uri("http://localhost:9090/sdshare/collections/SampleBusinessData/fragments"));
            Assert.AreEqual(0, feed.Items.Count());
        }

        [TestMethod]
        public void TestServiceStarted()
        {
            while (true)
            {
                Thread.Sleep(5000);
            }
        }

        [TestMethod]
        public void TestTopLevelFeedStructure()
        {
            var overviewFeed = GetFeed(new Uri("http://localhost:9090/sdshare/collections"));
            Assert.AreEqual(1, overviewFeed.Items.Count());

            var collectionFeedEntry = overviewFeed.Items.ToList()[0];

            var collectionFeedLink =
                collectionFeedEntry.Links.Where(l => l.RelationshipType != null && l.RelationshipType.Equals(ServerCore.SdShareNamespace + "collectionfeed")).
                    FirstOrDefault();

            Assert.IsNotNull(collectionFeedLink);

            var collectionFeedUri = new Uri(MakeAbsolute("http://localhost:9090/sdshare/collections", collectionFeedLink.Uri));
            var collectionFeed = GetFeed(collectionFeedUri);

            Assert.AreEqual(2, collectionFeed.Items.Count());

            var item = collectionFeed.Items.ToList()[0];
            var snapshotfeedLink =
                item.Links.Where(l => l.RelationshipType != null && l.RelationshipType.Equals(ServerCore.SdShareNamespace + "snapshotsfeed")).
                    FirstOrDefault();

            Assert.IsNotNull(snapshotfeedLink);

            item = collectionFeed.Items.ToList()[1];
            var fragmentsfeedLink =
                item.Links.Where(l => l.RelationshipType != null && l.RelationshipType.Equals(ServerCore.SdShareNamespace + "fragmentsfeed")).
                    FirstOrDefault();

            Assert.IsNotNull(fragmentsfeedLink);

            // check we get feeds from them
            var snapshotFeed = GetFeed(new Uri(MakeAbsolute(collectionFeedUri.ToString(), snapshotfeedLink.Uri)));
            var fragmentsFeed = GetFeed(new Uri(MakeAbsolute(collectionFeedUri.ToString(), fragmentsfeedLink.Uri)));

            Assert.IsNotNull(snapshotFeed);
            Assert.IsNotNull(fragmentsFeed);
        }

        [TestMethod]
        public void TestEncoding()
        {
        }

        [TestMethod]
        public void TestSinceParameter()
        {
            // insert new data and then use the since parameter    
        }

        [TestMethod]
        public void TestPaging()
        {
            
        }

        [TestMethod]
        public void TestUpdateToSourceReflectedInFeed()
        {
            
        }
        [TestMethod]
        public void TestAddToSourceReflectedInFeed()
        {

        }

        [TestMethod]
        public void TestDeleteToSourceReflectedInFeed()
        {

        }

        [TestMethod]
        public void TestUpdateToSourceReflectedInHashValueFeed()
        {            
        }

        [TestMethod]
        public void TestAddToSourceReflectedInHashValueFeed()
        {
        }

        [TestMethod]
        public void TestDeleteToSourceReflectedInHashValueFeed()
        {
        }
    }
}
