using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class NoIdPropertyTests
    {
        private readonly string _storeName;

        public NoIdPropertyTests()
        {
            _storeName = "NoIdPropertyTests_" + DateTime.Now.Ticks;
        }

        private MyEntityContext GetContext()
        {
            return new MyEntityContext("type=embedded;storesDirectory=c:\\brightstar;storeName=" + _storeName);
        }

        [Test]
        public void TestCreateEntity()
        {
            using (var context = GetContext())
            {
                var newEntity = new NoId {Name = "TestCreate"};
                context.NoIds.Add(newEntity);
                context.SaveChanges();
            }
            using (var context = GetContext())
            {
                var existingEntity = context.NoIds.FirstOrDefault(x => x.Name.Equals("TestCreate"));
                Assert.That(existingEntity, Is.Not.Null);
            }
        }

        [Test]
        public void TestPopulateEntityProperty()
        {
            using (var context = GetContext())
            {
                var fred = new Person{Name = "Fred"};
                var newEntity = new NoId {Name = "TestPopulateEntityProperty", Owner = fred};
                context.NoIds.Add(newEntity);
                context.Persons.Add(fred);
                context.SaveChanges();
            }
            using (var context = GetContext())
            {
                var existingEntity = context.NoIds.FirstOrDefault(x => x.Name.Equals("TestPopulateEntityProperty"));
                Assert.That(existingEntity, Is.Not.Null);
                Assert.That(existingEntity.Owner, Is.Not.Null);
                Assert.That(existingEntity.Owner.Name, Is.EqualTo("Fred"));
            }
        }
    }
}
