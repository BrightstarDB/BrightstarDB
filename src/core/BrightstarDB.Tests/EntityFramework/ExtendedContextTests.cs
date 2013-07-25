using System;
using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class ExtendedContextTests
    {
        private string _storeName;
        public ExtendedContextTests()
        {
            _storeName = Guid.NewGuid().ToString();
            var context = GetContext();
            // Put in data
            context.Companies.Add(new Company{Name="NetworkedPlanet", TickerSymbol = "NP", HeadCount = 4, CurrentSharePrice = 1.0m});
            var ftse = context.Markets.Create();
            ftse.Name = "FTSE";
            var cac = new Company {Name = "CAC Limited", TickerSymbol = "CAC", ListedOn = ftse, HeadCount = 200, CurrentSharePrice = 1.0m};
            context.Companies.Add(cac);
            context.SaveChanges();
        }

        private MyEntityContext GetContext()
        {
            return new MyEntityContext("type=embedded;storesDirectory=c:\\brightstar;storeName=" + _storeName);            
        }

        [Test]
        public void TestSelectProperty()
        {
            var context = GetContext();
            var q = from x in context.Companies select x.Name;
            var results = q.ToList();
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains("NetworkedPlanet"));
        }

        [Test]
        public void TestCreateAnonymous()
        {
            var context = GetContext();
            var q = from x in context.Companies select new { x.Name, x.TickerSymbol };
            var results = q.ToList();
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(x=>x.Name.Equals("NetworkedPlanet")));
            Assert.IsTrue(results.Any(x=>x.TickerSymbol.Equals("NP")));            

            var p = from x in context.Companies select new { x.Name, x.TickerSymbol, Market = x.ListedOn.Name };
            var results2 = p.ToList();
            Assert.AreEqual(2, results2.Count);
            var npResult = results2.Where(x => x.TickerSymbol.Equals("NP")).First();
            var cacResult = results2.Where(x => x.TickerSymbol.Equals("CAC")).First();
            Assert.AreEqual("FTSE", cacResult.Market);
            Assert.IsNull(npResult.Market);

            var r = from x in context.Companies select new { x.Name, x.TickerSymbol, Market = x.ListedOn };
            var results3 = r.ToList();
            Assert.AreEqual(2, results3.Count);
            var npResult2 = results3.Where(x => x.TickerSymbol.Equals("NP")).First();
            var cacResult2 = results3.Where(x => x.TickerSymbol.Equals("CAC")).First();
            Assert.IsNull(npResult2.Market);
            Assert.IsNotNull(cacResult2.Market);
            Assert.AreEqual("FTSE", cacResult2.Market.Name);

        }

        [Test]
        public void TestAggregates()
        {
            var context = GetContext();
            var averageHeadcount = context.Companies.Average(x => x.HeadCount);
            Assert.AreEqual(102, averageHeadcount);

            var count = context.Companies.Count();
            Assert.AreEqual(2, count);

            var largeCompanyCount = context.Companies.Where(x => x.HeadCount > 100).Count();
            Assert.AreEqual(1, largeCompanyCount);

            var largeCompanyHeadcount = context.Companies.Where(x => x.HeadCount > 100).Average(x => x.HeadCount);
            Assert.AreEqual(200, largeCompanyHeadcount);

            var companyLongCount = context.Companies.LongCount();
            Assert.AreEqual(2, companyLongCount);

            var smallCompanyLongCount = context.Companies.Where(x => x.HeadCount < 100).LongCount();
            Assert.AreEqual(1, smallCompanyLongCount);

            var smallestCompanyHeadcount = context.Companies.Min(x => x.HeadCount);
            Assert.AreEqual(4, smallestCompanyHeadcount);

            var largestCompanyHeadcount = context.Companies.Max(x => x.HeadCount);
            Assert.AreEqual(200, largestCompanyHeadcount);
        }

        [Test]
        public void TestOrdering()
        {
            var context = GetContext();
            var orderedCompanies = context.Companies.OrderBy(x => x.HeadCount).ToList();
            Assert.AreEqual("NP", orderedCompanies[0].TickerSymbol);
            Assert.AreEqual("CAC", orderedCompanies[1].TickerSymbol);

            orderedCompanies = context.Companies.OrderByDescending(x => x.HeadCount).ToList();
            Assert.AreEqual("NP", orderedCompanies[1].TickerSymbol);
            Assert.AreEqual("CAC", orderedCompanies[0].TickerSymbol);

            orderedCompanies = context.Companies.OrderBy(x => x.CurrentSharePrice).ThenBy(x => x.HeadCount).ToList();
            Assert.AreEqual("NP", orderedCompanies[0].TickerSymbol);
            Assert.AreEqual("CAC", orderedCompanies[1].TickerSymbol);

            orderedCompanies = context.Companies.OrderBy(x => x.CurrentSharePrice).ThenByDescending(x => x.HeadCount).ToList();
            Assert.AreEqual("NP", orderedCompanies[1].TickerSymbol);
            Assert.AreEqual("CAC", orderedCompanies[0].TickerSymbol);

        }

        [Test]
        public void TestSingle()
        {
            var context = GetContext();
            var singleMarket = context.Markets.Single();
            ICompany singleCompany;
            Assert.AreEqual("FTSE", singleMarket.Name);

            try
            {
                singleCompany = context.Companies.Single();
                Assert.Fail("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            try
            {
                context.Animals.Single();
                Assert.Fail("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            singleMarket = context.Markets.SingleOrDefault();
            Assert.IsNotNull(singleMarket);
            Assert.AreEqual("FTSE", singleMarket.Name);
            try
            {
                singleCompany = context.Companies.SingleOrDefault();
                Assert.Fail("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            var animal = context.Animals.SingleOrDefault();
            Assert.IsNull(animal);

            singleCompany = context.Companies.Single(x => x.HeadCount < 100);
            Assert.IsNotNull(singleCompany);
            Assert.AreEqual("NP", singleCompany.TickerSymbol);

            singleCompany = context.Companies.SingleOrDefault(x => x.HeadCount == 1);
            Assert.IsNull(singleCompany);
        }

        [Test]
        public void TestFirst()
        {
            var context = GetContext();
            var firstCo = context.Companies.First();
            Assert.IsNotNull(firstCo);

            try
            {
                var animal = context.Animals.First();
                Assert.Fail("Expected InvalidOperationException");
            } catch(InvalidOperationException)
            {
                // Expected
            }

            firstCo = context.Companies.First(x => x.HeadCount < 100);
            Assert.IsNotNull(firstCo);
            Assert.AreEqual("NP", firstCo.TickerSymbol);

            try
            {
                var animal = context.Animals.First(x=>x.Name.Equals("bob"));
                Assert.Fail("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

        }
    }
}
