using System;
using System.Linq;
using BrightstarDB.Mobile.Tests.EntityFramework;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Mobile.Tests
{
    [TestClass]
    public class EntityFrameworkTests
    {
        [TestMethod]
        [Tag("EF")]
        public void TestCreateEntity()
        {
            var context = new MyEntityContext("type=embedded;storesdirectory=brightstar;storename=test-" + Guid.NewGuid());
            context.Notes.Add(new Note{Title="My First Note"});
            context.SaveChanges();
            var allNotes = context.Notes.ToList();
            Assert.AreEqual(1, allNotes.Count());
        }
    }
}
