using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using NUnit.Framework;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using System.Diagnostics;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class NTriplesParserTests
    {
        private readonly IStoreManager _storeManager = StoreManagerFactory.GetStoreManager();

        [Test]
        public void TestBrightstarParser()
        {
            var t = new Stopwatch();
            t.Start();
            using (var fs = new FileStream(TestPaths.DataPath + "BSBM_370k.nt", FileMode.Open))
            {
                var parser = new NTriplesParser();
                parser.Parse(fs, new NoopParser(), Constants.DefaultGraphUri);                
            }
            t.Stop();
            Console.WriteLine("Time for Brightstar Parser is " + t.ElapsedMilliseconds);
        }

        [Test]
        [Ignore]
        public void TestVdsParser()
        {
            var t = new Stopwatch();
            t.Start();
            using (var fs = new FileStream(TestPaths.DataPath + "BSBM_370k.nt", FileMode.Open))
            {
                var parser = new VDS.RDF.Parsing.NTriplesParser();
                parser.Load(new NoopParser(), new StreamReader(fs));                
            }
            t.Stop();
            Console.WriteLine("Time for Brightstar Parser is " + t.ElapsedMilliseconds);
        }

        [Test]
        public void TestEncodingAndEscaping()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            store.Import(Guid.Empty, new FileStream(TestPaths.DataPath + "escaping.nt", FileMode.Open));
            store.Export(new FileStream(@"escaping-out.nt", FileMode.Create));
        }


        [Test]
        public void TestBasicNtriples()
        {
            var ntp = new NTriplesParser();
            using (var fs = new FileStream(TestPaths.DataPath+"simple.txt", FileMode.Open))
            {
                ntp.Parse(fs, new NoopParser(), Constants.DefaultGraphUri);
            }
        }

        [Test]
        public void TestBasicNQuads()
        {
            var ntp = new NTriplesParser();
            ntp.Parse(new FileStream(TestPaths.DataPath+"nquads.txt", FileMode.Open), new NoopParser(), Constants.DefaultGraphUri);
        }

        [Test]
        public void TestBackslashEscape()
        {
            const string ntriples = @"<http://example.org/s> <http://example.org/p1> ""c:\\users""
<http://example.org/s> <http://example.org/p2> ""\\users\\tom""";
            var parser = new NTriplesParser();
            var sink = new LoggingTripleSink();
            parser.Parse(new StringReader(ntriples), sink, "http://example.org/g" );

            Assert.That(sink.Triples, Has.Count.EqualTo(1));
            var triple1 = sink.Triples.FirstOrDefault(t => t.Predicate.Equals("http://example.org/p1"));
            var triple2 = sink.Triples.FirstOrDefault(t => t.Predicate.Equals("http://example.org/p2"));
            Assert.That(triple1, Is.Not.Null);
            Assert.That(triple1.IsLiteral);
            Assert.That(triple1.Object, Is.EqualTo(@"c:\users"));
            Assert.That(triple2, Is.Not.Null);
            Assert.That(triple2.IsLiteral);
            Assert.That(triple2.Object, Is.EqualTo(@"\users\tom"));
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

        internal class LoggingTripleSink : ITripleSink
        {
            public List<BrightstarDB.Model.Triple> Triples { get; set; }

            public LoggingTripleSink()
            {
                Triples = new List<BrightstarDB.Model.Triple>();
            }

            public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode,
                               bool objIsLiteral, string dataType, string langCode, string graphUri)
            {
                Triples.Add(new BrightstarDB.Model.Triple
                    {
                        Subject = subject,
                        Predicate = predicate,
                        Object = obj,
                        DataType = dataType,
                        Graph = graphUri,
                        IsLiteral = objIsLiteral,
                        LangCode = langCode
                    });
            }


        }
    }
}
