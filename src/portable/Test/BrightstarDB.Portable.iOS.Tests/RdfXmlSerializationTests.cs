using System;
using System.Xml.Linq;
using NUnit.Framework;
using VDS.RDF;
using VDS.RDF.Writing;
using StringWriter = System.IO.StringWriter;

namespace BrightstarDB.Portable.iOS.Tests
{
    [TestFixture]
    public class RdfXmlSerializationTests
    {
        [Test]
        public void TestWriteSimpleRdf()
        {
            var g = new Graph();
            g.Assert(g.CreateUriNode(new Uri("http://example.org/s")),
                g.CreateUriNode(new Uri("http://example.org/p")),
                g.CreateLiteralNode("o"));
            //g.Assert(g.CreateUriNode(new Uri("http://example.org/s")),
            //    g.CreateUriNode(new Uri("http://example.org/ns2/p")),
            //    g.CreateLiteralNode("Another o"));
            using (var stringWriter = new StringWriter())
            {
                var writer = new RdfXmlWriter();
                writer.Save(g, stringWriter);
                var buff = stringWriter.ToString();
                XDocument doc = XDocument.Parse(buff); // Fails
            }
        }
    }
}
