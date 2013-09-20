using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class ContextCallbackTests
    {
        private readonly string _storeName;
        private readonly string _connectionString;
        private readonly List<BrightstarEntityObject> _changedItems = new List<BrightstarEntityObject>();
 
        public ContextCallbackTests()
        {
            _storeName = "EFContextCallbackTests_" + DateTime.Now.Ticks;
            _connectionString = "type=embedded;storesDirectory=c:\\brightstar;storeName=" + _storeName;
        }

        [Test]
        public void TestSavingCallbackCalled()
        {
            using (var context = new MyEntityContext(_connectionString))
            {
                _changedItems.Clear();
                context.SavingChanges += LogChangedItems;

                var alice = new Person {Name = "Alice"};
                context.Persons.Add(alice);
                var bob = context.Persons.Create();
                bob.Name = "Bob";
                context.SaveChanges();

                Assert.AreEqual(2, _changedItems.Count);
                Assert.IsTrue(_changedItems.Cast<Person>().Any(p => p.Id.Equals(alice.Id)));
                Assert.IsTrue(_changedItems.Cast<Person>().Any(p => p.Id.Equals(bob.Id)));
                _changedItems.Clear();

                bob.Friends.Add(alice);
                context.SaveChanges();
                Assert.AreEqual(1, _changedItems.Count);
                Assert.IsTrue(_changedItems.Cast<Person>().Any(p => p.Id.Equals(bob.Id)));
                _changedItems.Clear();

                var skill = new Skill {Name = "Programming"};
                context.Skills.Add(skill);
                context.SaveChanges();
                _changedItems.Clear();

                skill.SkilledPeople.Add(bob);
                context.SaveChanges();
                Assert.AreEqual(1, _changedItems.Count);
                Assert.IsTrue(_changedItems.Cast<Person>().Any(p => p.Id.Equals(bob.Id)));
                _changedItems.Clear();
            }
        }

        [Test]
        public void TestSaveWorksWhenNoCallback()
        {
            using (var context = new MyEntityContext(_connectionString))
            {
                var carol = new Person {Name = "Carol"};
                context.Persons.Add(carol);
                context.SaveChanges();

                var found = context.Persons.FirstOrDefault(p => p.Name.Equals("Carol"));
                Assert.IsNotNull(found);
            }
        }

        [Test]
        public void TestSavingChangesUpdatesTimestamp()
        {
            IArticle article;
            DateTime saving, updating;
            using (var context = new MyEntityContext(_connectionString))
            {
                context.SavingChanges += UpdateTrackable;
                article = context.Articles.Create();
                article.Title = "My Test Article";
                saving = DateTime.Now;
                context.SaveChanges();
            }
            using (var context = new MyEntityContext(_connectionString))
            {
                context.SavingChanges += UpdateTrackable;
                article = context.Articles.FirstOrDefault(a => a.Id.Equals(article.Id));
                Assert.IsNotNull(article);
                Assert.IsTrue(article.Created >= saving);
                Assert.IsTrue(article.LastModified >= saving);
                article.BodyText = "Some body text";
                updating = DateTime.Now;
                context.SaveChanges();
            }
            using (var context = new MyEntityContext(_connectionString))
            {
                article = context.Articles.FirstOrDefault(a => a.Id.Equals(article.Id));
                Assert.IsNotNull(article);
                Assert.IsTrue(article.Created >= saving);
                Assert.IsTrue(article.LastModified >= updating);
            }
        }

        private void UpdateTrackable(object sender, EventArgs e)
        {
            var context = sender as MyEntityContext;
            foreach(var t in context.TrackedObjects.Where(t=>t is ITrackable).Cast<ITrackable>())
            {
                if (t.Created.Equals(DateTime.MinValue)) t.Created = DateTime.Now;
                t.LastModified = DateTime.Now;
            }
        }


        [Test]
        public void TestThrowingExceptionAbortsChanges()
        {
            using (var context = new MyEntityContext(_connectionString))
            {
                context.SavingChanges += ThrowOnChange;
                var dave = new Person {Name = "Dave"};
                context.Persons.Add(dave);
                try
                {
                    context.SaveChanges();
                }
                catch (ApplicationException)
                {
                    // Expected
                }
            }
            using (var context = new MyEntityContext(_connectionString))
            {
                var found = context.Persons.FirstOrDefault(p => p.Name.Equals("Dave"));
                Assert.IsNull(found);
            }
        }

        private void LogChangedItems(object sender, EventArgs e)
        {
            var context = sender as MyEntityContext;
            Assert.IsNotNull(context);
            foreach (var entity in context.TrackedObjects.Where(t=>t.IsModified))
            {
                _changedItems.Add(entity);
            }
        }

        private void ThrowOnChange(object sender, EventArgs e)
        {
            throw new ApplicationException("Oh noes!");
        }
    }
}
