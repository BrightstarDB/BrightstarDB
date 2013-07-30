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
                "SELECT (? v1 as ?host) (count(?v0) as ?dinnerCount) WHERE { " +
                "  ?v0 a <http://www.networkedplanet.com/schema/test/dinner> ." +
                "  ?v0 <http://www.networkedplanet.com/schema/test/host> ?v1 ." +
                "} GROUP BY ?v1");
        }
    }
}
