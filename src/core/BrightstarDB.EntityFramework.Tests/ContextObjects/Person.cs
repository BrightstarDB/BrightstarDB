using System;

namespace BrightstarDB.EntityFramework.Tests.ContextObjects
{
    [Entity("Person")]
    public interface IPerson
    {
        [Identifier]
        string Id { get; }

        [PropertyType("father")]
        IPerson Father { get; set; }
    }

    public class Person : MockEntityObject, IPerson
    {
        #region Implementation of IPerson

        public string Id
        {
            get { throw new NotImplementedException(); }
        }

        public IPerson Father
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion
    }
}
