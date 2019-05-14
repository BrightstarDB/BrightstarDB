using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture]
    public class DotNetRdfStorageServerConnectionStringTests
    {
        public void TestSesameServerConnector()
        {
            var doContext = BrightstarService.GetDataObjectContext(
                "type=dotNetRdf;configuration=" + Configuration.DataLocation +
                "mockStorageServerConfig.ttl;storageServer=server");
            Assert.That(doContext, Is.Not.Null);

            Assert.That(doContext.DoesStoreExist("foo"), Is.True);
            Assert.That(doContext.DoesStoreExist("bar"), Is.True);
            Assert.That(doContext.DoesStoreExist("bletch"), Is.False);
        }

    }
}