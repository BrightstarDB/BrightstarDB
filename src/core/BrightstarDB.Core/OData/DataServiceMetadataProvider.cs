using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Reflection;

namespace BrightstarDB.OData
{
    ///<summary>
    /// Provides metadata about the currently exposed entity model.
    ///</summary>
    internal class DataServiceMetadataProvider : IDataServiceMetadataProvider 
    {
        private readonly Dictionary<string, ResourceType> _resourceTypes  = new Dictionary<string, ResourceType>();
        private readonly Dictionary<string, ResourceSet> _resourceSets = new Dictionary<string, ResourceSet>();
        private readonly Dictionary<string, PropertyInfo> _collectionPropertyNames;
        private readonly List<ResourceAssociationSet> _associationSets = new List<ResourceAssociationSet>();

        public DataServiceMetadataProvider(Dictionary<string, PropertyInfo> collectionNames)
        {
            _collectionPropertyNames = collectionNames;
        }

        public Dictionary<string, PropertyInfo> CollectionPropertyNames
        {
            get { return _collectionPropertyNames; }
        }
        
        public void AddResourceType(ResourceType type)
        {
           type.SetReadOnly();
           _resourceTypes.Add(type.FullName, type);
        }

        public void AddResourceSet(ResourceSet set)
        {
           set.SetReadOnly();
           _resourceSets.Add(set.Name, set);
        }

        public string ContainerName
        {
            get { return "BrightstarEntities"; }
        } 

        public string ContainerNamespace
        {
            get { return "Namespace"; }
        }

        public IEnumerable<ResourceType> GetDerivedTypes(
            ResourceType resourceType
        )
        {
            // We don't support type inheritance yet
            yield break;
        } 

        public ResourceAssociationSet GetResourceAssociationSet(
            ResourceSet resourceSet,
            ResourceType resourceType,
            ResourceProperty resourceProperty)
        {
            // resourceProperty.GetAnnotation().ResourceAssociationSet;
            return resourceProperty.CustomState as ResourceAssociationSet;
        } 

        public bool HasDerivedTypes(ResourceType resourceType)
        {
            // We don’t support inheritance yet
            return false;
        }

        public IEnumerable<ResourceSet> ResourceSets
        {
            get { return _resourceSets.Values; }
        } 

        public IEnumerable<ServiceOperation> ServiceOperations
        {
            // No service operations yet
            get { yield break; }
        } 

        public bool TryResolveResourceSet(
            string name,
            out ResourceSet resourceSet)
        {
            return _resourceSets.TryGetValue(name, out resourceSet);
        } 

        public bool TryResolveResourceType(
            string name, 
            out ResourceType resourceType)
        {
            return _resourceTypes.TryGetValue(name, out resourceType);
        }

        public bool TryResolveServiceOperation(
            string name, 
            out ServiceOperation serviceOperation)
        {
            // No service operations are supported yet
            serviceOperation = null;
            return false;
        } 

        public IEnumerable<ResourceType> Types
        {
            get { return _resourceTypes.Values; }
        }

        public void AddAssociationSet(ResourceAssociationSet resourceAssociationSet)
        {
            _associationSets.Add(resourceAssociationSet); 
        }
    }
}