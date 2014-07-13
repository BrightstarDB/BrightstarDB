using System.Linq;
using BrightstarDB.Portable.Tests.EntityFramework.Model;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework
{
    [TestClass]
    public class StressTests : DataObjectStoreTestsBase
    {
        [DataTestMethod]
        [DataRow(PersistenceType.AppendOnly)]
        [DataRow(PersistenceType.Rewrite)]
        public void TestLoadAndQuery25K(PersistenceType persistenceType)
        {
            var storeName = MakeStoreName("TestLoadAndQuery10K", persistenceType);
            const int personCount = 25000;
            var hugosCount = personCount/TestDataLists.Firstnames.Count; // Estimated number of instances with name 'HUGO' we will generate
            using (var doStore = CreateStore(storeName, persistenceType))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var last10 = new IFoafPerson[10];
                    for (int i = 0; i < personCount; i++)
                    {
                        // Create an entity with properties set
                        var person = context.FoafPersons.Create();
                        person.Name = TestDataLists.Firstnames[i%TestDataLists.Firstnames.Count];
                        person.Organisation = TestDataLists.Organizations[i%TestDataLists.Organizations.Count];
                        foreach (var friend in last10.Where(friend => friend != null))
                        {
                            person.Knows.Add(friend);
                        }
                        last10[i%10] = person;
                    }
                    context.SaveChanges();

                    // Attempt a query using the same store handle
                    var hugos = context.FoafPersons.Where(p => p.Name.Equals("HUGO")).ToList();
                    Assert.IsTrue(hugos.Count >= hugosCount, "Expected at least {0} foafPerson instances returned with name HUGO", hugosCount);
                    hugosCount = hugos.Count; // Acual number may be one higher depending on where the iteration leaves off.
                }
            }

            // Try query with a new store context
            using (var doStore = OpenStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var hugos = context.FoafPersons.Where(p => p.Name.Equals("HUGO")).ToList();
                    Assert.AreEqual(hugosCount, hugos.Count, "Expected count of HUGOs to be unchanged when querying from a second context instance");
                }
            }

            // Try query with a completely new client session
            BrightstarDB.Client.BrightstarService.Shutdown();
            using (var doStore = OpenStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var hugos = context.FoafPersons.Where(p => p.Name.Equals("HUGO")).ToList();
                    Assert.AreEqual(hugosCount, hugos.Count, "Expected count of HUGOs to be unchanged when querying from a third context instance");
                }
            }
        }


    }
}
