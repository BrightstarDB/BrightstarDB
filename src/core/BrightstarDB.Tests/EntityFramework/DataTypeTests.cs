using System;
using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class DataTypeTests
    {
        private MyEntityContext _myEntityContext;

        private readonly string _connectionString = "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=" + Guid.NewGuid();

        [OneTimeSetUp]
        public void SetUp()
        {
            _myEntityContext = new MyEntityContext(_connectionString);
        }

        [Test]
        public void TestCreateAndSetProperties()
        {
            var entity = _myEntityContext.TestEntities.Create();
            var now = DateTime.Now;
            entity.SomeDateTime = now;
            entity.SomeDecimal = 3.14m;
            entity.SomeDouble = 3.14;
            entity.SomeFloat = 3.14F;
            entity.SomeInt = 3;
            entity.SomeNullableDateTime = null;
            entity.SomeNullableInt = null;
            entity.SomeString = "test entity";

            entity.SomeBool = true;
            entity.SomeLong = 50L;

            _myEntityContext.SaveChanges();
            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            var checkEntity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));

            Assert.IsNotNull(checkEntity);
            Assert.IsNotNull(checkEntity.SomeDateTime);
            Assert.IsNotNull(checkEntity.SomeDecimal);
            Assert.IsNotNull(checkEntity.SomeDouble);
            Assert.IsNotNull(checkEntity.SomeFloat);
            Assert.IsNotNull(checkEntity.SomeInt);
            Assert.IsNull(checkEntity.SomeNullableDateTime);
            Assert.IsNull(checkEntity.SomeNullableInt);
            Assert.IsNotNull(checkEntity.SomeString);

            Assert.AreEqual(now, checkEntity.SomeDateTime);
            Assert.AreEqual(3.14m, checkEntity.SomeDecimal);
            Assert.AreEqual(3.14, checkEntity.SomeDouble);
            Assert.AreEqual(3.14F, checkEntity.SomeFloat);
            Assert.AreEqual(3, checkEntity.SomeInt);
            Assert.AreEqual("test entity", checkEntity.SomeString);
            Assert.IsTrue(checkEntity.SomeBool);
            Assert.AreEqual(50L, checkEntity.SomeLong);          
        }

        [Test]
        public void TestIssue128CannotSetFloatValueBelow1()
        {
            // Create an entity
            var entity = _myEntityContext.TestEntities.Create();
            // Set the properties that allow fractional values to values < 1.0
            entity.SomeDecimal = 0.14m;
            entity.SomeDouble = 0.14;
            entity.SomeFloat = 0.14F;
            // Persist the changes
            _myEntityContext.SaveChanges();
            var entityId = entity.Id;

            // Create a new context connection so that we don't get a locally cached value from the context
            var newContext = new MyEntityContext(_connectionString);
            // Retrieve the previously created entity
            var checkEntity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));

            // Assert that the entity was found and the values we set are set to the values we originally provided
            Assert.IsNotNull(checkEntity);
            Assert.IsNotNull(checkEntity.SomeDecimal);
            Assert.IsNotNull(checkEntity.SomeDouble);
            Assert.IsNotNull(checkEntity.SomeFloat);
            Assert.AreEqual(0.14m, checkEntity.SomeDecimal);
            Assert.AreEqual(0.14, checkEntity.SomeDouble);
            Assert.AreEqual(0.14F, checkEntity.SomeFloat);
        }

        [Test]
        public void TestCreateAndSetCollections()
        {
            var entity = _myEntityContext.TestEntities.Create();

            var now = DateTime.Now;
            for(var i = 0; i<10;i++)
            {
                var date = now.AddDays(i);    
                entity.CollectionOfDateTimes.Add(date);
            }
            for (var i = 0; i < 10; i++)
            {
                var dec = i + .5m;
                entity.CollectionOfDecimals.Add(dec);
            }
            for (var i = 0; i < 10; i++)
            {
                var dbl = i + .5;
                entity.CollectionOfDoubles.Add(dbl);
            }
            for (var i = 0; i < 10; i++)
            {
                var flt = i + .5F;
                entity.CollectionOfFloats.Add(flt);
            }
            for (var i = 0; i < 10; i++)
            {
                entity.CollectionOfInts.Add(i);
            }
            entity.CollectionOfBools.Add(true);
            entity.CollectionOfBools.Add(false);
            for (var i = 0; i < 10; i++)
            {
                var l = i*100;
                entity.CollectionOfLong.Add(l);
            }
            for (var i = 0; i < 10; i++)
            {
                var s = "word" + i;
                entity.CollectionOfStrings.Add(s);
            }

            _myEntityContext.SaveChanges();
            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            var checkEntity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));

            Assert.IsNotNull(checkEntity);
            Assert.IsNotNull(checkEntity.CollectionOfDateTimes);
            Assert.IsNotNull(checkEntity.CollectionOfDecimals);
            Assert.IsNotNull(checkEntity.CollectionOfDoubles);
            Assert.IsNotNull(checkEntity.CollectionOfFloats);
            Assert.IsNotNull(checkEntity.CollectionOfInts);
            Assert.IsNotNull(checkEntity.CollectionOfBools);
            Assert.IsNotNull(checkEntity.CollectionOfLong);
            Assert.IsNotNull(checkEntity.CollectionOfStrings);

            var lstDateTimes = checkEntity.CollectionOfDateTimes.OrderBy(e => e).ToList();
            var lstDecs = checkEntity.CollectionOfDecimals.OrderBy(e => e).ToList();
            var lstDbls = checkEntity.CollectionOfDoubles.OrderBy(e => e).ToList();
            var lstFloats = checkEntity.CollectionOfFloats.OrderBy(e => e).ToList();
            var lstInts = checkEntity.CollectionOfInts.OrderBy(e => e).ToList();
            var lstLongs = checkEntity.CollectionOfLong.OrderBy(e => e).ToList();
            var lstStrings = checkEntity.CollectionOfStrings.OrderBy(e => e).ToList();
            var lstBools = checkEntity.CollectionOfBools.OrderBy(e => e).ToList();
            for (var i = 0; i < 10; i++)
            {
                var date = now.AddDays(i);
                var dec = i + .5m;
                var dbl = i + .5;
                var flt = i + .5F;
                var l = i * 100;
                var s = "word" + i;

                Assert.AreEqual(date, lstDateTimes[i]);
                Assert.AreEqual(dec, lstDecs[i]);
                Assert.AreEqual(dbl, lstDbls[i]);
                Assert.AreEqual(flt, lstFloats[i]);
                Assert.AreEqual(l, lstLongs[i]);
                Assert.AreEqual(i, lstInts[i]);
                Assert.AreEqual(s, lstStrings[i]);
            }
            Assert.AreEqual(2, lstBools.Count);
            
        }

        [Test]
        public void TestSetByte()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeByte = 255;
            entity.AnotherByte = 128;
            entity.NullableByte = null;
            entity.AnotherNullableByte = null;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.IsNotNull(entity);
            Assert.IsNotNull(entity.SomeByte);
            Assert.IsNotNull(entity.AnotherByte);

            Assert.AreEqual(255, entity.SomeByte);
            Assert.AreEqual(128, entity.AnotherByte);

            Assert.IsNull(entity.NullableByte);
            Assert.IsNull(entity.AnotherNullableByte);
        }

        [Test]
        public void TestSetChar()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeChar = 'C';
            entity.AnotherChar = 'c';
            entity.NullableChar = null;
            entity.AnotherNullableChar = null;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.IsNotNull(entity);
            Assert.IsNotNull(entity.SomeChar);
            Assert.IsNotNull(entity.AnotherChar);
            Assert.IsNull(entity.NullableChar);
            Assert.IsNull(entity.AnotherNullableChar);
            
            Assert.AreEqual('C', entity.SomeChar);
            Assert.AreEqual('c', entity.AnotherChar);

            Assert.AreNotEqual('c', entity.SomeChar);
            Assert.AreNotEqual('C', entity.AnotherChar);

        }

        [Test]
        public void TestSetSbyte()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeSByte = 127;
            entity.AnotherSByte = 64;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.IsNotNull(entity);
            Assert.IsNotNull(entity.SomeSByte);
            Assert.IsNotNull(entity.AnotherSByte);
            
            Assert.AreEqual(127, entity.SomeSByte);
            Assert.AreEqual(64, entity.AnotherSByte);

            Assert.AreNotEqual(64, entity.SomeSByte);
            Assert.AreNotEqual(127, entity.AnotherSByte);

        }

        [Test]
        public void TestSetShort()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeShort = 32767;
            entity.AnotherShort = -32768;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.IsNotNull(entity);
            Assert.IsNotNull(entity.SomeShort);
            Assert.IsNotNull(entity.AnotherShort);

            Assert.AreEqual(32767, entity.SomeShort);
            Assert.AreEqual(-32768, entity.AnotherShort);

            Assert.AreNotEqual(-32768, entity.SomeShort);
            Assert.AreNotEqual(32767, entity.AnotherShort);
        }

        [Test]
        public void TestSetUint()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeUInt = 4294967295;
            entity.AnotherUInt = 12;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.IsNotNull(entity);
            Assert.IsNotNull(entity.SomeUInt);
            Assert.IsNotNull(entity.AnotherUInt);

            Assert.AreEqual(4294967295U, entity.SomeUInt);
            Assert.AreEqual(12U, entity.AnotherUInt);

            Assert.AreNotEqual(12U, entity.SomeUInt);
            Assert.AreNotEqual(4294967295U, entity.AnotherUInt);
        }

        [Test]
        public void TestSetUlong()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeULong = 18446744073709551615;
            entity.AnotherULong = 52;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.IsNotNull(entity);
            Assert.IsNotNull(entity.SomeULong);
            Assert.IsNotNull(entity.AnotherULong);

            Assert.AreEqual(18446744073709551615, entity.SomeULong);
            Assert.AreEqual(52UL, entity.AnotherULong);

            Assert.AreNotEqual(52UL, entity.SomeULong);
            Assert.AreNotEqual(18446744073709551615, entity.AnotherULong);
        }

        [Test]
        public void TestSetUShort()
        {
            var entity = _myEntityContext.TestEntities.Create();
            entity.SomeUShort = 65535;
            entity.AnotherUShort = 52;
            _myEntityContext.SaveChanges();

            var entityId = entity.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entityId));
            Assert.IsNotNull(entity);
            Assert.IsNotNull(entity.SomeUShort);
            Assert.IsNotNull(entity.AnotherUShort);

            Assert.AreEqual(65535, entity.SomeUShort);
            Assert.AreEqual(52, entity.AnotherUShort);

            Assert.AreNotEqual(52, entity.SomeUShort);
            Assert.AreNotEqual(65535, entity.AnotherUShort);
        }

        [Test]
        public void TestEnums()
        {
            var entity1 = _myEntityContext.TestEntities.Create();
            entity1.SomeEnumeration = TestEnumeration.Second;
            entity1.SomeNullableEnumeration = TestEnumeration.Third;
            entity1.SomeFlagsEnumeration = TestFlagsEnumeration.FlagA | TestFlagsEnumeration.FlagB;
            entity1.SomeNullableFlagsEnumeration = TestFlagsEnumeration.FlagB | TestFlagsEnumeration.FlagC;
            entity1.SomeSystemEnumeration = DayOfWeek.Friday;
            entity1.SomeNullableSystemEnumeration = DayOfWeek.Friday;
            var entity2 = _myEntityContext.TestEntities.Create();
            _myEntityContext.SaveChanges();

            var entity1Id = entity1.Id;
            var entity2Id = entity2.Id;

            var newContext = new MyEntityContext(_connectionString);
            entity1 = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entity1Id));
            entity2 = newContext.TestEntities.FirstOrDefault(e => e.Id.Equals(entity2Id));
            Assert.IsNotNull(entity1);
            Assert.IsNotNull(entity2);
            Assert.AreEqual(TestEnumeration.Second, entity1.SomeEnumeration);
            Assert.AreEqual(TestEnumeration.Third, entity1.SomeNullableEnumeration);
            Assert.AreEqual(TestFlagsEnumeration.FlagB|TestFlagsEnumeration.FlagA, entity1.SomeFlagsEnumeration);
            Assert.AreEqual(TestFlagsEnumeration.FlagC|TestFlagsEnumeration.FlagB, entity1.SomeNullableFlagsEnumeration);
            Assert.AreEqual(DayOfWeek.Friday, entity1.SomeSystemEnumeration);
            Assert.AreEqual(DayOfWeek.Friday, entity1.SomeNullableSystemEnumeration);

            Assert.AreEqual(TestEnumeration.First, entity2.SomeEnumeration);
            Assert.IsNull(entity2.SomeNullableEnumeration);
            Assert.AreEqual(TestFlagsEnumeration.NoFlags, entity2.SomeFlagsEnumeration);
            Assert.IsNull(entity2.SomeNullableFlagsEnumeration);
            Assert.AreEqual(DayOfWeek.Sunday, entity2.SomeSystemEnumeration);
            Assert.IsNull(entity2.SomeNullableSystemEnumeration);
        }
        //note Test for SetByteArray and SetEnumeration are in SimpleContextTests.cs

    }
}
