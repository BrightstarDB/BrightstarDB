using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class CreateStoreRequestObjectSpec
    {
     
        [Test]
        public void TestCreateFullySpecifiedRequest()
        {
            var request = new CreateStoreRequestObject("foo", PersistenceType.AppendOnly);
            Assert.That(request, Has.Property("StoreName").EqualTo("foo"));
            Assert.That(request.PersistenceType, Is.EqualTo(0));
            Assert.That(request.GetBrightstarPersistenceType(), Is.EqualTo(PersistenceType.AppendOnly));

            request = new CreateStoreRequestObject("foo", PersistenceType.Rewrite);
            Assert.That(request, Has.Property("StoreName").EqualTo("foo"));
            Assert.That(request.PersistenceType, Is.EqualTo(1));
            Assert.That(request.GetBrightstarPersistenceType(), Is.EqualTo(PersistenceType.Rewrite));
        }

        [Test]
        public void TestCreatePartiallySpecifiedRequest()
        {
            var request = new CreateStoreRequestObject("foo");
            Assert.That(request, Has.Property("StoreName").EqualTo("foo"));
            Assert.That(request.PersistenceType, Is.EqualTo(-1));
            Assert.That(request.GetBrightstarPersistenceType(), Is.Null);
        }
    }
}
