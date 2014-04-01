using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class SparqlContextTests
    {
        [Test]
        public void TestReadOnlyConnection()
        {
            var context = new MyEntityContext("type=sparql;query=http://dbpedia.org/sparql");
            Assert.IsNotNull(context);
        }

#if !PORTABLE && !WINDOWS_PHONE // SPARQL Update not supported on these platforms yet
        [Test]
        public void TestReadWriteConnection()
        {
            var doContext =
                new MyEntityContext(
                    "type=sparql;query=http://example.org/sparql;update=http://example.org/update");
            Assert.IsNotNull(doContext);
        }
#endif
    }
}
