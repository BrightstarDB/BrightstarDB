using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class StringComparisonTests
    {
        private MyEntityContext _context;

        [OneTimeSetUp]
        public void SetUp()
        {
            _context = new MyEntityContext("type=embedded;storesDirectory=" + Configuration.StoreLocation + ";storeName=EFStringComparisonTests_" + DateTime.Now.Ticks);
            var np = new Company {Name = "NetworkedPlanet"};
            var apple = new Company {Name = "Apple"};
            _context.Companies.Add(np);
            _context.Companies.Add(apple);
            _context.SaveChanges();
        }

        [Test]
        public void TestStartsWith()
        {
            var results = _context.Companies.Where(c => c.Name.StartsWith("Net")).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet",results[0].Name);

#if !NETCOREAPP10 // No overload of StartsWith supports three arguments in .NET Core 1.0
            results = _context.Companies.Where(c => c.Name.StartsWith("net", true, CultureInfo.CurrentCulture)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", false, CultureInfo.CurrentCulture)).ToList();
            Assert.AreEqual(0, results.Count);
#endif

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.CurrentCultureIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.CurrentCulture)).ToList();
            Assert.AreEqual(0, results.Count);

#if !NETCOREAPP10 // InvariantCulture[IgnoreCase] is not supported by .NET Core 1.x
            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.InvariantCulture)).ToList();
            Assert.AreEqual(0, results.Count);
#endif

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.StartsWith("net", StringComparison.Ordinal)).ToList();
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void TestEndsWith()
        {
            var results = _context.Companies.Where(c => c.Name.EndsWith("net")).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net")).ToList();
            Assert.AreEqual(0, results.Count);

#if !NETCOREAPP10 // No overload of EndsWith supports three arguments in .NET Core 1.0
            results = _context.Companies.Where(c => c.Name.EndsWith("Net", true, CultureInfo.CurrentCulture)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", false, CultureInfo.CurrentCulture)).ToList();
            Assert.AreEqual(0, results.Count);
#endif

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.CurrentCultureIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.CurrentCulture)).ToList();
            Assert.AreEqual(0, results.Count);

#if !NETCOREAPP10 // InvariantCultureIgnoreCase is not supported by .NET Core 1.x
            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.InvariantCultureIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);
#endif

#if !NETCOREAPP10
            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.InvariantCulture)).ToList();
            Assert.AreEqual(0, results.Count);
#endif

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.EndsWith("Net", StringComparison.Ordinal)).ToList();
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void TestContains()
        {
            var results = _context.Companies.Where(c => c.Name.Contains("Pl")).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.Contains("pl")).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Apple", results[0].Name);

        }

        [Test]
        public void TestStringLengthFilter()
        {
            var results = _context.Companies.Where(c => c.Name.Length > 10).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("NetworkedPlanet", results[0].Name);

            results = _context.Companies.Where(c => c.Name.Length<10).ToList();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Apple", results[0].Name);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
