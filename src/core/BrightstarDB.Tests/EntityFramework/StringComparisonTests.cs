using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestClass]
    public class StringComparisonTests
    {
        private static MyEntityContext _context;

        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            _context = new MyEntityContext("type=embedded;storesDirectory=" + Configuration.StoreLocation + ";storeName=EFStringComparisonTests_" + DateTime.Now.Ticks);
            var np = new Company {Name = "NetworkedPlanet"};
            var apple = new Company {Name = "Apple"};
            _context.Companies.Add(np);
            _context.Companies.Add(apple);
            _context.SaveChanges();
        }

        [TestMethod]
        public void TestStartsWith()
        {
            var results = _context.Companies.Where(c => c.Name.StartsWith("Net")).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet",results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", true, CultureInfo.CurrentCulture)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", false, CultureInfo.CurrentCulture)).ToList();
            Assert.AreEqual(0, results.Count);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.CurrentCultureIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.CurrentCulture)).ToList();
            Assert.AreEqual(0, results.Count);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.InvariantCulture)).ToList();
            Assert.AreEqual(0, results.Count);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.Ordinal)).ToList();
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void TestEndsWith()
        {
            var results = _context.Companies.Where(c => c.Name.EndsWith("net")).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net")).ToList();
            Assert.AreEqual(0, results.Count);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", true, CultureInfo.CurrentCulture)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", false, CultureInfo.CurrentCulture)).ToList();
            Assert.AreEqual(0, results.Count);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.CurrentCultureIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.CurrentCulture)).ToList();
            Assert.AreEqual(0, results.Count);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.InvariantCulture)).ToList();
            Assert.AreEqual(0, results.Count);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.Ordinal)).ToList();
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void TestContains()
        {
            var results = _context.Companies.Where(c => c.Name.Contains("Pl")).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.Contains("pl")).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Apple", results[0].Name);

        }

        [TestMethod]
        public void TestStringLengthFilter()
        {
            var results = _context.Companies.Where(c => c.Name.Length > 10).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.Length<10).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Apple", results[0].Name);
        }
    }
}
