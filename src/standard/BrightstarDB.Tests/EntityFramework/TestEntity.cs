using System;

namespace BrightstarDB.Tests.EntityFramework
{
    internal partial class TestEntity : IEquatable<TestEntity>
    {
        public bool OnCreatedWasCalled { get; set; }

        public bool Equals(TestEntity other)
        {
            //Check whether the compared object is null.
            if (Object.ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data.
            if (Object.ReferenceEquals(this, other)) return true;

            //Check whether the products' SomeString and SomeInt properties are equal.
            return SomeString.Equals(other.SomeString) && SomeInt.Equals(other.SomeInt);
        }

        protected override void OnCreated(BrightstarDB.EntityFramework.BrightstarEntityContext context)
        {
            OnCreatedWasCalled = true;
        }

        // Implement the property we asked B* to ignore
        public string AnIgnoredProperty { get; set; }
    }
}
