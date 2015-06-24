using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;
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

        [Test]
        public void TestCreateCompositeKeyEntity()
        {
            using (var context = GetContext())
            {
                var entity = context.CompositeKeyEntities.Create();
                entity.First = "alpha";
                entity.Second = 1;
                context.SaveChanges();
            }
            using (var context = GetContext())
            {
                var entity = context.CompositeKeyEntities.FirstOrDefault(x => x.Id.Equals("alpha.1"));
                Assert.That(entity, Is.Not.Null);
                Assert.That(entity.First, Is.EqualTo("alpha"));
                Assert.That(entity.Second, Is.EqualTo(1));
            }
        }

        [Test]
        public void TestCreateParentChildCompositeKey()
        {
            using (var context = GetContext())
            {
                var parent = new BaseEntity{Id="foo"};
                context.BaseEntities.Add(parent);
                var child = new ChildKeyEntity {Parent = parent, Position = 1};
                var child2 = new ChildKeyEntity {Parent = parent, Position = 2};
                context.ChildKeyEntities.Add(child);
                context.ChildKeyEntities.Add(child2);
                context.SaveChanges();
            }

            using (var context = GetContext())
            {
                var entity = context.ChildKeyEntities.FirstOrDefault(x => x.Id.Equals("foo/1"));
                Assert.That(entity, Is.Not.Null);
                var entity2 = context.ChildKeyEntities.FirstOrDefault(x => x.Id.Equals("foo/2"));
                Assert.That(entity2, Is.Not.Null);
            }
        }

        [Test]
        public void TestCreateHiearchicalKey()
        {
            using (var context = GetContext())
            {
                var root = new HierarchicalKeyEntity {Code = "root"};
                var child = new HierarchicalKeyEntity {Parent = root, Code = "Child"};
                var grandchild = new HierarchicalKeyEntity {Parent = child, Code = "Grandchild"};
                context.HierarchicalKeyEntities.Add(root);
                context.HierarchicalKeyEntities.Add(child);
                context.HierarchicalKeyEntities.Add(grandchild);
                context.SaveChanges();
            }

            using (var context = GetContext())
            {
                var gc = context.HierarchicalKeyEntities.FirstOrDefault(x => x.Id.Equals("root/Child/Grandchild"));
                Assert.That(gc, Is.Not.Null);
                var c = context.HierarchicalKeyEntities.FirstOrDefault(x => x.Id.Equals("root/Child"));
                Assert.That(c, Is.Not.Null);
                var r = context.HierarchicalKeyEntities.FirstOrDefault(x => x.Id.Equals("root"));
                Assert.That(r, Is.Not.Null);
            }
        }

        // Test using a custom key converter
        [Test]
        public void TestCustomKeyConverter()
        {
            using (var context = GetContext())
            {
                var entity = context.LowerKeyEntities.Create();
                entity.Name = "FooBar";
                context.SaveChanges();
            }
            using (var context = GetContext())
            {
                var entity = context.LowerKeyEntities.FirstOrDefault(x => x.Id.Equals("foobar"));
                Assert.That(entity, Is.Not.Null);
                Assert.That(entity.Name, Is.EqualTo("FooBar"));
            }
        }

        // Test parent/child or hierarchical composite key with parent created in one context and child in a subsequent context
        [Test]
        public void TestStagedCompositeKeyGeneration()
        {
            string parentId;
            using (var context = GetContext())
            {
                var parent = context.BaseEntities.Create();
                parent.BaseStringValue = "Parent";
                parentId = parent.Id;
                context.SaveChanges();
            }
            using (var context = GetContext())
            {
                var parent = context.BaseEntities.FirstOrDefault(x => x.Id.Equals(parentId));
                Assert.That(parent, Is.Not.Null);
                var child = context.ChildKeyEntities.Create();
                child.Parent = parent;
                child.Position = 1;
                context.SaveChanges();
            }
            using (var context = GetContext())
            {
                var child = context.ChildKeyEntities.FirstOrDefault(x => x.Id.Equals(parentId + "/1"));
                Assert.That(child, Is.Not.Null);
                Assert.That(child.Position, Is.EqualTo(1));
                Assert.That(child.Parent.Id, Is.EqualTo(parentId));
            }
        }

        [Test]
        public void TestUpdateEntityWithCompositeKey()
        {
            string childId;
            using (var context = GetContext())
            {
                var parent = context.ParentEntities.Create();
                var child = context.ChildEntities.Create();
                child.Code = "child";
                child.Description = "Some description";
                child.Parent = parent;
                childId = child.Id;
                context.SaveChanges();
            }

            using (var context = GetContext())
            {
                var child = context.ChildEntities.First(x=>x.Id.Equals(childId));
                child.Description = "Update description";
                context.SaveChanges();
            }
        }

        [Test]
        [ExpectedException(typeof(EntityKeyRequiredException))]
        public void TestEntityKeyRequiredExceptionWhenIdIsNull()
        {
            using (var context = GetContext())
            {
                var entity = new StringKeyEntity();
                context.StringKeyEntities.Add(entity);
            }
        }

        [Test]
        [ExpectedException(typeof (EntityKeyRequiredException))]
        public void TestEntityKeyRequiredExceptionWhenIdIsEmptyString()
        {
            using (var context = GetContext())
            {
                var entity = new StringKeyEntity{Name=""};
                context.StringKeyEntities.Add(entity);
            }
            
        }

        [Test]
        public void Issue194Repro()
        {
            using (var context = GetContext())
            {
                var parent = new ParentEntity();
                parent.Id = "parent";
                var child = context.ChildEntities.Create();
                child.Code = "child";
                child.Description = "Some description";
                child.Parent = parent;
                context.ChildEntities.Add(child);

                context.SaveChanges();
            }

            using (var context = GetContext())
            {
                var parent = context.ParentEntities.First();
                var modifiedChild = new ChildEntity
                {
                    Parent = parent,
                    Code = "child",
                    Description = "A new description for the existing child"
                };

                var existingChild = context.ChildEntities.First();
                context.DeleteObject(existingChild);
                context.ChildEntities.Add(modifiedChild);
                context.SaveChanges();
            }
        }

        [Test]
        public void Issue194Repro2()
        {
            using (var context = GetContext())
            {
                var parent = new ParentEntity2();
                parent.Id = "parent";
                var child = context.ChildEntity2s.Create();
                child.Code = "child";
                child.Description = "Some description";
                child.Parent = parent;
                context.ChildEntity2s.Add(child);

                context.SaveChanges();
            }

            using (var context = GetContext())
            {
                var parent = context.ParentEntity2s.First();
                var modifiedChild = new ChildEntity2
                {
                    Parent = parent,
                    Code = "child",
                    Description = "A new description for the existing child"
                };

                var existingChild = context.ChildEntity2s.First();
                context.DeleteObject(existingChild);
                context.ChildEntity2s.Add(modifiedChild);
                context.SaveChanges();
            }
        }
    }
}
