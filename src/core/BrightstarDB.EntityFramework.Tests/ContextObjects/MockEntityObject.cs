using System;
using System.Collections.Generic;

namespace BrightstarDB.EntityFramework.Tests.ContextObjects
{
    public class MockEntityObject : IEntityObject
    {
        public bool IsAttached { get; private set; }
        public bool IsModified { get; private set; }
        public EntityContext Context { get; private set; }
        public string Key { get; set; }
        public void ReportPropertyChanging(string propertyName, object newValue)
        {
            throw new NotImplementedException();
        }

        public void ReportPropertyChanged(string propertyName)
        {
            throw new NotImplementedException();
        }

        public void SetRelatedObject<T>(string propertyName, T value) where T : class
        {
            throw new NotImplementedException();
        }

        public T GetRelatedObject<T>(string propertyName) where T : class
        {
            throw new NotImplementedException();
        }

        public IEntityCollection<T> GetRelatedObjects<T>(string propertyName) where T : class
        {
            throw new NotImplementedException();
        }

        public void SetRelatedObjects<T>(string propertyName, ICollection<T> relatedObjects) where T : class
        {
            throw new NotImplementedException();
        }

        public void Attach(EntityContext context, bool overwriteExisting)
        {
            throw new NotImplementedException();
        }

        public void Detach()
        {
            throw new NotImplementedException();
        }

        public string GetKey()
        {
            return Key;
        }
    }
}