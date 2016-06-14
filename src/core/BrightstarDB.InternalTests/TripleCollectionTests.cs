using System;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Model;
using BrightstarDB.Rdf;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class TripleCollectionTests
    {
        private static readonly ITriple T1 = new Triple
        {
            Subject = "http://example.org/s",
            Predicate = "http://example.org/p",
            Object = "http://example.org/o",
            IsLiteral = false, 
            Graph = Constants.DefaultGraphUri
        };

        private static readonly ITriple T2 = new Triple
        {
            Subject = "http://example.org/s",
            Predicate = "http://example.org/p",
            Object = "http://example.org/o",
            IsLiteral = false,
            Graph = "http://example.org/g"
        };

        private static readonly ITriple T3 = new Triple
        {
            Subject = "http://example.org/s",
            Predicate = "http://example.org/p",
            Object = "http://example.org/o",
            IsLiteral = true,
            DataType = RdfDatatypes.String,
            Graph = Constants.DefaultGraphUri
        };

        private static readonly ITriple T4 = new Triple
        {
            Subject = "http://example.org/s1",
            Predicate = "http://example.org/p",
            Object = "http://example.org/o",
            IsLiteral = false,
            Graph = Constants.DefaultGraphUri
        };

        private static readonly ITriple T5 = new Triple
        {
            Subject = "http://example.org/s",
            Predicate = "http://example.org/p1",
            Object = "http://example.org/o",
            IsLiteral = false,
            Graph = Constants.DefaultGraphUri
        };

        private static readonly ITriple T6 = new Triple
        {
            Subject = "http://example.org/s",
            Predicate = "http://example.org/p",
            Object = "http://example.org/o1",
            IsLiteral = false,
            Graph = Constants.DefaultGraphUri
        };

        public void TestAddUpdatesCollection()
        {
            var c = new TripleCollection();
            c.Add(T1);
            Assert.AreEqual(1, c.Count());
            Assert.IsTrue(c.Items.Any(x=>x.Equals(T1)));
        }

        [Test]
        public void TestAddRaisesArgumentNullException()
        {
            var c = new TripleCollection();
            Assert.Throws<ArgumentNullException>(()=>c.Add(null));
        }

        [Test]
        public void TestAddIgnoresDuplicates()
        {
            var c = new TripleCollection();
            c.Add(T1);
            c.Add(T1);
            Assert.AreEqual(1, c.Count());
            Assert.IsTrue(c.Items.Any(x=>x.Equals(T1)));
        }

        [Test]
        public void TestAddRangeUpdatesCollection()
        {
            var c = new TripleCollection();
            c.AddRange(new []{T1, T2, T3});
            Assert.AreEqual(3, c.Count());
            Assert.IsTrue(c.Items.Any(x=>x.Equals(T1)));
            Assert.IsTrue(c.Items.Any(x => x.Equals(T2)));
            Assert.IsTrue(c.Items.Any(x => x.Equals(T3)));
        }

        [Test]
        public void TestRemoveBySubjectRemovesAllMatches()
        {
            var c = new TripleCollection();
            c.AddRange(new []{T1, T2, T3, T4});
            Assert.AreEqual(4, c.Count());
            c.RemoveBySubject("http://example.org/s");
            Assert.AreEqual(1, c.Count());
            Assert.IsTrue(c.Items.Any(x=>x.Equals(T4)));
        }

        [Test]
        public void TestRemoveBySubjectPredicateRemoveAllMatches()
        {
            var c = new TripleCollection();
            c.AddRange(new []{T1, T5});
            Assert.AreEqual(2, c.Count());
            c.RemoveBySubjectPredicate("http://example.org/s", "http://example.org/p1");
            Assert.AreEqual(1, c.Count());
            Assert.IsTrue(c.Items.Any(x=>x.Equals(T1)));
        }

        [Test]
        public void TestRemoveBySubjectPredicateObjectDoesNotRemoveLiteralMatches()
        {
            var c = new TripleCollection();
            c.AddRange(new []{T2, T3});
            c.RemoveBySubjectPredicateObject("http://example.org/s", "http://example.org/p", "http://example.org/o");
            Assert.AreEqual(1, c.Count());
            Assert.IsTrue(c.Items.Any(x=>x.Equals(T3)));
        }

        [Test]
        public void TestRemoveBySubjectPredicateLiteralDoesNotRemoveUriMatches()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T2, T3 });
            c.RemoveBySubjectPredicateLiteral("http://example.org/s", "http://example.org/p", "http://example.org/o", RdfDatatypes.String, null);
            Assert.AreEqual(1, c.Count());
            Assert.IsTrue(c.Items.Any(x => x.Equals(T2)));
        }

        [Test]
        public void TestRemoveByPredicateObjectDoesNotRemoveLiteralMatches()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T2, T3 });
            c.RemoveByPredicateObject("http://example.org/p", "http://example.org/o");
            Assert.AreEqual(1, c.Count());
            Assert.IsTrue(c.Items.Any(x => x.Equals(T3)));
        }

        [Test]
        public void TestRemoveByObjectDoesNotRemoveLiteralMatches()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T2, T3 });
            c.RemoveByObject("http://example.org/o");
            Assert.AreEqual(1, c.Count());
            Assert.IsTrue(c.Items.Any(x => x.Equals(T3)));
        }

        [Test]
        public void TestGetMatchesWithDifferentTriplePatterns()
        {
            var c = new TripleCollection();
            c.AddRange(new []{T1, T2, T3, T4, T5, T6});
            Assert.AreEqual(1, c.GetMatches(new Triple{Subject = "http://example.org/s", Predicate = "http://example.org/p", Object = "http://example.org/o", Graph = Constants.DefaultGraphUri}).Count(),
                "Expected 1 match with fully-specified pattern");
            Assert.AreEqual(2, c.GetMatches(new Triple{Subject = "http://example.org/s", Predicate = "http://example.org/p", Object = "http://example.org/o", Graph = null}).Count(),
                "Expected two matches with graph wildcard.");
            // With Triple.Match an object wildcard matches literals and non-literals alike
            Assert.AreEqual(3, c.GetMatches(new Triple { Subject = "http://example.org/s", Predicate = "http://example.org/p", Object = null, Graph = Constants.DefaultGraphUri }).Count(),
                "Expected 3 matches with object wildcard");
            Assert.AreEqual(2, c.GetMatches(new Triple { Subject = "http://example.org/s", Predicate = null, Object = "http://example.org/o", Graph = Constants.DefaultGraphUri }).Count(),
                "Expected 2 matches with predicate wildcard");
            Assert.AreEqual(2, c.GetMatches(new Triple { Subject = null, Predicate = "http://example.org/p", Object = "http://example.org/o", Graph = Constants.DefaultGraphUri }).Count(),
                "Expected 2 matches with subject wildcard");

        }

        [Test]
        public void TestGetMatchesBySubject()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T1, T2, T3, T4, T5, T6 });
            Assert.AreEqual(5, c.GetMatches("http://example.org/s").Count());
            Assert.AreEqual(1, c.GetMatches("http://example.org/s1").Count());
        }

        [Test]
        public void TestGetMatchesBySubjectPredicate()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T1, T2, T3, T4, T5, T6 });
            Assert.AreEqual(4, c.GetMatches("http://example.org/s", "http://example.org/p").Count());
            Assert.AreEqual(1, c.GetMatches("http://example.org/s1", "http://example.org/p").Count());
            Assert.AreEqual(0, c.GetMatches("http://example.org/s1", "http://example.org/p1").Count());
        }

        [Test]
        public void TestGetMatchesBySubjectPredicateObject()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T1, T2, T3, T4, T5, T6 });
            Assert.AreEqual(2, c.GetMatches("http://example.org/s", "http://example.org/p", "http://example.org/o").Count());
            Assert.AreEqual(1, c.GetMatches("http://example.org/s1", "http://example.org/p", "http://example.org/o").Count());
            Assert.AreEqual(0, c.GetMatches("http://example.org/s1", "http://example.org/p1", "http://example.org/o1").Count());
        }

        [Test]
        public void TestEnumerateSubjects()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T1, T2, T3, T4, T5, T6 });
            var subjects = c.Subjects.ToList();
            Assert.AreEqual(2, subjects.Count);
            Assert.Contains("http://example.org/s", subjects);
            Assert.Contains("http://example.org/s1", subjects);
        }

        [Test]
        public void TestEnumerateTriples()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T1, T2, T3, T4, T5, T6 });
            var triples = c.Items.ToList();
            Assert.AreEqual(6, triples.Count);
            Assert.Contains(T1, triples);
            Assert.Contains(T2, triples);
            Assert.Contains(T3, triples);
            Assert.Contains(T4, triples);
            Assert.Contains(T5, triples);
            Assert.Contains(T6, triples);
        }

        [Test]
        public void TestClearRemovesAllTriples()
        {
            var c = new TripleCollection();
            c.AddRange(new[] {T1, T2, T3, T4, T5, T6});
            var triples = c.Items.ToList();
            Assert.AreEqual(6, triples.Count);
            c.Clear();
            Assert.AreEqual(0, c.Items.Count());
            triples = c.Items.ToList();
            Assert.AreEqual(0, triples.Count);
        }

        [Test]
        public void TestContainsSubject()
        {
            var c = new TripleCollection();
            c.AddRange(new[] { T1, T2, T3, T4, T5, T6 });
            Assert.IsTrue(c.ContainsSubject("http://example.org/s"));
            Assert.IsTrue(c.ContainsSubject("http://example.org/s1"));
            Assert.IsFalse(c.ContainsSubject("http://example.org/p"));
        }
    }
}
