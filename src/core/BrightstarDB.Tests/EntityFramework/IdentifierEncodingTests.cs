using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestClass]
    public class IdentifierEncodingTests
    {
                private readonly MyEntityContext _myEntityContext;

        private readonly string _connectionString =
            "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=IdentifierEncodingTests_" + DateTime.Now.Ticks;

        public IdentifierEncodingTests()
        {
            _myEntityContext = new MyEntityContext(_connectionString);
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
            var found = _myEntityContext.DBPediaPersons.Where(p => p.Id.Equals("Aleksandar_Đorđević")).FirstOrDefault();
            Assert.IsNotNull(found);
            Assert.AreEqual("Aleksandar", found.GivenName);
            Assert.AreEqual("Aleksandar_Đorđević", found.Id);

            //var foundEntity = found as BrightstarEntityObject;
            //Assert.IsNotNull(foundEntity);
            //Assert.AreEqual("http://dbpedia.org/resource/Aleksandar_Đorđević", foundEntity.DataObject.Identity);
        }
    }
}
