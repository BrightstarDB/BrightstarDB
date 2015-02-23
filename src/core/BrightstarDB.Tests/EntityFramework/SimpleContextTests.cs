using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework;
using BrightstarDB.EntityFramework.Query;
using NUnit.Framework;
using System.ComponentModel;
using UnitTesting = NUnit.Framework;
#if !PORTABLE
using System.ComponentModel.DataAnnotations;
#endif

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class SimpleContextTests
    {
        private readonly IDataObjectContext _dataObjectContext;
        public SimpleContextTests()
        {
            var connectionString = new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation);
            _dataObjectContext = new EmbeddedDataObjectContext(connectionString);
        }

        [Test]
        public void TestCreateAndRetrieve()
        {
            string storeName = Guid.NewGuid().ToString();
            string personId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.Create();
                    Assert.IsNotNull(person);
                    context.SaveChanges();
                    Assert.IsNotNull(person.Id);
                    personId = person.Id;
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    Assert.IsNotNull(person);
                }
            }
        }

        [Test]
        public void TestShortcutCreate()
        {
            string storeName = "TestShortcutCreate" + DateTime.Now.Ticks;
            string aliceId;
            using (var context = CreateEntityContext(storeName))
            {
                var sales = new Department(context) {Name = "Sales", DeptId = 1};
                var bob = new Person(context) {Name = "Bob"};
                var alice = new Person(context) {Name = "Alice", Age = 54, Department = sales, Friends = new Collection<IPerson>{bob}};
                context.SaveChanges();
                aliceId = alice.Id;
            }

            using (var context = CreateEntityContext(storeName))
            {
                var alice = context.Persons.FirstOrDefault(x => x.Id == aliceId);
                Assert.IsNotNull(alice);
                Assert.AreEqual("Alice", alice.Name);
                Assert.AreEqual(54, alice.Age);
                Assert.IsNotNull(alice.Department);
                Assert.AreEqual("Sales", alice.Department.Name);
                Assert.AreEqual(1, alice.Friends.Count);
                Assert.AreEqual("Bob", alice.Friends.First().Name);
            }
        }

        [Test]
        [ExpectedException(typeof(EntityKeyRequiredException))]
        [NUnit.Framework.Ignore("Behaviour is changed - an entity can be created, but will not be tracked or saved until its identity is set")]
        public void TestCannotCreateEntityWithKey()
        {
            string storeName = "CannotCreateEntityWithKey_" + DateTime.UtcNow.Ticks;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    // Should throw an exception as the Name property is required to generate the key
                    context.StringKeyEntities.Create();
                    context.SaveChanges();
                }
            }
        }


        [Test]
        public void TestCustomTriplesQuery()
        {
            string storeName = Guid.NewGuid().ToString();
            var people = new Person[10];
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var person = new Person { Age = 40 - i, Name = "Person #" + i };
                        context.Persons.Add(person);
                        people[i] = person;
                    }
                    context.SaveChanges();
                }
            }


            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var query = @"
select  ?s ?p ?o
where {
   ?s ?p ?o.
        ?s a <http://www.example.org/schema/Person>
}
";
                    IList<Person> results;
                    results = context.ExecuteQuery<Person>(query).ToList();
                    Assert.AreEqual(10, results.Count);

                    foreach (Person person in results)
                    {
                        Assert.AreNotEqual(0, person.Age);
                    }
                }
            }
        }

        [Test]
        public void TestCustomTriplesQueryWithOrderedSubjects()
        {
            string storeName = Guid.NewGuid().ToString();
            var people = new Person[10];
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var person = new Person { Age = 40 - i, Name = "Person #" + i };
                        context.Persons.Add(person);
                        people[i] = person;
                    }
                    context.SaveChanges();
                }
            }


            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var query = @"
select  ?s ?p ?o
where {
   ?s ?p ?o.
        ?s a <http://www.example.org/schema/Person>
}
order by ?s
";
                    IList<Person> results;
                    results = context.ExecuteQuery<Person>(new SparqlQueryContext(query){ExpectTriplesWithOrderedSubjects = true}).ToList();
                    Assert.AreEqual(10, results.Count);

                    foreach (Person person in results)
                    {
                        Assert.AreNotEqual(0, person.Age);
                    }
                }
            }
        }


        [Test]
        public void TestCustomTriplesQueryWithMultipleResultTypes()
        {
            string storeName = Guid.NewGuid().ToString();
            var people = new Person[10];
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var person = new Person { Age = 40 - i, Name = "Person #" + i };
                        context.Persons.Add(person);
                        people[i] = person;

                        if (i >= 5)
                        {
                            var session = new Session { Speaker = "Person #" + i };
                            context.Sessions.Add(session);
                        }
                    }
                    context.SaveChanges();
                }
            }


            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var query = @"
select  ?s ?p ?o
where {
   ?s ?p ?o.
   {?s a <http://www.example.org/schema/Person>}
    union
   {?s a <http://www.example.org/schema/Session>}

}

";
                    var results = context.ExecuteQueryToResultTypes(query);
                    var persons = results[typeof(Person)].Cast<Person>().ToList();
                    Assert.AreEqual(10, persons.Count);

                    var sessions = results[typeof(Session)].Cast<Session>().ToList();
                    Assert.AreEqual(5, sessions.Count);

                }
            }



        }

        [Test]
        public void TestSetAndGetSimpleProperty()
        {
            string storeName = Guid.NewGuid().ToString();
            string personId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.Create();
                    Assert.IsNotNull(person);
                    person.Name = "Kal";
                    context.SaveChanges();
                    personId = person.Id;
                }
            }

            // Test that the property is still there when we retrieve the object again
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    Assert.IsNotNull(person);
                    Assert.IsNotNull(person.Name, "person.Name was NULL when retrieved back from server");
                    Assert.AreEqual("Kal", person.Name, "Unexpected Name property value");
                }
            }

            // Test we can also use the simple property in a LINQ query
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Name == "Kal");
                    Assert.IsNotNull(person, "Could not find person by Name");
                    Assert.AreEqual(personId, person.Id, "Query for person by name returned an unexpected person entity");

                    // Test we can use ToList()
                    var people = context.Persons.Where(p => p.Name == "Kal").ToList();
                    Assert.IsNotNull(people);
                    Assert.AreEqual(1, people.Count);
                    Assert.AreEqual(personId, people[0].Id);
                }
            }
        }

        [Test]
        public void TestOrderingOfResults()
        {
            string storeName = Guid.NewGuid().ToString();
            var peterDob = DateTime.Now.AddYears(-35);
            var anneDob = DateTime.Now.AddYears(-28);
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var joDob = DateTime.Now.AddYears(-34);
                    var mirandaDob = DateTime.Now.AddYears(-32);

                    var jo = context.Persons.Create();
                    Assert.IsNotNull(jo);
                    jo.Name = "Jo";
                    jo.DateOfBirth = joDob;
                    jo.Age = 34;
                    var peter = context.Persons.Create();
                    Assert.IsNotNull(peter);
                    peter.Name = "Peter";
                    peter.DateOfBirth = peterDob;
                    peter.Age = 35;
                    var miranda = context.Persons.Create();
                    Assert.IsNotNull(miranda);
                    miranda.Name = "Miranda";
                    miranda.DateOfBirth = mirandaDob;
                    miranda.Age = 32;
                    var anne = context.Persons.Create();
                    Assert.IsNotNull(anne);
                    anne.Name = "Anne";
                    anne.DateOfBirth = anneDob;
                    anne.Age = 28;

                    context.SaveChanges();
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var people = context.Persons.ToList();
                    Assert.AreEqual(4, people.Count, "Amount of people in context was not 4");

                    var orderedByName = context.Persons.OrderBy(p => p.Name).ToList();
                    var orderedByAge = context.Persons.OrderBy(p => p.Age).ToList();
                    var orderedByDob = context.Persons.OrderBy(p => p.DateOfBirth).ToList();

                    Assert.AreEqual("Anne", orderedByName[0].Name, "First in list was not alphabetically first");
                    Assert.AreEqual("Peter", orderedByName[3].Name, "Last in list was not alphabetically last");
                    Assert.AreEqual(28, orderedByAge[0].Age, "First in list was not numerically first");
                    Assert.AreEqual(35, orderedByAge[3].Age, "Last in list was not numerically last");
                    Assert.AreEqual(peterDob, orderedByDob[0].DateOfBirth, "First in list was not first by date");
                    Assert.AreEqual(anneDob, orderedByDob[3].DateOfBirth, "Last in list was not last by date");
                }
            }

        }

        [Test]
        public void TestGetAndSetDateTimeProperty()
        {
            string storeName = Guid.NewGuid().ToString();
            string personId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var person = context.Persons.Create();
                Assert.IsNotNull(person);
                person.Name = "Kal";
                person.DateOfBirth = new DateTime(1970, 12, 12);
                context.SaveChanges();
                personId = person.Id;
            }

            // Test that the property is still there when we retrieve the object again
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                    Assert.IsNotNull(person);
                    Assert.IsNotNull(person.Name, "person.Name was NULL when retrieved back from server");
                    Assert.AreEqual("Kal", person.Name, "Unexpected Name property value");
                    Assert.IsTrue(person.DateOfBirth.HasValue);
                    Assert.AreEqual(1970, person.DateOfBirth.Value.Year);
                    Assert.AreEqual(12, person.DateOfBirth.Value.Month);
                    Assert.AreEqual(12, person.DateOfBirth.Value.Day);
                }
            }

            // Test we can also use the simple property in a LINQ query
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.DateOfBirth == new DateTime(1970, 12, 12));
                    Assert.IsNotNull(person, "Could not find person by Date of Birth");
                    Assert.AreEqual(personId, person.Id,
                                    "Query for person by date of birth returned an unexpected person entity");

                    // Test we can set a nullable property back to null
                    person.DateOfBirth = null;
                    context.SaveChanges();
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = context.Persons.FirstOrDefault(p => p.Name.Equals("Kal"));
                    Assert.IsNotNull(person);
                    Assert.IsNull(person.DateOfBirth);
                }
            }
        }

        [Test]
        public void TestLoopThroughEntities()
        {
            string storeName = Guid.NewGuid().ToString();
            string homerId, bartId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bart = context.Persons.Create();
                    bart.Name = "Bart Simpson";
                    var homer = context.Persons.Create();
                    homer.Name = "Homer Simpson";
                    bart.Father = homer;

                    var marge = context.Persons.Create();
                    marge.Name = "Marge Simpson";
                    bart.Mother = marge;

                    context.SaveChanges();
                    homerId = homer.Id;
                    bartId = bart.Id;
                }
            }

            // Query with results converted to a list
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var homersKids = context.Persons.Where(p => p.Father.Id == homerId).ToList();
                    Assert.AreEqual(1, homersKids.Count, "Could not find Bart with SPARQL query for Homer's kids");
                    Assert.AreEqual(bartId, homersKids.First().Id);
                }
            }
        }

        [Test]
        public void TestSetAndGetSingleRelatedObject()
        {
            string storeName = Guid.NewGuid().ToString();
            string bartId, homerId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var bart = context.Persons.Create();
                bart.Name = "Bart Simpson";
                var homer = context.Persons.Create();
                homer.Name = "Homer Simpson";
                bart.Father = homer;
                context.SaveChanges();
                homerId = homer.Id;
                bartId = bart.Id;
            }

            // Test that the property is still there when we retrieve the object again
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bart = context.Persons.FirstOrDefault(p => p.Id == bartId);
                    Assert.IsNotNull(bart, "Could not find Bart by ID");
                    var bartFather = bart.Father;
                    Assert.IsNotNull(bartFather, "Father property was not present on the returned person object");
                    Assert.AreEqual(homerId, bartFather.Id, "Incorrect Father property value on returned object");
                }
            }
            // See if we can use the property in a query
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var homersKids = context.Persons.Where(p => p.Father.Id == homerId).ToList();
                    Assert.AreEqual(1, homersKids.Count, "Could not find Bart with SPARQL query for Homer's kids");
                    Assert.AreEqual(bartId, homersKids.First().Id);
                }
            }
        }

        [Test]
        public void TestPopulateEntityCollectionWithExistingEntities()
        {
            string storeName = Guid.NewGuid().ToString();
            string aliceId, bobId, carolId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                alice.Name = "Alice";
                var bob = context.Persons.Create();
                bob.Name = "Bob";
                var carol = context.Persons.Create();
                carol.Name = "Carol";
                alice.Friends.Add(bob);
                alice.Friends.Add(carol);
                context.SaveChanges();
                aliceId = alice.Id;
                bobId = bob.Id;
                carolId = carol.Id;
            }

            // See if we can access the collection on a loaded object
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var alice = context.Persons.FirstOrDefault(p => p.Id == aliceId);
                    Assert.IsNotNull(alice);
                    var friends = alice.Friends as IEntityCollection<IPerson>;
                    Assert.IsNotNull(friends);
                    Assert.IsFalse(friends.IsLoaded);
                    friends.Load();
                    Assert.IsTrue(friends.IsLoaded);
                    Assert.AreEqual(2, alice.Friends.Count);
                    Assert.IsTrue(alice.Friends.Any(p => p.Id.Equals(bobId)));
                    Assert.IsTrue(alice.Friends.Any(p => p.Id.Equals(carolId)));
                }
            }
        }

        [Test]
        public void TestSetEntityCollection()
        {
            string storeName = Guid.NewGuid().ToString();
            string aliceId, bobId, carolId, daveId, edwinaId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                var bob = context.Persons.Create();
                var carol = context.Persons.Create();
                alice.Friends = new List<IPerson> {bob, carol};
                context.SaveChanges();

                aliceId = alice.Id;
                bobId = bob.Id;
                carolId = carol.Id;
            }

            // See if we can access the collection on a loaded object
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var alice = context.Persons.FirstOrDefault(p => p.Id == aliceId);
                    Assert.IsNotNull(alice);
                    var friends = alice.Friends as IEntityCollection<IPerson>;
                    Assert.IsNotNull(friends);
                    Assert.IsFalse(friends.IsLoaded);
                    friends.Load();
                    Assert.IsTrue(friends.IsLoaded);
                    Assert.AreEqual(2, alice.Friends.Count);
                    Assert.IsTrue(alice.Friends.Any(p => p.Id.Equals(bobId)));
                    Assert.IsTrue(alice.Friends.Any(p => p.Id.Equals(carolId)));
                    var dave = context.Persons.Create();
                    var edwina = context.Persons.Create();
                    alice.Friends = new List<IPerson> {dave, edwina};
                    context.SaveChanges();
                    daveId = dave.Id;
                    edwinaId = edwina.Id;
                }
            }

            // See if we can access the collection on a loaded object
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var alice = context.Persons.FirstOrDefault(p => p.Id == aliceId);
                    Assert.IsNotNull(alice);
                    var friends = alice.Friends as IEntityCollection<IPerson>;
                    Assert.IsNotNull(friends);
                    Assert.IsFalse(friends.IsLoaded);
                    friends.Load();
                    Assert.IsTrue(friends.IsLoaded);
                    Assert.AreEqual(2, alice.Friends.Count);
                    Assert.IsTrue(alice.Friends.Any(p => p.Id.Equals(daveId)));
                    Assert.IsTrue(alice.Friends.Any(p => p.Id.Equals(edwinaId)));
                }
            }
        }

        
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetRelatedEntitiesToNullThrowsArgumentNullException()
        {
            var storeName = "TestSetRelatedEntitiesToNullThrowsArgumentNullException_" + DateTime.Now.Ticks;
            string aliceId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                var bob = context.Persons.Create();
                var carol = context.Persons.Create();
                alice.Friends = new List<IPerson> { bob, carol };
                context.SaveChanges();
                aliceId = alice.Id;
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                Assert.IsNotNull(alice);
                Assert.AreEqual(2, alice.Friends.Count);

                alice.Friends = null; // throws
            }
        }

        [Test]
        public void TestClearRelatedObjects()
        {
            var storeName = "TestClearRelatedObjects_" + DateTime.Now.Ticks;
            string aliceId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                var bob = context.Persons.Create();
                var carol = context.Persons.Create();
                alice.Friends = new List<IPerson> { bob, carol };
                context.SaveChanges();
                aliceId = alice.Id;
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                Assert.IsNotNull(alice);
                Assert.AreEqual(2, alice.Friends.Count);
                alice.Friends.Clear();
                Assert.AreEqual(0, alice.Friends.Count);
                context.SaveChanges();
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                Assert.IsNotNull(alice);
                Assert.AreEqual(0, alice.Friends.Count);
            }
        }

        [Test]
        public void TestOneToOneInverse()
        {
            string storeName = Guid.NewGuid().ToString();
            string aliceId, bobId, carolId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var alice = context.Persons.Create();
                    alice.Name = "Alice";
                    var bob = context.Animals.Create();
                    alice.Pet = bob;
                    context.SaveChanges();
                    aliceId = alice.Id;
                    bobId = bob.Id;
                }
            }

            // See if we can access the inverse property
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bob = context.Animals.FirstOrDefault(a => a.Id.Equals(bobId));
                    Assert.IsNotNull(bob);
                    Assert.IsNotNull(bob.Owner);
                    Assert.AreEqual(aliceId, bob.Owner.Id);

                    // see if we can access an item by querying against its inverse property
                    bob = context.Animals.FirstOrDefault(a => a.Owner.Id.Equals(aliceId));
                    Assert.IsNotNull(bob);

                    // check that alice.Pet refers to the same object as bob
                    bob.Name = "Bob";
                    Assert.IsNotNull(bob.Name);
                    Assert.AreEqual("Bob", bob.Name);
                    var alice = context.Persons.FirstOrDefault(a => a.Id.Equals(aliceId));
                    Assert.IsNotNull(alice);
                    var alicePet = alice.Pet;
                    Assert.IsNotNull(alicePet);
                    Assert.AreEqual(bob, alicePet);
                    Assert.AreEqual("Bob", alicePet.Name);

                    // Transfer object by changing the forward property
                    var carol = context.Persons.Create();
                    carol.Name = "Carol";
                    carol.Pet = bob;
                    carolId = carol.Id;
                    Assert.AreEqual(carol, bob.Owner);
                    Assert.IsNull(alice.Pet, "Expected alice.Pet to be null after owner change");
                    context.SaveChanges();
                }
            }

            // Check that changes to forward properties get persisted
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bob = context.Animals.FirstOrDefault(a => a.Owner.Id.Equals(carolId));
                    Assert.IsNotNull(bob);
                    Assert.AreEqual("Bob", bob.Name);
                    var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                    Assert.IsNotNull(alice);
                    Assert.IsNull(alice.Pet);
                    var carol = context.Persons.FirstOrDefault(p => p.Id.Equals(carolId));
                    Assert.IsNotNull(carol);
                    Assert.IsNotNull(carol.Pet);
                    Assert.AreEqual(bob, carol.Pet);

                    // Transfer object by changing inverse property
                    bob.Owner = alice;
                    Assert.IsNotNull(alice.Pet);
                    Assert.IsNull(carol.Pet);
                    context.SaveChanges();
                }
            }

            // Check that changes to inverse properties get persisted
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var bob = context.Animals.FirstOrDefault(a => a.Id.Equals(bobId));
                    Assert.IsNotNull(bob);
                    Assert.AreEqual("Bob", bob.Name);
                    Assert.AreEqual(aliceId, bob.Owner.Id);
                    var alice = context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId));
                    Assert.IsNotNull(alice);
                    Assert.IsNotNull(alice.Pet);
                    Assert.AreEqual(bob, alice.Pet);
                    var carol = context.Persons.FirstOrDefault(p => p.Id.Equals(carolId));
                    Assert.IsNotNull(carol);
                    Assert.IsNull(carol.Pet);
                }
            }
        }

        [Test]
        public void TestManyToManyInverse()
        {
            string storeName = Guid.NewGuid().ToString();
            string aliceId, bobId, jsId, cssId, jqueryId, rdfId;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                var context = new MyEntityContext(dataObjectStore);
                var alice = context.Persons.Create();
                alice.Name = "Alice";
                var bob = context.Persons.Create();
                bob.Name = "Bob";
                var js = context.Skills.Create();
                js.Name = "Javascript";
                var css = context.Skills.Create();
                css.Name = "CSS";
                var jquery = context.Skills.Create();
                jquery.Name = "JQuery";
                var rdf = context.Skills.Create();
                rdf.Name = "RDF";

                alice.Skills.Add(js);
                alice.Skills.Add(css);
                bob.Skills.Add(js);
                bob.Skills.Add(jquery);
                context.SaveChanges();

                aliceId = alice.Id;
                bobId = bob.Id;
                jsId = js.Id;
                cssId = css.Id;
                jqueryId = jquery.Id;
                rdfId = rdf.Id;
            }

            // See if we can access the inverse properties correctly
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var js = context.Skills.FirstOrDefault(x => x.Id.Equals(jsId));
                    Assert.IsNotNull(js);
                    Assert.AreEqual(2, js.SkilledPeople.Count);
                    Assert.IsTrue(js.SkilledPeople.Any(x => x.Id.Equals(aliceId)));
                    Assert.IsTrue(js.SkilledPeople.Any(x => x.Id.Equals(bobId)));
                    var css = context.Skills.FirstOrDefault(x => x.Id.Equals(cssId));
                    Assert.IsNotNull(css);
                    Assert.AreEqual(1, css.SkilledPeople.Count);
                    Assert.IsTrue(css.SkilledPeople.Any(x => x.Id.Equals(aliceId)));
                    var jquery = context.Skills.FirstOrDefault(x => x.Id.Equals(jqueryId));
                    Assert.IsNotNull(jquery);
                    Assert.AreEqual(1, jquery.SkilledPeople.Count);
                    Assert.IsTrue(jquery.SkilledPeople.Any(x => x.Id.Equals(bobId)));
                    var rdf = context.Skills.FirstOrDefault(x => x.Id.Equals(rdfId));
                    Assert.IsNotNull(rdf);
                    Assert.AreEqual(0, rdf.SkilledPeople.Count);

                    //  Test adding to an inverse property with some existing values and an inverse property with no existing values
                    var bob = context.Persons.FirstOrDefault(x => x.Id.Equals(bobId));
                    Assert.IsNotNull(bob);
                    var alice = context.Persons.FirstOrDefault(x => x.Id.Equals(aliceId));
                    Assert.IsNotNull(alice);
                    css.SkilledPeople.Add(bob);
                    Assert.AreEqual(2, css.SkilledPeople.Count);
                    Assert.IsTrue(css.SkilledPeople.Any(x => x.Id.Equals(bobId)));
                    Assert.AreEqual(3, bob.Skills.Count);
                    Assert.IsTrue(bob.Skills.Any(x => x.Id.Equals(cssId)));
                    rdf.SkilledPeople.Add(alice);
                    Assert.AreEqual(1, rdf.SkilledPeople.Count);
                    Assert.IsTrue(rdf.SkilledPeople.Any(x => x.Id.Equals(aliceId)));
                    Assert.AreEqual(3, alice.Skills.Count);
                    Assert.IsTrue(alice.Skills.Any(x => x.Id.Equals(rdfId)));
                }
            }
        }

        [Test]
        [UnitTesting::Description("Tests a property that in the forward direction is a many-to-one relationship and its inverse property as a one to many relationship")]
        public void TestManyToOneInverse()
        {
            string storeName = Guid.NewGuid().ToString();
            string rootId, skillAId, skillBId, childSkillId, childSkill2Id;
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {

                    var root = context.Skills.Create();
                    root.Name = "Root";
                    var skillA = context.Skills.Create();
                    skillA.Parent = root;
                    skillA.Name = "Skill A";
                    var skillB = context.Skills.Create();
                    skillB.Parent = root;
                    skillB.Name = "Skill B";

                    Assert.IsNotNull(root.Children);
                    Assert.AreEqual(2, root.Children.Count);
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(skillA.Id)));
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(skillB.Id)));
                    context.SaveChanges();

                    rootId = root.Id;
                    skillAId = skillA.Id;
                    skillBId = skillB.Id;
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var root = context.Skills.FirstOrDefault(x => x.Id.Equals(rootId));
                    Assert.IsNotNull(root);
                    var childSkill = context.Skills.Create();
                    childSkill.Name = "Child Skill";
                    childSkill.Parent = root;
                    Assert.AreEqual(3, root.Children.Count);
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(childSkill.Id)));
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(skillAId)));
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(skillBId)));
                    context.SaveChanges();
                    childSkillId = childSkill.Id;
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var root = context.Skills.FirstOrDefault(x => x.Id.Equals(rootId));
                    Assert.IsNotNull(root);
                    Assert.AreEqual(3, root.Children.Count);
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(childSkillId)));
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(skillAId)));
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(skillBId)));
                    var childSkill2 = context.Skills.Create();
                    childSkill2.Name = "Child Skill 2";
                    childSkill2.Parent = root;
                    Assert.AreEqual(4, root.Children.Count);
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(childSkill2.Id)));
                    context.SaveChanges();
                    childSkill2Id = childSkill2.Id;
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var root = context.Skills.FirstOrDefault(x => x.Id.Equals(rootId));
                    var skillA = context.Skills.FirstOrDefault(x => x.Id.Equals(skillAId));
                    var childSkill2 = context.Skills.FirstOrDefault(x => x.Id.Equals(childSkill2Id));
                    Assert.IsNotNull(root);
                    Assert.IsNotNull(skillA);
                    Assert.IsNotNull(childSkill2);
                    Assert.AreEqual(4, root.Children.Count);
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(childSkillId)));
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(skillAId)));
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(skillBId)));
                    Assert.IsTrue(root.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    // Move a skill to a new parent
                    childSkill2.Parent = skillA;
                    Assert.AreEqual(3, root.Children.Count);
                    Assert.IsFalse(root.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    Assert.AreEqual(1, skillA.Children.Count);
                    Assert.IsTrue(skillA.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    context.SaveChanges();
                }
            }

            // Check the move has persisted
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var root = context.Skills.FirstOrDefault(x => x.Id.Equals(rootId));
                    var skillA = context.Skills.FirstOrDefault(x => x.Id.Equals(skillAId));
                    var childSkill2 = context.Skills.FirstOrDefault(x => x.Id.Equals(childSkill2Id));
                    Assert.IsNotNull(root);
                    Assert.IsNotNull(skillA);
                    Assert.IsNotNull(childSkill2);
                    Assert.AreEqual(3, root.Children.Count);
                    Assert.IsFalse(root.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    Assert.AreEqual(1, skillA.Children.Count);
                    Assert.IsTrue(skillA.Children.Any(x => x.Id.Equals(childSkill2Id)));
                    Assert.AreEqual(skillA.Id, childSkill2.Parent.Id);
                    Assert.AreEqual(root.Id, childSkill2.Parent.Parent.Id);
                }
            }
        }
       
        /// <summary>
        /// Tests for a property that in its forward direction is a one-to-many property (i.e. a collection)
        /// and in its inverse is a many-to-one property (i.e. a single-valued property).
        /// </summary>
        [Test]
        public void TestOneToManyInverse()
        {
            string storeName = "SimpleContextTests.TestOneToManyInverse_" + DateTime.Now.Ticks;
            var connectionString = "type=embedded;storesDirectory=c:\\brightstar;storeName=" + storeName;
            string market1Id, market2Id, companyAId, companyBId, companyCId, companyDId;
            using (var context = new MyEntityContext(connectionString))
            {

                var market1 = context.Markets.Create();
                var market2 = context.Markets.Create();
                market1.Name = "Market1";
                market2.Name = "Market2";
                var companyA = context.Companies.Create();
                var companyB = context.Companies.Create();
                var companyC = context.Companies.Create();
                companyA.Name = "CompanyA";
                companyB.Name = "CompanyB";
                companyC.Name = "CompanyC";

                market1Id = market1.Id;
                market2Id = market2.Id;
                companyAId = companyA.Id;
                companyBId = companyB.Id;
                companyCId = companyC.Id;

                market1.ListedCompanies.Add(companyA);
                market2.ListedCompanies.Add(companyB);

                Assert.AreEqual(market1, companyA.ListedOn);
                Assert.AreEqual(market2, companyB.ListedOn);
                Assert.IsNull(companyC.ListedOn);
                context.SaveChanges();
            }
            using (var context = new MyEntityContext(connectionString))
            {
                var market1 = context.Markets.FirstOrDefault(x => x.Id.Equals(market1Id));
                var market2 = context.Markets.FirstOrDefault(x => x.Id.Equals(market2Id));
                var companyA = context.Companies.FirstOrDefault(x => x.Id.Equals(companyAId));
                var companyB = context.Companies.FirstOrDefault(x => x.Id.Equals(companyBId));
                var companyC = context.Companies.FirstOrDefault(x => x.Id.Equals(companyCId));
                Assert.IsNotNull(market1);
                Assert.IsNotNull(market2);
                Assert.IsNotNull(companyA);
                Assert.IsNotNull(companyB);
                Assert.IsNotNull(companyC);
                Assert.AreEqual(market1, companyA.ListedOn);
                Assert.AreEqual(market2, companyB.ListedOn);
                Assert.IsNull(companyC.ListedOn);

                // Add item to collection
                market1.ListedCompanies.Add(companyC);

                Assert.AreEqual(market1, companyA.ListedOn);
                Assert.AreEqual(market1, companyC.ListedOn);
                Assert.AreEqual(2, market1.ListedCompanies.Count);
                Assert.IsTrue(market1.ListedCompanies.Any(x => x.Id.Equals(companyA.Id)));
                Assert.IsTrue(market1.ListedCompanies.Any(x => x.Id.Equals(companyC.Id)));
                context.SaveChanges();
            }

            using (var context = new MyEntityContext(connectionString))
            {
                var market1 = context.Markets.FirstOrDefault(x => x.Id.Equals(market1Id));
                var market2 = context.Markets.FirstOrDefault(x => x.Id.Equals(market2Id));
                var companyA = context.Companies.FirstOrDefault(x => x.Id.Equals(companyAId));
                var companyB = context.Companies.FirstOrDefault(x => x.Id.Equals(companyBId));
                var companyC = context.Companies.FirstOrDefault(x => x.Id.Equals(companyCId));
                Assert.IsNotNull(market1);
                Assert.IsNotNull(market2);
                Assert.IsNotNull(companyA);
                Assert.IsNotNull(companyB);
                Assert.IsNotNull(companyC);

                Assert.AreEqual(market1, companyA.ListedOn);
                Assert.AreEqual(market1, companyC.ListedOn);
                Assert.AreEqual(2, market1.ListedCompanies.Count);
                Assert.IsTrue(market1.ListedCompanies.Any(x => x.Id.Equals(companyA.Id)));
                Assert.IsTrue(market1.ListedCompanies.Any(x => x.Id.Equals(companyC.Id)));

                // Set the single-valued inverse property
                var companyD = context.Companies.Create();
                companyD.Name = "CompanyD";
                companyD.ListedOn = market2;
                companyDId = companyD.Id;

                Assert.AreEqual(market2, companyB.ListedOn);
                Assert.AreEqual(market2, companyD.ListedOn);
                Assert.AreEqual(2, market2.ListedCompanies.Count);
                Assert.IsTrue(market2.ListedCompanies.Any(x => x.Id.Equals(companyB.Id)));
                Assert.IsTrue(market2.ListedCompanies.Any(x => x.Id.Equals(companyD.Id)));
                context.SaveChanges();
            }
            using (var context = new MyEntityContext(connectionString))
            {
                var market2 = context.Markets.FirstOrDefault(x => x.Id.Equals(market2Id));
                var companyB = context.Companies.FirstOrDefault(x => x.Id.Equals(companyBId));
                var companyD = context.Companies.FirstOrDefault(x => x.Id.Equals(companyDId));
                Assert.IsNotNull(market2);
                Assert.IsNotNull(companyB);
                Assert.IsNotNull(companyD);
                Assert.AreEqual(market2, companyB.ListedOn);
                Assert.AreEqual(market2, companyD.ListedOn);
                Assert.AreEqual(2, market2.ListedCompanies.Count);
                Assert.IsTrue(market2.ListedCompanies.Any(x => x.Id.Equals(companyB.Id)));
                Assert.IsTrue(market2.ListedCompanies.Any(x => x.Id.Equals(companyD.Id)));
            }
        }


        [Test]
        public void TestSetContextAndIdentityProperties()
        {
            string storeName = Guid.NewGuid().ToString();
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var person = new Person
                        {
                            Context = context,
                            Id = "http://example.org/people/123",
                            Name = "Kal",
                            DateOfBirth = new DateTime(1970, 12, 12)
                        };

                    context.SaveChanges();
                }
            }
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var found =
                        context.Persons.FirstOrDefault(p => p.Id.Equals("http://example.org/people/123"));
                    Assert.IsNotNull(found);
                }
            }
        }

        [Test]
        public void TestSetPropertiesThenAttach()
        {
            string storeName = Guid.NewGuid().ToString();
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    // ReSharper disable UseObjectOrCollectionInitializer
                    // Purposefully setting properties and then attaching Context property
                    var person = new Person
                        {
                            Name = "Kal",
                            DateOfBirth = new DateTime(1970, 12, 12),
                            Friends = new List<IPerson>
                                {
                                    new Person {Name = "Gra", Id = "http://example.org/people/1234"},
                                    new Person {Name = "Stu", Id = "http://example.org/people/456"}
                                }
                        };
                    person.Id = "http://example.org/people/123";
                    person.Context = context;
                    // ReSharper restore UseObjectOrCollectionInitializer
                    context.SaveChanges();
                }
            }
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var found =
                        context.Persons.FirstOrDefault(p => p.Id.Equals("http://example.org/people/123"));
                    Assert.IsNotNull(found);
                    Assert.AreEqual("Kal", found.Name);
                    Assert.AreEqual(new DateTime(1970, 12, 12), found.DateOfBirth);

                    found = context.Persons.FirstOrDefault(p => p.Id.Equals("http://example.org/people/1234"));
                    Assert.IsNotNull(found);
                    Assert.AreEqual("Gra", found.Name);

                    found = context.Persons.FirstOrDefault(p => p.Id.Equals("http://example.org/people/456"));
                    Assert.IsNotNull(found);
                    Assert.AreEqual("Stu", found.Name);
                }
            }
        }

        [Test]
        public void TestBaseResourceAddress()
        {
            string storeName = Guid.NewGuid().ToString();
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var skill = new Skill {Id = "foo", Name = "Foo"};
                    context.Skills.Add(skill);
                    var otherSkill = new Skill {Id = "bar", Name = "Bar", Context = context};
                    var yetAnotherSkill = new Skill {Name = "Bletch"};
                    context.Skills.Add(yetAnotherSkill);
                    context.SaveChanges();

                    var found = context.Skills.FirstOrDefault(s => s.Id.Equals("foo"));
                    Assert.IsNotNull(found);
                    Assert.AreEqual("foo", found.Id);
                    Assert.AreEqual("Foo", found.Name);

                    found = context.Skills.FirstOrDefault(s => s.Id.Equals("bar"));
                    Assert.IsNotNull(found);
                    Assert.AreEqual("bar", found.Id);
                    Assert.AreEqual("Bar", found.Name);

                    found = context.Skills.FirstOrDefault(s => s.Name.Equals("Bletch"));
                    Assert.IsNotNull(found);
                    Guid foundId;
                    Assert.IsTrue(Guid.TryParse(found.Id, out foundId));
                }
            }
        }

        //[Ignore]
        [Test]
        public void TestAddGeneratesIdentity()
        {
            string storeName = Guid.NewGuid().ToString();
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var skill = new Skill {Name = "Bar"};
                    var person = new Person {Name = "Kal"};
                    //var person2 = new Person2 {Name = "Jen"};
                    var company = new Company {Name = "NetworkedPlanet"};
                    context.Persons.Add(person);
                    //context.Person2s.Add(person2);
                    context.Skills.Add(skill);
                    context.Companies.Add(company);
                    context.SaveChanges();
                }
            }
            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    var foundPerson = context.Persons.FirstOrDefault(p => p.Name.Equals("Kal"));
                    //var foundPerson2 = context.Person2s.Where(p => p.Name.Equals("Jen")).FirstOrDefault();
                    var foundSkill = context.Skills.FirstOrDefault(s => s.Name.Equals("Bar"));
                    var foundCompany = context.Companies.FirstOrDefault(s => s.Name.Equals("NetworkedPlanet"));
                    Assert.IsNotNull(foundPerson);
                    //Assert.IsNotNull(foundPerson2);
                    Assert.IsNotNull(foundSkill);
                    Assert.IsNotNull(foundCompany);

                    // Generated Ids should be GUIDs
                    Guid g;
                    Assert.IsTrue(Guid.TryParse(foundPerson.Id, out g));
                    Assert.IsTrue(Guid.TryParse(foundCompany.Id, out g));
                    Assert.IsTrue(Guid.TryParse(foundSkill.Id, out g));
                }
            }
        }

        [Test]
        public void TestIdentifierPrefix()
        {
            var dataStoreName = "TestIdentifierPrefix_" + DateTime.Now.Ticks;
            using (var dataStore = _dataObjectContext.CreateStore(dataStoreName))
            {
                using (var context = new MyEntityContext(dataStore))
                {
                    var fido = context.Animals.Create();
                    fido.Name = "Fido";
                    var foafPerson = context.FoafPersons.Create();
                    foafPerson.Name = "Bob";
                    var skill = context.Skills.Create();
                    skill.Name = "Testing";
                    var company = context.Companies.Create();
                    company.Name = "BrightstarDB";
                    context.SaveChanges();
                }
                var fidoDo = dataStore.BindDataObjectsWithSparql(
                    "SELECT ?f WHERE { ?f a <http://www.example.org/schema/Animal> }").FirstOrDefault();
                Assert.IsNotNull(fidoDo);
                Assert.IsTrue(fidoDo.Identity.StartsWith("http://brightstardb.com/instances/Animals/"));
            }
        }

        [Test]
        public void TestEmptyStringIdentifierPrefix()
        {
            var dataStoreName = "TestEmptyStringIdentifierPrefix_" + DateTime.UtcNow.Ticks;
            using (var dataStore = _dataObjectContext.CreateStore(dataStoreName))
            {
                using (var context = new MyEntityContext(dataStore))
                {
                    var fido = new UriEntity {Id = "http://brightstardb.com/instances/Animals/fido", Label = "Fido"};
                    context.UriEntities.Add(fido);
                    var bob = new UriEntity(context) { Id = "http://example.org/people/bob", Label = "Bob" };
                    context.SaveChanges();
                }
                var fidoDo =
                    dataStore.BindDataObjectsWithSparql(
                        "SELECT ?f WHERE { ?f <http://www.w3.org/2000/01/rdf-schema#label> \"Fido\"^^<http://www.w3.org/2001/XMLSchema#string> }").FirstOrDefault();
                Assert.IsNotNull(fidoDo);
                Assert.AreEqual(fidoDo.Identity, "http://brightstardb.com/instances/Animals/fido");
                using (var context = new MyEntityContext(dataStore))
                {
                    var fido =
                        context.UriEntities.FirstOrDefault(
                            x => x.Id.Equals("http://brightstardb.com/instances/Animals/fido"));
                    Assert.NotNull(fido);
                    Assert.That(fido.Label, Is.EqualTo("Fido"));

                    var bob = context.UriEntities.FirstOrDefault(x => x.Label.Equals("Bob"));
                    Assert.NotNull(bob);
                    Assert.That(bob.Id, Is.EqualTo("http://example.org/people/bob"));
                }
            }
        }

        [Test]
        public void TestCreateMethodGeneratesValidUriForEmptyIdentifierPrefix()
        {
            var dataStoreName = "TestEmptyStringIdentifierPrefix_" + DateTime.UtcNow.Ticks;
            using (var dataStore = _dataObjectContext.CreateStore(dataStoreName))
            {
                using (var context = new MyEntityContext(dataStore))
                {
                    var test = context.UriEntities.Create();
                    test.Label = "Test";
                    context.SaveChanges();
                }
                var testDo =
                    dataStore.BindDataObjectsWithSparql(
                        "SELECT ?f WHERE { ?f <http://www.w3.org/2000/01/rdf-schema#label> \"Test\"^^<http://www.w3.org/2001/XMLSchema#string> }")
                        .FirstOrDefault();
                Assert.IsNotNull(testDo);
                Assert.IsTrue(testDo.Identity.StartsWith(Constants.GeneratedUriPrefix), "Unexpected identity: {0}. Expected a URI with the prefix {1}", testDo.Identity, Constants.GeneratedUriPrefix);
            }
        }

        [Test]
        public void TestSkipAndTake()
        {
            string storeName = Guid.NewGuid().ToString();
            var people = new Person[10];
            using (var dataObjectStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var person = new Person {Age = 40 - i, Name = "Person #" + i};
                        context.Persons.Add(person);
                        people[i] = person;
                    }
                    context.SaveChanges();
                }
            }

            using (var dataObjectStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(dataObjectStore))
                {

                    // Take, skip and skip and take with no other query expression
                    var top3 = context.Persons.Take(3).ToList();
                    Assert.AreEqual(3, top3.Count);
                    foreach (var p in top3)
                    {
                        Assert.IsTrue(people.Any(x => p.Id.Equals(x.Id)));
                    }
                    var after3 = context.Persons.Skip(3).ToList();
                    Assert.AreEqual(7, after3.Count);
                    var nextPage = context.Persons.Skip(3).Take(3).ToList();
                    Assert.AreEqual(3, nextPage.Count);

                    // Combined with a sort expression
                    var top3ByAge = context.Persons.OrderByDescending(p => p.Age).Take(3).ToList();
                    Assert.AreEqual(3, top3ByAge.Count);
                    foreach (var p in top3ByAge) Assert.IsTrue(p.Age >= 38);

                    var allButThreeOldest = context.Persons.OrderByDescending(p => p.Age).Skip(3).ToList();
                    Assert.AreEqual(7, allButThreeOldest.Count);
                    foreach (var p in allButThreeOldest) Assert.IsFalse(p.Age >= 38);

                    var nextThreeOldest = context.Persons.OrderByDescending(p => p.Age).Skip(3).Take(3).ToList();
                    Assert.AreEqual(3, nextThreeOldest.Count);
                    foreach (var p in nextThreeOldest) Assert.IsTrue(p.Age < 38 && p.Age > 34);
                }
            }
        }

        [Test]
        public void TestConnectionString()
        {
            var storeName = Guid.NewGuid().ToString();
            BrightstarService.GetClient("type=embedded;storesdirectory=c:\\brightstar").CreateStore(storeName);
            string personId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.Persons.Create();
                Assert.IsNotNull(person);
                context.SaveChanges();
                Assert.IsNotNull(person.Id);
                personId = person.Id;
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                Assert.IsNotNull(person);
            }
        }

        [Test]
        public void TestConnectionStringCreatesStore()
        {
            var storeName = Guid.NewGuid().ToString();
            string personId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.Persons.Create();
                Assert.IsNotNull(person);
                context.SaveChanges();
                Assert.IsNotNull(person.Id);
                personId = person.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                Assert.IsNotNull(person);
            }
        }


        [Test]
        public void TestMultipleConnections()
        {
            var storeName = Guid.NewGuid().ToString();
            string personId;
            var client = BrightstarService.GetClient("type=embedded;storesdirectory=c:\\brightstar");
            client.CreateStore(storeName);

            // TODO: Reinstate this when GetStoreData is added back to the service interface
            //client.GetStoreData(storeName).Close();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.Persons.Create();
                Assert.IsNotNull(person);
                context.SaveChanges();
                Assert.IsNotNull(person.Id);
                personId = person.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                Assert.IsNotNull(person);
            }
        }

        [Test]
        public void TestSetTwoInverse()
        {
            var storeName = "TestSetTwoInverse_" + DateTime.Now.Ticks;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var market = context.Markets.Create();
                var company1 = context.Companies.Create();
                var company2 = context.Companies.Create();
                company1.ListedOn = market;
                company2.ListedOn = market;
                context.SaveChanges();

                market = context.Markets.FirstOrDefault();
                Assert.IsNotNull(market);
                Assert.IsNotNull(market.ListedCompanies);
                Assert.AreEqual(2, market.ListedCompanies.Count);
                var company3 = context.Companies.Create();
                market.ListedCompanies.Add(company3);
                context.SaveChanges();
            }
        }

        [Test]
        public void TestSetCollectionWithManyToOneInverse()
        {
            var storeName = "TestSetCollectionWithManyToOneInverse_" + DateTime.Now.Ticks;
            string marketId;
            using (var context = CreateEntityContext(storeName))
            {
                var market = new Market
                {
                    ListedCompanies = new[]
                    {
                        new Company {Name = "CompanyA"},
                        new Company {Name = "CompanyB"},
                        new Company {Name = "CompanyC"},
                    }
                };
                context.Markets.Add(market);
                context.SaveChanges();
                marketId = market.Id;
            }

            using (var context = CreateEntityContext(storeName))
            {
                var market = context.Markets.FirstOrDefault(x => x.Id.Equals(marketId));
                Assert.IsNotNull(market);
                Assert.AreEqual(3, market.ListedCompanies.Count);
                Assert.IsTrue(market.ListedCompanies.Any(x => x.Name.Equals("CompanyA")));
                Assert.IsTrue(market.ListedCompanies.Any(x => x.Name.Equals("CompanyB")));
                Assert.IsTrue(market.ListedCompanies.Any(x => x.Name.Equals("CompanyC")));
            }
        }

        [Test]
        public void TestQueryOnPrefixedIdentifier()
        {
            var storeName = Guid.NewGuid().ToString();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var skill = new Skill {Name = "Fencing", Id = "fencing"};
                context.Skills.Add(skill);
                context.SaveChanges();

                var skillId = skill.Id;
                Assert.IsNotNull(skillId);
                Assert.AreEqual("fencing", skill.Id);

                var foundSkill = context.Skills.FirstOrDefault(s => s.Id.Equals(skillId));
                Assert.IsNotNull(foundSkill);
                Assert.AreEqual("Fencing", foundSkill.Name);
            }
        }

        [Test]
        public void TestGreaterThanLessThan()
        {
            var storeName = Guid.NewGuid().ToString();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var apple = context.Companies.Create();
                apple.Name = "Apple";
                apple.CurrentMarketCap = 1.0;
                apple.HeadCount = 150000;

                var ibm = context.Companies.Create();
                ibm.Name = "IBM";
                ibm.CurrentMarketCap = 2.0;
                ibm.HeadCount = 200000;

                var np = context.Companies.Create();
                np.Name = "NetworkedPlanet";
                np.CurrentMarketCap = 3.0;
                np.HeadCount = 4;

                context.SaveChanges();

                var smallCompanies = context.Companies.Where(x => x.HeadCount < 10).ToList();
                Assert.AreEqual(1, smallCompanies.Count);
                Assert.AreEqual(np.Id, smallCompanies[0].Id);

                var bigCompanies = context.Companies.Where(x => x.HeadCount > 1000).ToList();
                Assert.AreEqual(2, bigCompanies.Count);
                Assert.IsTrue(bigCompanies.Any(x => x.Id.Equals(apple.Id)));
                Assert.IsTrue(bigCompanies.Any(x => x.Id.Equals(ibm.Id)));
            }
        }

        [Test]
        public void TestSetAndGetLiteralsCollection()
        {
            var storeName = Guid.NewGuid().ToString();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var agent7 = context.FoafAgents.Create();
                agent7.MboxSums.Add("mboxsum1");
                agent7.MboxSums.Add("mboxsum2");
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var agent7 = context.FoafAgents.FirstOrDefault();
                Assert.IsNotNull(agent7);
                Assert.AreEqual(2, agent7.MboxSums.Count);
                Assert.IsTrue(agent7.MboxSums.Any(x => x.Equals("mboxsum1")));
                Assert.IsTrue(agent7.MboxSums.Any(x => x.Equals("mboxsum2")));
                agent7.MboxSums = new List<string> {"replacement1", "replacement2", "replacement3"};
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var agent7 = context.FoafAgents.FirstOrDefault();
                Assert.IsNotNull(agent7);
                Assert.AreEqual(3, agent7.MboxSums.Count);
                Assert.IsTrue(agent7.MboxSums.Any(x => x.Equals("replacement1")));
                Assert.IsTrue(agent7.MboxSums.Any(x => x.Equals("replacement2")));
                Assert.IsTrue(agent7.MboxSums.Any(x => x.Equals("replacement3")));

                var found = context.FoafAgents.Where(x => x.MboxSums.Contains("replacement2"));
                Assert.IsNotNull(found);

                agent7.MboxSums.Clear();
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var agent7 = context.FoafAgents.FirstOrDefault();
                Assert.IsNotNull(agent7);
                Assert.AreEqual(0, agent7.MboxSums.Count);
            }
        }

        [Test]
        public void TestSetByteArray()
        {
            var storeName = "SetByteArray_" + Guid.NewGuid();
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var testEntity = context.Entities.Create();
                testEntity.SomeByteArray = new byte[] {0, 1, 2, 3, 4};
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var e = context.Entities.FirstOrDefault();
                Assert.IsNotNull(e);
                Assert.IsNotNull(e.SomeByteArray);
                Assert.AreEqual(5, e.SomeByteArray.Count());
                for (byte i = 0; i < 5; i++)
                {
                    Assert.AreEqual(i, e.SomeByteArray[i]);
                }
            }
        }

        [Test]
        public void TestSetGuid()
        {
            var storeName = "SetGuid_" + DateTime.Now.Ticks;
            var testGuid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            using (var doStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var testEntity = context.Entities.Create();
                    testEntity.SomeGuid = testGuid;
                    context.SaveChanges();
                }
            }
            using (var doStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var testEntity = context.Entities.FirstOrDefault();
                    Assert.IsNotNull(testEntity);
                    var testEntityId = testEntity.Id;
                    Assert.AreEqual(testGuid, testEntity.SomeGuid);

                    // Verify we can use a Guid value in a search
                    testEntity = context.Entities.FirstOrDefault(e => e.SomeGuid.Equals(testGuid));
                    Assert.IsNotNull(testEntity);
                    Assert.AreEqual(testEntityId, testEntity.Id);
                    Assert.IsNull(context.Entities.FirstOrDefault(e=>e.SomeGuid.Equals(Guid.Empty)));
                }
            }
        }

        [Test]
        public void TestGuidAndNullableGuidDefaults()
        {
            var storeName = "TestGuidAndNullableGuidDefaults_" + DateTime.Now.Ticks;
            string testEntityId;
            using (var doStore = _dataObjectContext.CreateStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var testEntity = context.Entities.Create();
                    testEntityId = testEntity.Id;
                    context.SaveChanges();
                }
            }
            using (var doStore = _dataObjectContext.OpenStore(storeName))
            {
                using (var context = new MyEntityContext(doStore))
                {
                    var testEntity = context.Entities.FirstOrDefault(x => x.Id.Equals(testEntityId));
                    Assert.IsNotNull(testEntity);
                    Assert.IsNotNull(testEntity.SomeGuid);
                    Assert.AreEqual(Guid.Empty, testEntity.SomeGuid);
                    Assert.IsFalse(testEntity.SomeNullableGuid.HasValue);
                }
            }
        }

        [Test]
        public void TestSetEnumeration()
        {
            var storeName = "SetEnumeration_" + DateTime.Now.Ticks;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var testEntity = context.Entities.Create();
                testEntity.SomeEnumeration = TestEnumeration.Third;
                context.SaveChanges();
            }
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var e = context.Entities.FirstOrDefault();
                Assert.IsNotNull(e);
                Assert.AreEqual(TestEnumeration.Third, e.SomeEnumeration);
            }
        }

        [Test]
        public void TestQueryOnEnumeration()
        {
            var storeName = "QueryEnumeration_" + DateTime.Now.Ticks;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var entity1 = context.Entities.Create();
                var entity2 = context.Entities.Create();
                var entity3 = context.Entities.Create();
                entity1.SomeString = "Entity1";
                entity1.SomeEnumeration = TestEnumeration.First;
                entity2.SomeString = "Entity2";
                entity2.SomeEnumeration = TestEnumeration.Second;
                entity3.SomeString = "Entity3";
                entity3.SomeEnumeration = TestEnumeration.Second;
                context.SaveChanges();

                Assert.AreEqual(1,
                                context.Entities.Count(e => e.SomeEnumeration == TestEnumeration.First));
                Assert.AreEqual(2,
                                context.Entities.Count(e => e.SomeEnumeration == TestEnumeration.Second));
                Assert.AreEqual(0,
                                context.Entities.Count(e => e.SomeEnumeration == TestEnumeration.Third));
            }
        }

        [Test]
        [ExpectedException(typeof(TransactionPreconditionsFailedException))]
        public void TestOptimisticLocking()
        {
            var storeName = "TestOptimisticLocking_" + DateTime.Now.Ticks;
            string personId;
            using (
                var context = new MyEntityContext(
                    "type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName, true))
            {
                var person = context.Persons.Create();
                context.SaveChanges();
                personId = person.Id;
            }

            using (
                var context1 = new MyEntityContext(
                    "type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName, true))
            {
                var person1 = context1.Persons.FirstOrDefault(p => p.Id == personId);
                Assert.IsNotNull(person1);

                using (
                    var context2 = new MyEntityContext(
                        "type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName, true))
                {
                    var person2 = context2.Persons.FirstOrDefault(p => p.Id == personId);
                    Assert.IsNotNull(person2);

                    Assert.AreNotSame(person2, person1);

                    person1.Name = "bob";
                    person2.Name = "berby";

                    context1.SaveChanges();
                    context2.SaveChanges();
                }
            }
        }

        [Test]
        public void TestDeleteEntity()
        {
            var storeName = Guid.NewGuid().ToString();
            string jenId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                // create person
                var p1 = context.Persons.Create();
                p1.Name = "jen";
                context.SaveChanges();

                // retrieve object
                jenId = p1.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var jen = context.Persons.FirstOrDefault(p => p.Id == jenId);

                context.DeleteObject(jen);
                context.SaveChanges();

                jen = context.Persons.FirstOrDefault(p => p.Id == jenId);
                Assert.That(jen, Is.Null);
            }
        }

        [Test]
        public void TestDeleteEntityInSameContext()
        {
            var storeName = "DeleteEntityInSameContext_" + DateTime.Now.Ticks;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var alice = context.Persons.Create();
                alice.Name = "Alice";
                context.SaveChanges();

                string aliceId = alice.Id;

                // Delete object
                context.DeleteObject(alice);
                context.SaveChanges();

                // Object should no longer be discoverable
                Assert.That(context.Persons.FirstOrDefault(p => p.Id.Equals(aliceId)), Is.Null);
            }
        }

        [Test]
        public void TestDeletionOfEntities()
        {
            var storeName = Guid.NewGuid().ToString();
            string jenId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var p1 = context.Persons.Create();
                p1.Name = "jen";

                var skillIds = new List<string>();
                for (var i = 0; i < 5; i++)
                {
                    var skill = context.Skills.Create();
                    skill.Name = "Skill " + i;
                    if (i < 3)
                    {
                        p1.Skills.Add(skill);
                    }
                    skillIds.Add(skill.Id);
                }
                context.SaveChanges();
                jenId = p1.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var jen = context.Persons.FirstOrDefault(p => p.Id.Equals(jenId));

                Assert.IsNotNull(jen);
                Assert.AreEqual("jen", jen.Name);
                Assert.AreEqual(3, jen.Skills.Count);

                var allSkills = context.Skills;
                Assert.AreEqual(5, allSkills.Count());
                foreach (var s in allSkills)
                {
                    context.DeleteObject(s);
                }
                context.SaveChanges();
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var allSkills = context.Skills;
                Assert.AreEqual(0, allSkills.Count());

                var jen = context.Persons.FirstOrDefault(p => p.Id.Equals(jenId));

                Assert.IsNotNull(jen);
                Assert.AreEqual("jen", jen.Name);
                Assert.AreEqual(0, jen.Skills.Count, "Person still has 3 skills even after skills are deleted");

                context.DeleteObject(jen);
                context.SaveChanges();

                jen = context.Persons.FirstOrDefault(p => p.Id.Equals(jenId));
                Assert.IsNull(jen);
            }
        }

#if !PORTABLE
        [Test]
        public void TestGeneratedPropertyAttributes()
        {
            var foafPerson = typeof(FoafPerson);
            var nameProperty = foafPerson.GetProperty("Name");
            Assert.IsNotNull(nameProperty, "Could not find expected Name property on FoafPerson class");
            var generatedAttributes = nameProperty.GetCustomAttributes(false);
            Assert.AreEqual(2, generatedAttributes.Length, "Expected 2 custom attributes on Name property of generated FoafPerson class");
            Assert.IsTrue(generatedAttributes.Any(a=>a.GetType() == typeof(RequiredAttribute)), "Could not find expected Required attribute of FoafPerson.Name property");
            var customValidation = generatedAttributes.FirstOrDefault(a => a is CustomValidationAttribute) as CustomValidationAttribute;
            Assert.IsNotNull(customValidation, "Could not find expected CustomValidation attribute on FoafPerson.Name property");
            Assert.AreEqual(typeof(MyCustomValidator), customValidation.ValidatorType, "CustomValidation.ValidatorType property does not match expected value.");
            Assert.AreEqual("ValidateName", customValidation.Method, "CustomValidation.Name property does not match expected value.");
            Assert.AreEqual("Custom error message", customValidation.ErrorMessage, "CustomValidation.ErroMessage property does not match expected value.");

            var nickNameProperty = foafPerson.GetProperty("Nickname");
            Assert.IsNotNull(nickNameProperty, "Could not find expected Nickname property on FoafPerson class");
            generatedAttributes = nickNameProperty.GetCustomAttributes(false);
            Assert.AreEqual(1, generatedAttributes.Length);
            var displayName = generatedAttributes.FirstOrDefault(a => a.GetType() == typeof(DisplayNameAttribute)) as DisplayNameAttribute;
            Assert.IsNotNull(displayName, "Could not find expected DisplayName attribute of Foaf.Nickname property");
            Assert.AreEqual("Also Known As", displayName.DisplayName, "DisplayName.DisplayName property does not match expected value.");

            var dobProperty = foafPerson.GetProperty("BirthDate");
            Assert.IsNotNull(dobProperty, "Could not find expected BirthDate property on FoafPerson class");
            generatedAttributes = dobProperty.GetCustomAttributes(false);
            Assert.AreEqual(1, generatedAttributes.Length);
            var datatype = generatedAttributes.OfType<DataTypeAttribute>().FirstOrDefault();
            Assert.IsNotNull(datatype, "Could not find expected DataType attribute on Foaf.BirthDate property");
            Assert.AreEqual(DataType.Date, datatype.DataType);
        }
#endif

        [Test]
        public void TestGeneratedClassAttributes()
        {
            var foafPerson = typeof(FoafPerson);
            var generatedAttributes = foafPerson.GetCustomAttributes(false);
            Assert.AreEqual(1, generatedAttributes.Length, "Expected 1 custom attribute on the FoafPerson class");
            var displayAttribute = generatedAttributes.FirstOrDefault(a=>a.GetType() == typeof(DisplayNameAttribute)) as DisplayNameAttribute;
            Assert.IsNotNull(displayAttribute, "Could not find expected Display attribute on FoafPerson class");
            Assert.AreEqual("Person", displayAttribute.DisplayName);
        }

        [Test]
        public void TestSingleUriProperty()
        {
            var storeName = "TestSingleUriProperty_" + DateTime.Now.Ticks;
            string personId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.FoafPersons.Create();
                person.Name = "Kal Ahmed";
                person.Homepage = new Uri("http://www.techquila.com/");
                context.SaveChanges();
                personId = person.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var retrieved = context.FoafPersons.FirstOrDefault(p => p.Id.Equals(personId));
                Assert.IsNotNull(retrieved);
                Assert.AreEqual("Kal Ahmed", retrieved.Name);
                Assert.AreEqual(new Uri("http://www.techquila.com/"), retrieved.Homepage);
            }
        }

        [Test]
        public void TestUriCollectionProperty()
        {
            var storeName = "TestUriCollectionProperty_" + DateTime.Now.Ticks;
            string personId;
            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {

                var person = context.Persons.Create();
                person.Name = "Kal Ahmed";
                person.Websites.Add(new Uri("http://www.techquila.com/"));
                person.Websites.Add(new Uri("http://brightstardb.com/"));
                person.Websites.Add(new Uri("http://www.networkedplanet.com/"));
                context.SaveChanges();

                personId = person.Id;
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var retrieved = context.Persons.FirstOrDefault(p => p.Id.Equals(personId));
                Assert.IsNotNull(retrieved);
                Assert.AreEqual("Kal Ahmed", retrieved.Name);
                Assert.AreEqual(3, retrieved.Websites.Count);
                Assert.IsTrue(retrieved.Websites.Any(w => w.Equals(new Uri("http://www.techquila.com/"))));
                retrieved.Websites.Remove(new Uri("http://www.techquila.com/"));
                context.SaveChanges();
            }

            using (
                var context = new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName)
                )
            {
                var retrieved = context.Persons.FirstOrDefault(p => p.Id.Equals(personId));
                Assert.IsNotNull(retrieved);
                Assert.AreEqual("Kal Ahmed", retrieved.Name);
                Assert.AreEqual(2, retrieved.Websites.Count);
                Assert.IsFalse(retrieved.Websites.Any(w => w.Equals(new Uri("http://www.techquila.com/"))));
                Assert.IsTrue(retrieved.Websites.Contains(new Uri("http://brightstardb.com/")));
            }
        }

        [Test]
        public void TestCollectionUpdatedByInverseProperty()
        {
            var storeName = "TestCollectionUpdatedByInverseProperty_" + DateTime.Now.Ticks;
            using (var context = CreateEntityContext(storeName))
            {
                var dept = new Department {Name = "Research"};
                context.Departments.Add(dept);
                
                // Attach before property is set
                var alice = new Person {Name = "Alice"};
                context.Persons.Add(alice);
                alice.Department = dept;
                Assert.AreEqual(1, dept.Persons.Count);
                
                // Attach after property set
                var bob = new Person {Name = "Bob", Department = dept};
                context.Persons.Add(bob);
                Assert.AreEqual(2, dept.Persons.Count);

                // Attach after property set by explicit call
                var charlie = new Person { Name = "Charlie"};
                charlie.Department = dept;
                context.Persons.Add(charlie);
                Assert.AreEqual(3, dept.Persons.Count);

                // Not attached before checking inverse property
                var dave = new Person { Name = "Dave", Department = dept };
                Assert.AreEqual(3, dept.Persons.Count);
                context.Persons.Add(dave);
                Assert.AreEqual(4, dept.Persons.Count);
                
                context.SaveChanges();

                Assert.AreEqual(4, dept.Persons.Count);
                context.DeleteObject(bob);
                Assert.AreEqual(3, dept.Persons.Count);
                context.SaveChanges();

                Assert.AreEqual(3, dept.Persons.Count);
            }
        }

        [Test]
        public void TestEntitySetsHelper()
        {
            var storeName = "TestEntitySetsHelper" + DateTime.Now.Ticks;
            string pid;
            using (var context = CreateEntityContext(storeName))
            {
                var p = context.Persons.Create();
                var b = context.BaseEntities.Create();
                var d = context.DerivedEntities.Create();
                context.SaveChanges();

                // Test that we can use the returned entity set for query
                var personSet = context.EntitySet<IPerson>();
                Assert.That(personSet, Is.Not.Null);
                Assert.That(personSet.FirstOrDefault(x=>x.Id.Equals(p.Id)), Is.Not.Null);
                Assert.That(personSet.FirstOrDefault(x=>x.Id.Equals(b.Id)), Is.Null);
                Assert.That(personSet.FirstOrDefault(x => x.Id.Equals(d.Id)), Is.Null);

                var baseEntitySet = context.EntitySet<IBaseEntity>();
                Assert.That(baseEntitySet, Is.Not.Null);
                Assert.That(baseEntitySet.FirstOrDefault(x => x.Id.Equals(p.Id)), Is.Null);
                Assert.That(baseEntitySet.FirstOrDefault(x => x.Id.Equals(b.Id)), Is.Not.Null);
                Assert.That(baseEntitySet.FirstOrDefault(x => x.Id.Equals(d.Id)), Is.Not.Null);

                var derivedEntitySet = context.EntitySet<IDerivedEntity>();
                Assert.That(derivedEntitySet, Is.Not.Null);
                Assert.That(derivedEntitySet.FirstOrDefault(x => x.Id.Equals(p.Id)), Is.Null);
                Assert.That(derivedEntitySet.FirstOrDefault(x => x.Id.Equals(b.Id)), Is.Null);
                Assert.That(derivedEntitySet.FirstOrDefault(x => x.Id.Equals(d.Id)), Is.Not.Null);

                // Test that we can use the returned entity set for update
                var p2 = context.EntitySet<IPerson>().Create();
                p2.Name = "Bob";
                context.SaveChanges();
                pid = p2.Id;
            }
            using (var context = CreateEntityContext(storeName))
            {
                var bob  = context.EntitySet<IPerson>().FirstOrDefault(x => x.Id.Equals(pid));
                Assert.That(bob, Is.Not.Null);
                Assert.That(bob.Name, Is.EqualTo("Bob"));
            }
        }

        [Test]
        public void TestRetrieveUnsetId()
        {
            MyEntityContext.InitializeEntityMappingStore();
            var entity = new Person();
            var id = entity.Id;
            Assert.That(id, Is.EqualTo(null));
        }

        [Test]
        public void TestRepositoryPattern()
        {
            var storeName = "TestRepositoryPattern" + DateTime.Now.Ticks;
            string id;
            using (var context = CreateEntityContext(storeName))
            {
                var uow = new UnitOfWork(context);
                var repo = new Repository<IDerivedEntity>(uow);
                var derived = repo.Create();
                derived.BaseStringValue = "Party!";
                derived.DateTimeProperty= new DateTime(1999, 12, 31, 23, 58, 00);
                context.SaveChanges();
                id = derived.Id;
            }

            using (var context = CreateEntityContext(storeName))
            {
                var uow = new UnitOfWork(context);
                var repo = new Repository<IDerivedEntity>(uow);
                var derived = repo.GetById(id);
                Assert.That(derived, Is.Not.Null);
                Assert.That(derived.Id, Is.EqualTo(id));
                Assert.That(derived.BaseStringValue, Is.EqualTo("Party!"));
                Assert.That(derived.DateTimeProperty, Is.EqualTo(new DateTime(1999, 12, 31, 23, 58, 00)));
            }
        }

        [Test]
        public void TestRetrieveEntityWithSpaceInId()
        {
            var storeName = "TestRetrieveEntityWithSpaceInId_" + DateTime.Now.Ticks;
            using (var context = CreateEntityContext(storeName))
            {
                var entity = new Entity {Id = "some entity", SomeString = "Some Entity"};
                context.Entities.Add(entity);
                context.SaveChanges();
            }

            using (var context = CreateEntityContext(storeName))
            {
                var entity = context.Entities.FirstOrDefault(x => x.Id.Equals("some entity"));
                Assert.That(entity, Is.Not.Null);
            }
        }

        MyEntityContext CreateEntityContext(string storeName)
        {
            return new MyEntityContext("type=embedded;storesdirectory=c:\\brightstar;storename=" + storeName);
        }
    }

    public class UnitOfWork
    {
        public MyEntityContext Context { get; private set; }

        public UnitOfWork(MyEntityContext context)
        {
            Context = context;
        }

        public void Save()
        {
            Context.SaveChanges();
        }
    }

    public class Repository<T> where T : class, IBaseEntity
    {
        private readonly UnitOfWork _unitOfWork;

        public Repository(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public T Create()
        {
            return _unitOfWork.Context.EntitySet<T>().Create();
        }

       
        public T GetById(string id)
        {
            return _unitOfWork.Context.EntitySet<T>().FirstOrDefault(x => x.Id == id);
        }
    }
}
