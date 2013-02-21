using System;
using System.IO;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using System.Diagnostics;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class NTriplesParserTests
    {
        private readonly IStoreManager _storeManager = StoreManagerFactory.GetStoreManager();

        [TestMethod]
        public void TestBrightstarParser()
        {
            var t = new Stopwatch();
            t.Start();
            using (var fs = new FileStream("BSBM_370k.nt", FileMode.Open))
            {
                var parser = new NTriplesParser();
                parser.Parse(fs, new NoopParser(), Constants.DefaultGraphUri);                
            }
            t.Stop();
            Console.WriteLine("Time for Brightstar Parser is " + t.ElapsedMilliseconds);
        }

        [TestMethod]
        [Ignore]
        public void TestVdsParser()
        {
            var t = new Stopwatch();
            t.Start();
            using (var fs = new FileStream("BSBM_370k.nt", FileMode.Open))
            {
                var parser = new VDS.RDF.Parsing.NTriplesParser();
                parser.Load(new NoopParser(), new StreamReader(fs));                
            }
            t.Stop();
            Console.WriteLine("Time for Brightstar Parser is " + t.ElapsedMilliseconds);
        }

        [TestMethod]
        public void TestEncodingAndEscaping()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            store.Import(Guid.Empty, new FileStream("escaping.nt", FileMode.Open));
            store.Export(new FileStream(@"escaping-out.nt", FileMode.Create));
        }


        [TestMethod]
        public void TestBasicNtriples()
        {
            var ntp = new NTriplesParser();
            using (var fs = new FileStream("simple.txt", FileMode.Open))
            {
                ntp.Parse(fs, new NoopParser(), Constants.DefaultGraphUri);
            }
        }

        [TestMethod]
        public void TestBasicNQuads()
        {
            var ntp = new NTriplesParser();
            ntp.Parse(new FileStream("nquads.txt", FileMode.Open), new NoopParser(), Constants.DefaultGraphUri);
        }

        public class NoopParser : BaseRdfHandler , ITripleSink
        {
            protected override bool HandleTripleInternal(Triple t)
            {
                return true;
            }

            public override bool AcceptsAll
            {
                get { return true; }
            }

            public void Triple(string subject, bool subjectIsBNode,
                string predicate, bool predicateIsBNode,
                string obj, bool objectIsBNode,
                bool isLiteral, string dataType, string langCode, string graphUri)
            {
                // no op
            }

            public void BNode(string bnodeId, string bnodeUri)
            {
                // no op
            }
        }

    }
}
