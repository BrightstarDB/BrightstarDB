﻿using System;
using BrightstarDB.EntityFramework;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture("type=embedded;storesDirectory=c:\\brightstar;storeName={0}")]
    // TODO: Reinstate REST client tests when REST server is reinstated
    // [TestFixture("type=rest;endpoint=http://localhost:8090/brightstar/;storeName={0}")]
    public class UniqueKeyConstraintTests : ClientTestBase
    {
        private readonly string _connectionString;
        private readonly string _storeName;

        public UniqueKeyConstraintTests(string connectionStringTemplate)
        {
            _storeName = "UniqueKeyConstraintTests_" + DateTime.Now.Ticks;
            _connectionString = String.Format(connectionStringTemplate, _storeName);
        }

        private MyEntityContext GetContext()
        {
            return new MyEntityContext(_connectionString);
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            if (_connectionString.Contains("type=rest"))
            {
                StartService();
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (_connectionString.Contains("type=rest"))
            {
                CloseService();
            }
        }

        [Test]
        public void TestUniqueConstraintViolationThrownWhenKeyChanges()
        {
            using (var context = GetContext())
            {
                var entity1 = context.StringKeyEntities.Create();
                entity1.Name = "A";
                var entity2 = context.StringKeyEntities.Create();
                Assert.Throws<UniqueConstraintViolationException>(() => entity2.Name = "A");
            }
        }

        [Test]
        public void TestUniqueConstraintViolationThrownWhenSavingChanges()
        {
            using (var context = GetContext())
            {
                var entity1 = context.StringKeyEntities.Create();
                entity1.Name = "B";
                context.SaveChanges();
            }
            
            using (var context = GetContext())
            {
                var entity2 = context.StringKeyEntities.Create();
                entity2.Name = "B";
                Assert.Throws<UniqueConstraintViolationException>(()=>context.SaveChanges());
            }
        }
    }
}
