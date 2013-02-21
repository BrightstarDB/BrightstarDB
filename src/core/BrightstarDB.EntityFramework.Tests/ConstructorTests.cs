using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestClass]
    public class ConstructorTests : LinqToSparqlTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            base.InitializeContext();
        }

        public class StockQuote
        {
            public string Ticker { get; set; }
            public decimal Price { get; set; }
        }

        [TestMethod]
        public void TestNoArgsWithPublicProperties()
        {
            var q =
                Context.Companies.Where(c => c.ListedOn.Id.Equals("FTSE")).Select(
                    c => new StockQuote {Ticker = c.TickerSymbol, Price = c.CurrentSharePrice});
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?v1 ?v2 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
<FTSE> <http://www.networkedplanet.com/schemas/test/listing> ?c .
?c <http://www.networkedplanet.com/schemas/test/ticker> ?v1 .
?c <http://www.networkedplanet.com/schemas/test/price> ?v2 .}"
                );
            Assert.IsNotNull(Context.LastSparqlQueryContext.Constructor);
            Assert.AreEqual(typeof(StockQuote), Context.LastSparqlQueryContext.Constructor.DeclaringType);
            Assert.AreEqual(0, Context.LastSparqlQueryContext.Constructor.GetParameters().Count());
            Assert.AreEqual(0, Context.LastSparqlQueryContext.ConstructorArgs.Count);
            Assert.AreEqual(2, Context.LastSparqlQueryContext.MemberAssignment.Count);
            var tickerAssignment =
                Context.LastSparqlQueryContext.MemberAssignment.Where(ma => ma.Item1.Name.Equals("Ticker")).
                    FirstOrDefault();
            Assert.IsNotNull(tickerAssignment);
            Assert.AreEqual("v1", tickerAssignment.Item2);
            var priceAssignment =
                Context.LastSparqlQueryContext.MemberAssignment.Where(ma => ma.Item1.Name.Equals("Price")).
                    FirstOrDefault();
            Assert.IsNotNull(priceAssignment);
            Assert.AreEqual("v2", priceAssignment.Item2);
        }

    }
}
