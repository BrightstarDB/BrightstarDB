using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData
{
    internal class DataServiceQueryProvider<T> : IDataServiceQueryProvider where T : BrightstarEntityContext
    {
        T _currentDataSource;
        readonly DataServiceMetadataProvider _metadata;

        public DataServiceQueryProvider(DataServiceMetadataProvider metadata)
        {
            _metadata = metadata;
        }

        public object CurrentDataSource
        {
            get {
                return _currentDataSource;
            }
            set {
                _currentDataSource= value as T;
            }
        } 
        
        public object GetOpenPropertyValue(object target, string propertyName)
        {
            // not going to be called on us.
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<string, object>> GetOpenPropertyValues(object target)
        {
            // never going to be called for our strongly typed, closed world model
            throw new NotImplementedException();
        }
        
        public object GetPropertyValue(object target,ResourceProperty resourceProperty)
        {
            // never called on strongly typed model
            throw new NotImplementedException();
        } 

        public IQueryable GetQueryRootForResourceSet(ResourceSet resourceSet)
        {
            // need to translate between the resource set and the IQueryable method 
            var pinfo = _metadata.CollectionPropertyNames[resourceSet.Name];
            var o = pinfo.GetValue(_currentDataSource, null);
            return o as IQueryable; 
        }
 
        public ResourceType GetResourceType(object target)
        {
            if (target.GetType().IsInterface)
            {
                Type type = target.GetType();
                return _metadata.Types.Single(t => t.InstanceType == type);                 
            }

            foreach (var intf in target.GetType().GetInterfaces())
            {
                var type = _metadata.Types.Where(t => t.InstanceType == intf).FirstOrDefault();
                if (type != null) return type;
            }

            throw new BrightstarInternalException("Unable to find Resource type for " + target);
        }
 
        public object InvokeServiceOperation(
             ServiceOperation serviceOperation,
             object[] parameters)
        {
            throw new NotImplementedException();
        }
 
        public bool IsNullPropagationRequired
        {
            get { return false; }
        }
    } 
}