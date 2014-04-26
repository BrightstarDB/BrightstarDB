using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class GeneratedKeyTests
    {
        private readonly string _storeName;

        public GeneratedKeyTests()
        {
            _storeName = "GeneratedKeyTests_" + DateTime.Now.Ticks;
        }

        private MyEntityContext GetContext()
        {
            return new MyEntityContext("type=embedded;storesDirectory=c:\\brightstar;storeName=" + _storeName);
        }

        [Test]
        public void TestCreateStringKeyEntity()
        {
            using (var context = GetContext())
            {
                var entity = new StringKeyEntity {Name = "Entity1", Description = "This is Entity 1"};
                context.StringKeyEntities.Add(entity);
                context.SaveChanges();
            }
            using (var context = GetContext())
            {
                var entity = context.StringKeyEntities.FirstOrDefault(x => x.Id.Equals("Entity1"));
                Assert.That(entity, Is.Not.Null);
                Assert.That(entity.Name, Is.EqualTo("Entity1"));
                Assert.That(entity.Description, Is.EqualTo("This is Entity 1"));
            }
        }

        [Test]
        public void TestCreateStringKeyEntity2()
        {
            using (var context = GetContext())
            {
                var entity = context.StringKeyEntities.Create();
                entity.Name = "Entity2";
                entity.Description = "This is Entity 2";
                context.SaveChanges();
            }
            using (var context = GetContext())
            {
                var entity = context.StringKeyEntities.FirstOrDefault(x => x.Id.Equals("Entity2"));
                Assert.That(entity, Is.Not.Null);
                Assert.That(entity.Name, Is.EqualTo("Entity2"));
                Assert.That(entity.Description, Is.EqualTo("This is Entity 2"));
            }
        }
    }
}
