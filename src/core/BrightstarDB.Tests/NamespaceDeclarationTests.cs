using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BrightstarDB.EntityFramework;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class NamespaceDeclarationTests
    {
        private readonly Assembly _thisAssembly;
        public NamespaceDeclarationTests()
        {
            _thisAssembly = typeof(NamespaceDeclarationTests).GetTypeInfo().Assembly;
        }
        [Test]
        public void TestNamspaceDeclarationEnumeration()
        {
            // Lookup the namespace declarations in this assembly
            var nsDeclarations = NamespaceDeclarations.ForAssembly(_thisAssembly).ToList();
            Assert.That(nsDeclarations, Is.All.Not.Null);
            Assert.That(nsDeclarations.Count, Is.EqualTo(3));
        }

        [Test]
        public void TestNamespaceDeclarationsAsDictionary()
        {
            var nsDeclarations = NamespaceDeclarations.ForAssembly(_thisAssembly).AsDictionary();
            Assert.That(nsDeclarations.Count, Is.EqualTo(3));
            Assert.That(nsDeclarations.ContainsKey("dc"));
            Assert.That(nsDeclarations["dc"], Is.EqualTo("http://purl.org/dc/terms/"));
            Assert.That(nsDeclarations.ContainsKey("foaf"));
            Assert.That(nsDeclarations["foaf"], Is.EqualTo("http://xmlns.com/foaf/0.1/"));
            Assert.That(nsDeclarations.ContainsKey("bsi"));
            Assert.That(nsDeclarations["bsi"], Is.EqualTo("http://brightstardb.com/instances/"));
        }

        [Test]
        public void TestNamespaceDeclarationsAsSparql()
        {
            var sparql = NamespaceDeclarations.ForAssembly(_thisAssembly).AsSparql();
            Assert.That(sparql.Contains("PREFIX dc: <http://purl.org/dc/terms/>"));
            Assert.That(sparql.Contains("PREFIX foaf: <http://xmlns.com/foaf/0.1/>"));
            Assert.That(sparql.Contains("PREFIX bsi: <http://brightstardb.com/instances/>"));
        }

        [Test]
        public void TestNamespaceDeclarationsAsTurtle()
        {
            var turtle = NamespaceDeclarations.ForAssembly(_thisAssembly).AsTurtle();
            Assert.That(turtle.Contains("@prefix dc: <http://purl.org/dc/terms/> ."));
            Assert.That(turtle.Contains("@prefix foaf: <http://xmlns.com/foaf/0.1/> ."));
            Assert.That(turtle.Contains("@prefix bsi: <http://brightstardb.com/instances/> ."));
        }
    }
}
