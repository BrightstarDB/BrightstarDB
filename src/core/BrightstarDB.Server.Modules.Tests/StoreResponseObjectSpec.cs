using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Server.Modules.Model;
using NUnit.Framework;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class StoreResponseObjectSpec
    {
        public void TestConstructorThrowsOnNullInput()
        {
            Assert.Throws<ArgumentNullException>(() => new StoreResponseModel(null));
        }

        public void TestConstructorThrowsOnEmptyInput()
        {
            Assert.Throws<ArgumentException>(() => new StoreResponseModel(""));
        }

        public void TestConstructorThrowsOnWhitespaceInput()
        {
            Assert.Throws<ArgumentException>(() => new StoreResponseModel(" "));
        }

        [Test]
        public void TestConstructorSetsAllFields()
        {
            var o = new StoreResponseModel("storeName");
            Assert.That(o, Is.Not.Null);
            Assert.That(o, Has.Property("Name").EqualTo("storeName"));
            Assert.That(o, Has.Property("Commits").EqualTo("storeName/commits"));
            Assert.That(o, Has.Property("Transactions").EqualTo("storeName/transactions"));
            Assert.That(o, Has.Property("Jobs").EqualTo("storeName/jobs"));
                           Assert.That(o, Has.Property("Statistics").EqualTo("storeName/statistics"));
        }
    }
}
