using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestClass]
    public class IdentifierEncodingTests
    {
        private MyEntityContext _myEntityContext;

        private readonly string _connectionString =
            "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=IdentifierEncodingTests_" + DateTime.Now.Ticks;

        [TestInitialize]
        public void SetUp()
        {
            _myEntityContext = new MyEntityContext(_connectionString);
        }

        [TestCleanup]
        public void TearDown()
        {
            _myEntityContext.Dispose();
        }

        [TestMethod]
        public void TestCreateItemWithSpecialCharactersInIdentifier()
        {
            var person = new DBPediaPerson
                             {
                                 Id = "Aleksandar_Đorđević",
                                 Name = "Aleksandar Djordjevic",
                                 GivenName = "Aleksandar",
                                 Surname = "Djordjevic"
                             };
            _myEntityContext.DBPediaPersons.Add(person);
            _myEntityContext.SaveChanges();

            // Try to retrieve by Id 
            var found = _myEntityContext.DBPediaPersons.FirstOrDefault(p => p.Id.Equals("Aleksandar_Đorđević"));
            Assert.IsNotNull(found);
            Assert.AreEqual("Aleksandar", found.GivenName);
            Assert.AreEqual("Aleksandar_Đorđević", found.Id);

        }
    }
}
