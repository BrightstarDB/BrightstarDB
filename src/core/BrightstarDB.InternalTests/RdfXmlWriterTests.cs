using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using BrightstarDB.Rdf;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class RdfXmlWriterTests
    {
        private static readonly XNamespace RdfNS = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        [Test]
        public void TestWriteEmptyFile()
        {
            StringWriter sw;
            using (var writer = GetStringWriter(out sw))
            {

            }
            XDocument doc = XDocument.Parse(sw.ToString());
            Assert.AreEqual(doc.Root.Name.LocalName, "RDF");
            Assert.AreEqual(doc.Root.Name.Namespace, RdfNS);
        }


        private RdfXmlWriter GetStringWriter(out StringWriter sw)
        {
            sw = new StringWriter();
            return new RdfXmlWriter(new XmlTextWriter(sw));
        }

        [Test]
        public void TestSimpleTriple()
        {
            StringWriter sw;
            using (var writer = GetStringWriter(out sw))
            {
                writer.Triple("http://example.org/s", false,
                    "http://example.org/p", false, 
                    "http://example.org/o", false, false, null, null, null);
            }
            XNamespace expectedNamespace = "http://example.org/";
            XDocument doc = XDocument.Parse(sw.ToString());
            var desc = doc.Descendants(RdfNS + "Description").FirstOrDefault();
            Assert.That(desc, Is.Not.Null);
            var about = desc.Attribute(RdfNS + "about");
            Assert.That(about, Is.Not.Null);
            Assert.That(about.Value, Is.EqualTo("http://example.org/s"));
            var prop = desc.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("p"));
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop.Name.Namespace, Is.EqualTo(expectedNamespace));
            var res = prop.Attribute(RdfNS + "resource");
            Assert.That(res, Is.Not.Null);
            Assert.That(res.Value, Is.EqualTo("http://example.org/o"));
        }

        [Test]
        public void TestBNodeReference()
        {
            StringWriter sw;
            using (var writer = GetStringWriter(out sw))
            {
                writer.Triple("http://example.org/s", false, 
                    "http://example.org/p", false,
                    "o", true, false, null, null, null);
                writer.Triple("o", true,
                    "http://example.org/p2", false,
                    "foo", false, true, null, null, null);
            }

            XNamespace expectedNamespace = "http://example.org/";
            XDocument doc = XDocument.Parse(sw.ToString());
            var s =
                doc.Descendants(RdfNS + "Description").Where(x => x.Attribute(RdfNS + "about") != null).ToList();
            Assert.That(s.Count, Is.EqualTo(1));
            var p = s[0].Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("p"));
            Assert.That(p, Is.Not.Null);
            var nodeRef = p.Attribute(RdfNS + "nodeID");
            Assert.That(nodeRef, Is.Not.Null);
            Assert.That(nodeRef.Value, Is.EqualTo("o"));

            var bnodes =
                doc.Descendants(RdfNS + "Description").Where(x => x.Attribute(RdfNS + "nodeID") != null).ToList();
            Assert.That(bnodes.Count, Is.EqualTo(1));
            Assert.That(bnodes[0].Attribute(RdfNS + "nodeID").Value, Is.EqualTo("o"));
            var p2 = bnodes[0].Attribute(expectedNamespace + "p2");
            Assert.That(p2, Is.Not.Null);
            Assert.That(p2.Value, Is.EqualTo("foo"));
        }

        [Test]
        public void TestSimpleLiteral()
        {
            StringWriter sw;
            using (var writer = GetStringWriter(out sw))
            {
                writer.Triple("http://example.org/s", false,
                    "http://example.org/p", false,
                    "foo", false, true, null, null, null);
            }

            XNamespace expectedNamespace = "http://example.org/";
            XDocument doc = XDocument.Parse(sw.ToString());
            var p = doc.Descendants(RdfNS + "Description")
               .FirstOrDefault(x => x.Attribute(expectedNamespace + "p") != null);
            Assert.That(p, Is.Not.Null);
            Assert.That(p.Attribute(expectedNamespace + "p").Value, Is.EqualTo("foo"));
        }

        [Test]
        public void TestLiteralWithLanguage()
        {
            StringWriter sw;
            using (var writer = GetStringWriter(out sw))
            {
                writer.Triple("http://example.org/s", false,
                    "http://example.org/p", false,
                    "foo", false, true, null, "en", null);
            }
            XNamespace xmlNamespace = "http://www.w3.org/XML/1998/namespace";
            XNamespace expectedNamespace = "http://example.org/";
            XDocument doc = XDocument.Parse(sw.ToString());
            var p = doc.Descendants(RdfNS + "Description")
               .FirstOrDefault(x => x.Attribute(expectedNamespace + "p") != null);
            Assert.That(p, Is.Not.Null);
            Assert.That(p.Attribute(expectedNamespace + "p").Value, Is.EqualTo("foo"));
            var lang = p.Attribute(xmlNamespace + "lang");
            Assert.That(lang, Is.Not.Null);
            Assert.That(lang.Value, Is.EqualTo("en"));
        }

        [Test]
        public void TestLiteralWithDatatype()
        {
            StringWriter sw;
            using (var writer = GetStringWriter(out sw))
            {
                writer.Triple("http://example.org/s", false,
                              "http://example.org/p", false,
                              "123", false, true, "http://www.w3.org/2001/XMLSchema#int", null, null);
            }
            XNamespace expectedNamespace = "http://example.org/";
            XDocument doc = XDocument.Parse(sw.ToString());
            Console.WriteLine(sw.ToString());
            var p = doc.Descendants(RdfNS + "Description")
                       .Descendants()
                       .FirstOrDefault(x => x.Name.LocalName.Equals("p") && x.Name.Namespace.Equals(expectedNamespace));
            Assert.That(p, Is.Not.Null);
            var datatype = p.Attribute(RdfNS + "datatype");
            Assert.That(datatype, Is.Not.Null);
            Assert.That(datatype.Value, Is.EqualTo("http://www.w3.org/2001/XMLSchema#int"));
            Assert.That(p.Value, Is.EqualTo("123"));
        }

        [Test]
        public void TestLiteralWithDatatypeAndLanguage()
        {
            StringWriter sw;
            using (var writer = GetStringWriter(out sw))
            {
                writer.Triple("http://example.org/s", false,
                              "http://example.org/p", false,
                              "123", false, true, "http://www.w3.org/2001/XMLSchema#int", "en", null);
            }
            XNamespace xmlNamespace = "http://www.w3.org/XML/1998/namespace";
            XNamespace expectedNamespace = "http://example.org/";
            XDocument doc = XDocument.Parse(sw.ToString());
            Console.WriteLine(sw.ToString());
            var p = doc.Descendants(RdfNS + "Description")
                       .Descendants()
                       .FirstOrDefault(x => x.Name.LocalName.Equals("p") && x.Name.Namespace.Equals(expectedNamespace));
            Assert.That(p, Is.Not.Null);
            var datatype = p.Attribute(RdfNS + "datatype");
            Assert.That(datatype, Is.Not.Null);
            Assert.That(datatype.Value, Is.EqualTo("http://www.w3.org/2001/XMLSchema#int"));
            Assert.That(p.Value, Is.EqualTo("123"));
            var lang = p.Attribute(xmlNamespace + "lang");
            Assert.That(lang, Is.Not.Null);
            Assert.That(lang.Value, Is.EqualTo("en"));
        }

        [Test]
        public void TestMultipleTriples()
        {
            StringWriter sw;
            using (var writer = GetStringWriter(out sw))
            {
                writer.Triple("http://example.org/s", false,
                    "http://example.org/p", false,
                    "http://example.org/o", false, false, null, null, null);
                writer.Triple("http://example.org/s", false,
                    "http://example.org/p2", false,
                    "123", false, true, "http://www.w3.org/2001/XMLSchema#int", "en", null);
                writer.Triple("http://example.org/s", false,
                              "http://example.org/p2", false,
                              "foo", false, true, null, null, null);

            }
            XDocument doc = XDocument.Parse(sw.ToString());
            Console.WriteLine(sw.ToString());
            var descriptions = doc.Root.Elements(RdfNS + "Description");
            Assert.That(descriptions.Count(), Is.EqualTo(3));
        }
    }
}
