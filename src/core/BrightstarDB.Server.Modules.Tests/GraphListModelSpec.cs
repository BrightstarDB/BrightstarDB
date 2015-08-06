using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using CsQuery.EquationParser.Implementation;
using NUnit.Framework;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class GraphListModelSpec
    {
        private static readonly List<string> TestGraphs = new List<string> {"http://example.org/g1", "http://example.org/g2", "http://example.org/g3"};

        [Test]
        public void TestGraphListModelSupportsAnEmptyList()
        {
            var m = new GraphListModel(new String[]{});
            var resultString = m.AsString(SparqlResultsFormat.Xml);
            var xmlDoc = XDocument.Parse(resultString);
            Assert.That(xmlDoc.SparqlResultRows().Count(), Is.EqualTo(0));
        }

        [Test]
        public void TestGraphListModelSupportsSparqlXmlFormat()
        {
            var m = new GraphListModel(TestGraphs);
            var resultsString = m.AsString(SparqlResultsFormat.Xml);
            ValidateXmlResults(resultsString, TestGraphs);
        }

        [Test]
        public void TestGraphListModelSupportsSparqlCsvFormat()
        {
            var m = new GraphListModel(TestGraphs);
            var resultsString = m.AsString(SparqlResultsFormat.Csv);
            var lines = resultsString.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines.Count(), Is.EqualTo(TestGraphs.Count + 1));
            Assert.That(lines[0].TrimEnd(), Is.EqualTo("graphUri"));
        }

        [Test]
        public void TestGraphListModelSupportsSparqlTsvFormat()
        {
            var m = new GraphListModel(TestGraphs);
            var resultsString = m.AsString(SparqlResultsFormat.Tsv);
            var lines = resultsString.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines.Count(), Is.EqualTo(TestGraphs.Count + 1));
            Assert.That(lines[0].TrimEnd(), Is.EqualTo("?graphUri"));
        }

        [Test]
        public void TestGraphListModelSupportsSparqlJsonFormat()
        {
            var m = new GraphListModel(TestGraphs);
            var resultsString = m.AsString(SparqlResultsFormat.Json);
            // TODO: Validate result content
        }

        private void ValidateXmlResults(string resultsString, List<string> expectedGraphs)
        {
            XDocument xmlDoc = XDocument.Parse(resultsString);
            Assert.That(xmlDoc.GetVariableNames(), Contains.Item("graphUri"));
            var actualGraphs = xmlDoc.SparqlResultRows().Select(r => r.GetColumnValue("graphUri").ToString()).ToList();
            Assert.That(actualGraphs.Count, Is.EqualTo(expectedGraphs.Count));
            CollectionAssert.AreEquivalent(expectedGraphs, actualGraphs);
        }

    }
}
