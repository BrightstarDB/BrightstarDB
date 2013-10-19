using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.IntegrationTests.Context;
using NUnit.Framework;

namespace BrightstarDB.Server.IntegrationTests
{
    [TestFixture]
    public class OptimisticLockingTests : ClientTestBase
    {
        private readonly string _storeName = "HttpOptimisticLockingTests_" + DateTime.Now.Ticks;

        [TestFixtureSetUp]
        public void SetUp()
        {
            StartService();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            CloseService();
        }

        protected MyEntityContext NewContext()
        {
            return new MyEntityContext(
                String.Format("type=rest;endpoint=http://localhost:8090/brightstar;storeName={0};optimisticLocking=true", _storeName));
        }



        #region Test Single Object Refresh
        [Test]
        public void TestSimplePropertyRefreshWithClientWins()
        {
            using (var context1 = NewContext())
            {
                var alice1 = context1.Persons.Create();
                alice1.Age = 21;
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                    Assert.IsNotNull(alice2);
                    Assert.AreEqual(21, alice2.Age);
                    alice2.Age = 22;
                    context2.SaveChanges();
                }

                alice1.Age = 20;
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected a TransactionPreconditionsFailedException");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.ClientWins, alice1);
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var alice3 = context3.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                        Assert.IsNotNull(alice3);
                        Assert.AreEqual(20, alice3.Age);
                    }
                }
            }
        }

        [Test]
        public void TestSimplePropertyRefreshWithStoreWins()
        {
            using (var context1 = NewContext())
            {
                var alice1 = context1.Persons.Create();
                alice1.Age = 21;
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                    Assert.IsNotNull(alice2);
                    Assert.AreEqual(21, alice2.Age);
                    alice2.Age = 22;
                    context2.SaveChanges();
                }

                alice1.Age = 20;
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected a TransactionPreconditionsFailedException");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.StoreWins, alice1);
                    Assert.AreEqual(22, alice1.Age);
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var alice3 = context3.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                        Assert.IsNotNull(alice3);
                        Assert.AreEqual(22, alice3.Age);
                    }
                }
            }
        }

        [Test]
        public void TestRelatedObjectRefreshWithClientWins()
        {
            using (var context1 = NewContext())
            {
                var alice1 = context1.Persons.Create();
                alice1.Name = "Alice";
                var skill1 = context1.Skills.Create();
                skill1.Name = "Programming";
                alice1.MainSkill = skill1;
                var webDesign1 = context1.Skills.Create();
                webDesign1.Name = "Web Design";
                var projMgt1 = context1.Skills.Create();
                projMgt1.Name = "Project Management";
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                    var webDesign2 = context2.Skills.FirstOrDefault(s => s.Id.Equals(webDesign1.Id));
                    Assert.IsNotNull(alice2);
                    Assert.IsNotNull(webDesign2);
                    alice2.MainSkill = webDesign2;
                    context2.SaveChanges();
                }
                alice1.MainSkill = projMgt1;
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected a TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.ClientWins, alice1);
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var alice3 = context3.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                        Assert.IsNotNull(alice3);
                        Assert.IsNotNull(alice3.MainSkill);
                        Assert.AreEqual("Project Management", alice3.MainSkill.Name);
                    }
                }
            }
        }

        [Test]
        public void TestRelatedObjectRefreshWithStoreWins()
        {
            using (var context1 = NewContext())
            {
                var alice1 = context1.Persons.Create();
                alice1.Name = "Alice";
                var skill1 = context1.Skills.Create();
                skill1.Name = "Programming";
                alice1.MainSkill = skill1;
                var webDesign1 = context1.Skills.Create();
                webDesign1.Name = "Web Design";
                var projMgt1 = context1.Skills.Create();
                projMgt1.Name = "Project Management";
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                    var webDesign2 = context2.Skills.FirstOrDefault(s => s.Id.Equals(webDesign1.Id));
                    Assert.IsNotNull(alice2);
                    Assert.IsNotNull(webDesign2);
                    alice2.MainSkill = webDesign2;
                    context2.SaveChanges();
                }

                alice1.MainSkill = projMgt1;
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected a TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.StoreWins, alice1);
                    Assert.AreEqual("Web Design", alice1.MainSkill.Name);
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var alice3 = context3.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                        Assert.IsNotNull(alice3);
                        Assert.IsNotNull(alice3.MainSkill);
                        Assert.AreEqual("Web Design", alice3.MainSkill.Name);
                    }
                }
            }
        }

        [Test]
        public void TestLiteralCollectionRefreshWithClientWins()
        {
            using (var context1 = NewContext())
            {
                var person1 = context1.FoafPersons.Create();
                person1.MboxSums = new List<string> {"sum1", "sum2"};
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var person2 = context2.FoafPersons.FirstOrDefault(p => p.Id.Equals(person1.Id));
                    Assert.IsNotNull(person2);
                    person2.MboxSums.Remove("sum1");
                    person2.MboxSums.Add("sum3");
                    context2.SaveChanges();
                }

                person1.MboxSums.Remove("sum1");
                person1.MboxSums.Add("sum4");
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected a TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.ClientWins, person1);
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var person3 = context3.FoafPersons.FirstOrDefault(p => p.Id.Equals(person1.Id));
                        Assert.IsNotNull(person3);
                        Assert.AreEqual(2, person3.MboxSums.Count);
                        Assert.IsTrue(person3.MboxSums.Contains("sum2"));
                        Assert.IsTrue(person3.MboxSums.Contains("sum4"));
                    }
                }
            }
        }

        [Test]
        public void TestLiteralCollectionRefreshWithStoreWins()
        {
            using (var context1 = NewContext())
            {
                var person1 = context1.FoafPersons.Create();
                person1.MboxSums = new List<string> {"sum1", "sum2"};
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var person2 = context2.FoafPersons.FirstOrDefault(p => p.Id.Equals(person1.Id));
                    Assert.IsNotNull(person2);
                    person2.MboxSums.Remove("sum1");
                    person2.MboxSums.Add("sum3");
                    context2.SaveChanges();
                }

                person1.MboxSums.Remove("sum1");
                person1.MboxSums.Add("sum4");
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected a TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.StoreWins, person1);
                    Assert.AreEqual(2, person1.MboxSums.Count);
                    Assert.IsTrue(person1.MboxSums.Contains("sum2"));
                    Assert.IsTrue(person1.MboxSums.Contains("sum3"));
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var person3 = context3.FoafPersons.FirstOrDefault(p => p.Id.Equals(person1.Id));
                        Assert.IsNotNull(person3);
                        Assert.AreEqual(2, person3.MboxSums.Count);
                        Assert.IsTrue(person3.MboxSums.Contains("sum2"));
                        Assert.IsTrue(person3.MboxSums.Contains("sum3"));
                    }
                }
            }
        }

        [Test]
        public void TestObjectCollectionRefreshWithClientWins()
        {
            using (var context1 = NewContext())
            {
                var alice1 = context1.Persons.Create();
                alice1.Name = "Alice";
                var programming1 = context1.Skills.Create();
                programming1.Name = "Programming";
                var webDesign1 = context1.Skills.Create();
                webDesign1.Name = "Web Design";
                alice1.Skills = new List<ISkill> {programming1, webDesign1};
                var projMgt1 = context1.Skills.Create();
                projMgt1.Name = "Project Management";
                var scrum1 = context1.Skills.Create();
                scrum1.Name = "SCRUM";
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                    var webDesign2 = context2.Skills.FirstOrDefault(s => s.Id.Equals(webDesign1.Id));
                    var projMgt2 = context2.Skills.FirstOrDefault(s => s.Id.Equals(projMgt1.Id));
                    Assert.IsNotNull(alice2);
                    Assert.IsNotNull(webDesign2);
                    Assert.IsNotNull(projMgt2);
                    alice2.Skills.Remove(webDesign2);
                    alice2.Skills.Add(projMgt2);
                    context2.SaveChanges();
                }
                alice1.Skills.Remove(webDesign1);
                alice1.Skills.Add(scrum1);
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected a TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.ClientWins, alice1);
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var alice3 = context3.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                        Assert.IsNotNull(alice3);
                        Assert.AreEqual(2, alice3.Skills.Count);
                        Assert.IsTrue(alice3.Skills.Any(s => s.Id.Equals(programming1.Id)),
                                      "Could not find Programming skill after save");
                        Assert.IsTrue(alice3.Skills.Any(s => s.Id.Equals(scrum1.Id)),
                                      "Could not find SCRUM skill after save");
                    }
                }
            }
        }

        [Test]
        public void TestObjectCollectionRefreshWithStoreWins()
        {
            using (var context1 = NewContext())
            {
                var alice1 = context1.Persons.Create();
                alice1.Name = "Alice";
                var programming1 = context1.Skills.Create();
                programming1.Name = "Programming";
                var webDesign1 = context1.Skills.Create();
                webDesign1.Name = "Web Design";
                alice1.Skills = new List<ISkill> {programming1, webDesign1};
                var projMgt1 = context1.Skills.Create();
                projMgt1.Name = "Project Management";
                var scrum1 = context1.Skills.Create();
                scrum1.Name = "SCRUM";
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                    var webDesign2 = context2.Skills.FirstOrDefault(s => s.Id.Equals(webDesign1.Id));
                    var projMgt2 = context2.Skills.FirstOrDefault(s => s.Id.Equals(projMgt1.Id));
                    Assert.IsNotNull(alice2);
                    Assert.IsNotNull(webDesign2);
                    Assert.IsNotNull(projMgt2);
                    alice2.Skills.Remove(webDesign2);
                    alice2.Skills.Add(projMgt2);
                    context2.SaveChanges();
                }

                alice1.Skills.Remove(webDesign1);
                alice1.Skills.Add(scrum1);
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected a TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.StoreWins, alice1);
                    Assert.AreEqual(2, alice1.Skills.Count);
                    Assert.IsTrue(alice1.Skills.Any(s => s.Id.Equals(programming1.Id)));
                    Assert.IsTrue(alice1.Skills.Any(s => s.Id.Equals(projMgt1.Id)));
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var alice3 = context3.Persons.FirstOrDefault(p => p.Id.Equals(alice1.Id));
                        Assert.IsNotNull(alice3);
                        Assert.AreEqual(2, alice3.Skills.Count);
                        Assert.IsTrue(alice3.Skills.Any(s => s.Id.Equals(programming1.Id)),
                                      "Could not find Programming skill after save");
                        Assert.IsTrue(alice3.Skills.Any(s => s.Id.Equals(projMgt1.Id)),
                                      "Could not find Project Management skill after save");
                    }
                }
            }
        }
        #endregion

        #region Multiple Object Refresh
        [Test]
        public void MultiLiteralPropertyRefreshClientWins()
        {
            using (var context1 = NewContext())
            {
                var alice = context1.Persons.Create();
                var bob = context1.Persons.Create();
                alice.Age = 21;
                bob.Age = 40;
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(x => x.Id.Equals(alice.Id));
                    var bob2 = context2.Persons.FirstOrDefault(x => x.Id.Equals(bob.Id));
                    Assert.IsNotNull(alice2);
                    Assert.IsNotNull(bob2);
                    alice2.Age = 22;
                    bob2.Age = 41;
                    context2.SaveChanges();
                }

                alice.Age = 20;
                bob.Age = 39;
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.ClientWins, new[] {alice, bob});
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var alice3 = context3.Persons.FirstOrDefault(x => x.Id.Equals(alice.Id));
                        var bob3 = context3.Persons.FirstOrDefault(x => x.Id.Equals(bob.Id));
                        Assert.IsNotNull(alice3);
                        Assert.IsNotNull(bob3);
                        Assert.AreEqual(20, alice3.Age);
                        Assert.AreEqual(39, bob3.Age);
                    }
                }
            }
        }

        [Test]
        public void MultiLiteralPropertyRefreshStoreWins()
        {
            using (var context1 = NewContext())
            {
                var alice = context1.Persons.Create();
                var bob = context1.Persons.Create();
                alice.Age = 21;
                bob.Age = 40;
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(x => x.Id.Equals(alice.Id));
                    var bob2 = context2.Persons.FirstOrDefault(x => x.Id.Equals(bob.Id));
                    Assert.IsNotNull(alice2);
                    Assert.IsNotNull(bob2);
                    alice2.Age = 22;
                    bob2.Age = 41;
                    context2.SaveChanges();
                }

                alice.Age = 20;
                bob.Age = 39;
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.StoreWins, new[] {alice, bob});
                    Assert.AreEqual(22, alice.Age);
                    Assert.AreEqual(41, bob.Age);
                    context1.SaveChanges();

                    using (var context3 = NewContext())
                    {
                        var alice3 = context3.Persons.FirstOrDefault(x => x.Id.Equals(alice.Id));
                        var bob3 = context3.Persons.FirstOrDefault(x => x.Id.Equals(bob.Id));
                        Assert.IsNotNull(alice3);
                        Assert.IsNotNull(bob3);
                        Assert.AreEqual(22, alice3.Age);
                        Assert.AreEqual(41, bob3.Age);
                    }
                }
            }
        }

        [Test]
        public void MultiLiteralPropertyRefreshMixedModes()
        {
            using (var context1 = NewContext())
            {
                var alice = context1.Persons.Create();
                var bob = context1.Persons.Create();
                alice.Age = 21;
                bob.Age = 40;
                context1.SaveChanges();

                using (var context2 = NewContext())
                {
                    var alice2 = context2.Persons.FirstOrDefault(x => x.Id.Equals(alice.Id));
                    var bob2 = context2.Persons.FirstOrDefault(x => x.Id.Equals(bob.Id));
                    Assert.IsNotNull(alice2);
                    Assert.IsNotNull(bob2);
                    alice2.Age = 22;
                    bob2.Age = 41;
                    context2.SaveChanges();
                }
                alice.Age = 20;
                bob.Age = 39;
                try
                {
                    context1.SaveChanges();
                    Assert.Fail("Expected TransactionPreconditionsFailed exception");
                }
                catch (TransactionPreconditionsFailedException)
                {
                    context1.Refresh(RefreshMode.StoreWins, alice);
                    context1.Refresh(RefreshMode.ClientWins, bob);
                    Assert.AreEqual(22, alice.Age);
                    Assert.AreEqual(39, bob.Age);
                    context1.SaveChanges();

                    var context3 = NewContext();
                    var alice3 = context3.Persons.FirstOrDefault(x => x.Id.Equals(alice.Id));
                    var bob3 = context3.Persons.FirstOrDefault(x => x.Id.Equals(bob.Id));
                    Assert.IsNotNull(alice3);
                    Assert.IsNotNull(bob3);
                    Assert.AreEqual(22, alice3.Age);
                    Assert.AreEqual(39, bob3.Age);
                }
            }
        }
        #endregion

        #region CRUD
        [Test]
        public void TestCreateAndDeleteInSameContext()
        {
            using (var context = NewContext())
            {
                var alice = context.Persons.Create();
                alice.Name = "Alice";
                context.SaveChanges();

                var aliceId = alice.Id;

                context.DeleteObject(alice);
                context.SaveChanges();

                Assert.That(context.Persons.FirstOrDefault(p=>p.Id.Equals(aliceId)), Is.Null);
            }
        }

        #endregion
    }
}