using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Linq;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkedPlanet.Brightstar.OData.Tests.ClientLibraryTests;

namespace NetworkedPlanet.Brightstar.OData.Tests.Tests
{
    [TestClass]
    public class ODataClientLibraryTests : ODataTestBase
    {
        private static List<string> _entitySets;
        private static List<string> _associationSets;


        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            DropAndRecreateStore();
            CreateData();
            if (!Directory.Exists(@".\out"))
            {
                Directory.CreateDirectory(@".\out");
            }

            _entitySets = new List<string>
                              {
                                  "Article",
                                  "Company",
                                  "DataTypeTestEntity",
                                  "Department",
                                  "JobRole",
                                  "Person",
                                  "Skill"
                              };
            _associationSets = new List<string> {"Publisher", "Company", "Department", "JobRole", "Skills", "Children"};
        }

        [ClassCleanup]
        public static void TearDown()
        {
        }

        [TestInitialize]
        public void TestSetUp()
        {
            StartService(new Uri("http://localhost:8090/odata"));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            StopService();
        }

     

        [TestMethod]
        public void TestGetNetflixData()
        {
            HTTPResponse samples = new HTTPResponse();
            //FileResponseSamples fileResponseSamples = new FileResponseSamples();
            string atomFormat = "application/atom+xml";
            string jsonFormat = "application/json";
            string localBaseUri = "http://localhost:8090/odata";
            string netflixBaseUri = "http://odata.netflix.com/v2/Catalog/";
            string stackoverflowBaseUri = "http://data.stackexchange.com/stackoverflow/atom/";
            ODataVersion baseVersion = ODataVersion.V2;
            ODataVersion maxVersion = ODataVersion.V3;

            var model = samples.GetMetadata(netflixBaseUri + "$metadata");
            samples.ExecuteNetflixRequest(model, "NetflixGenres");

          
        }

        static string _atomFormat = "application/atom+xml";
        static string _jsonFormat = "application/json";
        static string _localBaseUri = "http://localhost:8090/odata/";
        static ODataVersion _baseVersion = ODataVersion.V2;
        static ODataVersion _maxVersion = ODataVersion.V3;

        [TestMethod]
        public void TestModel()
        {
            var samples = new HTTPResponse();
            var model = samples.GetMetadata(_localBaseUri + "$metadata");
            Assert.IsNotNull(model);

            var entitySets =
                model.EntityContainers.First().Elements.Where(
                    e => e.ContainerElementKind.Equals(EdmContainerElementKind.EntitySet)).ToList();
            Assert.IsNotNull(entitySets);
            Assert.AreEqual(_entitySets.Count, entitySets.Count);
            foreach(var es in entitySets)
            {
                Assert.IsTrue(_entitySets.Contains(es.Name));
            }
            

            var associationSets =
                model.EntityContainers.First().Elements.Where(
                    e => e.ContainerElementKind.Equals(EdmContainerElementKind.AssociationSet)).ToList();
            Assert.IsNotNull(associationSets);
            Assert.AreEqual(_associationSets.Count, associationSets.Count);
            foreach(var assoc in associationSets)
            {
                Assert.IsTrue(_associationSets.Contains(assoc.Name));
            }

            var functionImports =
                model.EntityContainers.First().Elements.Where(
                    e => e.ContainerElementKind.Equals(EdmContainerElementKind.FunctionImport)).ToList();
            Assert.IsNotNull(functionImports);
            Assert.AreEqual(0, functionImports.Count);
        }

        [TestMethod]
        public void TestBaseballStats()
        {
            var baseBallStatsUri = "http://baseball-stats.info/OData/baseballstats.svc/";
            var samples = new HTTPResponse();
            var model = samples.GetMetadata(baseBallStatsUri + "$metadata");
            samples.ExecuteBaseballStatsRequest(model, "BaseballStatsRequest");
        }

        [TestMethod]
        public void TestGetArticles()
        {
            var samples = new HTTPResponse();
            var model = samples.GetMetadata(_localBaseUri + "$metadata");
            var mreader = samples.GetResponse(_localBaseUri + "Article", _atomFormat, _baseVersion, _maxVersion, model);

            
        }

        [TestMethod]
        public void TestGetPeople()
        {
            var samples = new HTTPResponse();
            var model = samples.GetMetadata(_localBaseUri + "$metadata");
            var mreader = samples.GetResponse(_localBaseUri + "Person", _atomFormat, _baseVersion, _maxVersion, model);

            CheckResponse(mreader);
        }

        void CheckResponse(ODataMessageReader mreader)
        {
            using (mreader)
            {
                var reader = mreader.CreateODataFeedReader();
                while (reader.Read())
                {
                    Assert.IsNotNull(reader.State);
                    switch (reader.State)
                    {
                        case ODataReaderState.FeedStart:
                            {
                                ODataFeed feed = (ODataFeed)reader.Item;

                            }

                            break;
                        case ODataReaderState.FeedEnd:
                            {
                                ODataFeed feed = (ODataFeed)reader.Item;
                                if (feed.Count != null)
                                {
                                    //this.writer.WriteLine("Count: " + feed.Count.ToString());
                                }
                                if (feed.NextPageLink != null)
                                {
                                    //this.writer.WriteLine("NextPageLink: " + feed.NextPageLink.AbsoluteUri);
                                }

                            }

                            break;

                        case ODataReaderState.EntryStart:
                            {
                                ODataEntry entry = (ODataEntry)reader.Item;

                            }

                            break;

                        case ODataReaderState.EntryEnd:
                            {
                                ODataEntry entry = (ODataEntry)reader.Item;
                                // this.writer.WriteLine("TypeName: " + (entry.TypeName ?? "<null>"));
                                // this.writer.WriteLine("Id: " + (entry.Id ?? "<null>"));
                                if (entry.ReadLink != null)
                                {
                                    // this.writer.WriteLine("ReadLink: " + entry.ReadLink.AbsoluteUri);
                                }

                                if (entry.EditLink != null)
                                {
                                    //this.writer.WriteLine("EditLink: " + entry.EditLink.AbsoluteUri);
                                }

                                if (entry.MediaResource != null)
                                {
                                    //     this.writer.Write("MediaResource: ");
                                    //      this.WriteValue(entry.MediaResource);
                                }

                                //  this.WriteProperties(entry.Properties);
                                foreach (ODataProperty property in entry.Properties)
                                {
                                    //   this.writer.Write(property.Name + ": ");
                                    //  this.WriteValue(property.Value);
                                }

                                foreach (var assoc in entry.AssociationLinks)
                                {
                                    //do something
                                }

                                //  this.writer.Indent--;
                            }

                            break;

                        case ODataReaderState.NavigationLinkStart:
                            {
                                ODataNavigationLink navigationLink = (ODataNavigationLink)reader.Item;
                                //  this.writer.WriteLine(navigationLink.Name + ": ODataNavigationLink: ");
                                //  this.writer.Indent++;
                            }

                            break;

                        case ODataReaderState.NavigationLinkEnd:
                            {
                                ODataNavigationLink navigationLink = (ODataNavigationLink)reader.Item;
                                // this.writer.WriteLine("Url: " + (navigationLink.Url == null ? "<null>" : navigationLink.Url.AbsoluteUri));
                                // this.writer.Indent--;
                            }

                            break;
                    }
                }
            }
        }

      


    }
}
