using System;
using System.Linq;
using BrightstarDB.EntityFramework;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class InterfaceInheritanceTests
    {
        private const string ConnectionString = "type=embedded;storesDirectory=c:\\brightstar\\;storeName=";

        private static string MakeStoreName(string suffix)
        {
            var dt = DateTime.Now;
            return String.Format("{0:02d}{1:02d}{2:02d}_{3}", dt.Hour, dt.Minute, dt.Second, suffix);
        }

        [Test]
        public void TestRetrieveDerivedInstancesFromBaseCollection()
        {
            var storeName = MakeStoreName("retrieveDerviedInstances");
            var context = new MyEntityContext(ConnectionString + storeName);
            var derivedEntity = context.DerivedEntities.Create();
            derivedEntity.BaseStringValue = "This is a dervied entity";
            derivedEntity.DateTimeProperty = new DateTime(2011, 11, 11);
            var baseEntity = context.BaseEntities.Create();
            baseEntity.BaseStringValue = "This is a base entity";
            context.SaveChanges();

            context = new MyEntityContext(ConnectionString+storeName);

            var baseEntities = context.BaseEntities.ToList();
            Assert.AreEqual(2,baseEntities.Count);
            Assert.IsTrue(baseEntities.Any(x=>x.BaseStringValue.Equals("This is a base entity")));
            Assert.IsTrue(baseEntities.Any(x=>x.BaseStringValue.Equals("This is a dervied entity")));

            var derivedEntities = context.DerivedEntities.ToList();
            Assert.AreEqual(1, derivedEntities.Count);
            Assert.IsTrue(derivedEntities.Any(x=>x.BaseStringValue.Equals("This is a dervied entity")));
        }

        [Test]
        public void TestUseDerivedInstanceInBaseClassCollectionProperty()
        {
            var storeName = MakeStoreName("useDerivedInstance");
            var context = new MyEntityContext(ConnectionString + storeName);
            var entity1 = context.DerivedEntities.Create();
            entity1.BaseStringValue = "Entity1";
            var entity2 = context.DerivedEntities.Create();
            entity2.BaseStringValue = "Entity2";
            var entity3 = context.BaseEntities.Create();
            entity3.BaseStringValue = "Entity3";
            entity1.RelatedEntities.Add(entity2);
            entity1.RelatedEntities.Add(entity3);
            context.SaveChanges();

            context=new MyEntityContext(ConnectionString + storeName);
            var baseEntities = context.BaseEntities.ToList();
            Assert.AreEqual(3, baseEntities.Count);
            var derivedEntities = context.DerivedEntities.ToList();
            Assert.AreEqual(2, derivedEntities.Count);
            entity1 = context.DerivedEntities.Where(x => x.BaseStringValue.Equals("Entity1")).FirstOrDefault();
            Assert.IsNotNull(entity1);
            Assert.AreEqual(2, entity1.RelatedEntities.Count);
            Assert.IsTrue(entity1.RelatedEntities.Any(x=>x.BaseStringValue.Equals("Entity2")));
            Assert.IsTrue(entity1.RelatedEntities.Any(x=>x.BaseStringValue.Equals("Entity3")));

        }

        [Test]
        public void TestBecomeAndUnbecome()
        {
            var storeName = MakeStoreName("becomeAndUnbecome");
            var context = new MyEntityContext(ConnectionString + storeName);
            var entity1 = context.BaseEntities.Create();
            entity1.BaseStringValue = "BecomeTest";
            context.SaveChanges();

            context = new MyEntityContext(ConnectionString + storeName);
            Assert.AreEqual(1, context.BaseEntities.Count());
            Assert.AreEqual(0, context.DerivedEntities.Count());
            var entity =
                context.BaseEntities.Where(x => x.BaseStringValue.Equals("BecomeTest")).FirstOrDefault();
            var derived = (entity as BrightstarEntityObject).Become<IDerivedEntity>();
            derived.DateTimeProperty = new DateTime(2011, 11,11);
            context.SaveChanges();

            context = new MyEntityContext(ConnectionString + storeName);
            Assert.AreEqual(1, context.BaseEntities.Count());
            Assert.AreEqual(1, context.DerivedEntities.Count());
            entity =
                context.BaseEntities.Where(x => x.BaseStringValue.Equals("BecomeTest")).FirstOrDefault();
            Assert.AreEqual("BecomeTest", entity.BaseStringValue);
            var derivedEntity = (entity as BrightstarEntityObject).Become<IDerivedEntity>();
            Assert.AreEqual("BecomeTest", derivedEntity.BaseStringValue);
            Assert.AreEqual(new DateTime(2011, 11, 11), derivedEntity.DateTimeProperty);

            context.SaveChanges();

            context = new MyEntityContext(ConnectionString + storeName);
            var d2 = context.DerivedEntities.Where(x => x.BaseStringValue.Equals("BecomeTest")).FirstOrDefault();
            Assert.IsNotNull(d2);
            (d2 as BrightstarEntityObject).Unbecome<IDerivedEntity>();
            context.SaveChanges();

            context = new MyEntityContext(ConnectionString + storeName);
            Assert.AreEqual(1, context.BaseEntities.Count());
            Assert.AreEqual(0, context.DerivedEntities.Count());

        }
    }
}
