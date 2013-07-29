using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class GroupingTests : LinqToSparqlTestBase
    {
        [SetUp]
        public void Setup()
        {
            InitializeContext();
        }

        [Test]
        public void TestGroupCount()
        {
            var q = Context.Dinners.GroupBy(d => d.Host).Select(g => new {host = g.Key, dinnerCount = g.Count()});
            q.ToList();
            AssertQuerySparql(
                "SELECT (?v0 as ?host) (count(?d) as ?dinnerCount) WHERE { " +
                "  ?d a <http://www.networkedplanet.com/schema/test/dinner> ." +
                "  ?d <http://www.networkedplanet.com/schema/test/host> ?v0 ." +
                "} GROUP BY ?v1");
        }
    }
}
