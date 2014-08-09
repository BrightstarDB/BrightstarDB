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
        public void TestBrightstarParserStillFaster()
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

            var t2 = new Stopwatch();
            t2.Start();
            using (var fs = new FileStream(TestPaths.DataPath + "BSBM_370k.nt", FileMode.Open))
            {
                var parser = new VDS.RDF.Parsing.NTriplesParser();
                parser.Load(new NoopParser(), new StreamReader(fs));
            }
            t2.Stop();
            Console.WriteLine("Time for dotNetRDF Parser is " + t2.ElapsedMilliseconds);

            Assert.That(t.ElapsedMilliseconds, Is.LessThan(t2.ElapsedMilliseconds));
        }

        [Test]
        public void TestEncodingAndEscaping()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid);
            using (
                var stream = new FileStream(TestPaths.DataPath + "escaping.nt", FileMode.Open, FileAccess.Read,
                                            FileShare.ReadWrite))
            {
                store.Import(Guid.Empty, stream);
            }
            store.Export(new FileStream(@"escaping-out.nt", FileMode.Create));
        }

        [Test]
        public void TestImportEscaping()
        {
            var parser = new NTriplesParser();
            var sink = new LoggingTripleSink();
            using (
                var stream = new FileStream(TestPaths.DataPath + "escaping.nt", FileMode.Open, FileAccess.Read,
                                            FileShare.ReadWrite))
            {
                parser.Parse(stream, sink, "http://example.org/g");
            }

            Assert.That(sink.Triples, Has.Count.EqualTo(8));
            Assert.That(sink.Triples, Has.Some.Property("Object").EqualTo("simple literal"));
            Assert.That(sink.Triples, Has.Some.Property("Object").EqualTo("backslash:\\"));
            Assert.That(sink.Triples, Has.Some.Property("Object").EqualTo("dquote:\""));
            Assert.That(sink.Triples, Has.Some.Property("Object").EqualTo("newline:\n"));
            Assert.That(sink.Triples, Has.Some.Property("Object").EqualTo("tab:\t"));
            Assert.That(sink.Triples, Has.Some.Property("Object").EqualTo("\u00E9"));
            Assert.That(sink.Triples, Has.Some.Property("Object").EqualTo("\u20AC"));
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

            Assert.That(sink.Triples, Has.Count.EqualTo(2));
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

            public void Close()
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
            public List<Model.Triple> Triples { get; set; }

            public LoggingTripleSink()
            {
                Triples = new List<Model.Triple>();
            }

            public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode,
                               bool objIsLiteral, string dataType, string langCode, string graphUri)
            {
                Triples.Add(new Model.Triple
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

            public void Close()
            {
                // No-op
            }
        }
    }
}
