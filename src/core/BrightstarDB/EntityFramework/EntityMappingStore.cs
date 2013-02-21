using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Manages the interface to implementation class mappings, interface to resource type mappings
    /// and property to RDF property type mappings required by a <see cref="EntityContext"/>.
    /// </summary>
    [DoNotPruneType, DoNotObfuscateType]
    public class EntityMappingStore
    {
        private readonly Dictionary<Type, string> _typeMappings;
        private readonly Dictionary<Type, string> _identifierPrefixes; 
        private readonly Dictionary<PropertyInfo, PropertyHint> _propertyHints;
        private readonly Dictionary<Type, Type> _implMappings;
        private readonly Dictionary<Type, Type> _interfaceMappings;
        private readonly Dictionary<Type, PropertyInfo> _identityProperties; 

        /// <summary>
        /// Creates a new mapping store with no mappings defined
        /// </summary>
        public EntityMappingStore()
        {
            _typeMappings = new Dictionary<Type, string>();
            _identifierPrefixes = new Dictionary<Type, string>();
            _propertyHints = new Dictionary<PropertyInfo, PropertyHint>();
            _implMappings = new Dictionary<Type, Type>();
            _interfaceMappings = new Dictionary<Type, Type>();
            _identityProperties = new Dictionary<Type, PropertyInfo>();
        }

        /// <summary>
        /// Creates a new mapping store that copies its initial mappings from the specified source store
        /// </summary>
        /// <param name="source">The source <see cref="EntityMappingStore"/> from which mappings are copied</param>
        public EntityMappingStore(EntityMappingStore source)
        {
            _typeMappings = new Dictionary<Type, string>(source._typeMappings);
            _identifierPrefixes = new Dictionary<Type, string>(source._identifierPrefixes);
            _propertyHints = new Dictionary<PropertyInfo, PropertyHint>(source._propertyHints);
            _implMappings = new Dictionary<Type, Type>(source._implMappings);
            _identityProperties = new Dictionary<Type, PropertyInfo>(source._identityProperties);
        }

        /// <summary>
        /// Adds a mapping between an entity definition interface and its implementation class
        /// </summary>
        /// <typeparam name="I">The entity definition interface type</typeparam>
        /// <typeparam name="T">The entity implementation class type</typeparam>
        public void AddImplMapping<I, T>()
            where I : class
            where T : I
        {
            _implMappings[typeof(I)] = typeof(T);
            _interfaceMappings[typeof (T)] = typeof (I);
        }

        /// <summary>
        /// Sets the prefix string for generated URI identifiers for the instances of an entity type
        /// </summary>
        /// <param name="mappedType">The entity implementation class type</param>
        /// <param name="prefix">The URI identifier prefix string</param>
        public void AddIdentifierPrefix(Type mappedType, string prefix)
        {
            _identifierPrefixes[mappedType] = prefix;
        }

        /// <summary>
        /// Gets the prefix string for the generated URI identifiers for the instances of an entity type
        /// </summary>
        /// <param name="mappedType">The entity implementation class type</param>
        /// <returns>The URI identifier prefix string for the entity implementation type or null if there is no mapping</returns>
        public string GetIdentifierPrefix(Type mappedType)
        {
            string prefix;
            Type interfaceType;
            if (_identifierPrefixes.TryGetValue(mappedType, out prefix))
            {
                return prefix;
            }
            if (_interfaceMappings.TryGetValue(mappedType, out interfaceType) &&
                _identifierPrefixes.TryGetValue(interfaceType, out prefix))
            {
                return prefix;
            }
            return null;
        }

        /// <summary>
        /// Sets the URI identifier for the RDF schema type that is mapped to an entity type
        /// </summary>
        /// <param name="mappedType">The entity type</param>
        /// <param name="typeUri">The schema type URI</param>
        public void AddTypeMapping(Type mappedType, string typeUri)
        {
            _typeMappings[mappedType] = typeUri;
        }

        /// <summary>
        /// Sets the URI identifier for the RDF schema tyep that is mapped to an entity type
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="typeUri">The schema type URI</param>
        public void AddTypeMapping<T>(string typeUri) where T : class
        {
            _typeMappings[typeof(T)] = typeUri;
        }

        /// <summary>
        /// Sets the mapping hint for a .NET property
        /// </summary>
        /// <param name="propertyInfo">The property that the hint is set for</param>
        /// <param name="propertyHint">The property hint information</param>
        public void AddPropertyHint(PropertyInfo propertyInfo, PropertyHint propertyHint)
        {
            _propertyHints.Add(propertyInfo, propertyHint);
            if (propertyHint.MappingType == PropertyMappingType.Id && propertyInfo.DeclaringType != null)
            {
                _identityProperties[propertyInfo.DeclaringType] = propertyInfo;
            }
        }

        /// <summary>
        /// Gets the mapping hint for a .NET property
        /// </summary>
        /// <param name="propertyInfo">The property whose mapping hint is to be retrieved</param>
        /// <returns>The property mapping hint or null if no mapping hint is found for the specified property</returns>
        public PropertyHint GetPropertyHint(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) throw new ArgumentNullException("propertyInfo");
            if ((propertyInfo.DeclaringType != null) && _interfaceMappings.ContainsKey(propertyInfo.DeclaringType))
            {
                foreach(var @interface in propertyInfo.DeclaringType.GetInterfaces().Where(i=>_implMappings.ContainsKey(i)))
                {
                    var interfaceProperty = @interface.GetProperty(propertyInfo.Name);
                    if (interfaceProperty != null)
                    {
                        propertyInfo = interfaceProperty;
                        break;
                    }
                }
            }
            PropertyHint ret;
            return _propertyHints.TryGetValue(propertyInfo, out ret) ? ret : null;
        }

        /// <summary>
        /// Retrieves the schema type URI that is mapped to the specified entity implementation type
        /// </summary>
        /// <param name="type">The entity implementation type</param>
        /// <returns>The schema type URI</returns>
        /// <exception cref="MappingNotFoundException">Raised if <paramref name="type"/> is not a mapped entity implementation type or if no schema type URI has been mapped</exception>
        public string GetMappedInterfaceTypeUri(Type type)
        {
            Type interfaceType;
            string identifier;
            if (_interfaceMappings.TryGetValue(type, out interfaceType) &&
                _typeMappings.TryGetValue(interfaceType, out identifier))
            {
                return identifier;
            }
            throw new MappingNotFoundException(type);
        }
        
        /// <summary>
        /// Returns the collection of schema type URIs that a given entity implementation type can be mapped to
        /// </summary>
        /// <param name="type">The entity implementation type</param>
        /// <returns>An enumeration over the collection of schema type URIs that the implementation type can be mapped to</returns>
        /// <remarks>An entity implementation type can potentially map to multiple schema type URIs e.g. when the implementation type
        /// implements multiple interfaces or where there is an inheritance hierarchy on the interfaces</remarks>
        public IEnumerable<string> MapTypeToUris(Type type)
        {
            bool haveMapping = false;
            if (_typeMappings.ContainsKey(type))
            {
                haveMapping = true;
                yield return _typeMappings[type];
            }
            foreach(var typeUri in 
                type.GetInterfaces().Where(i => _typeMappings.ContainsKey(i)).Select(i => _typeMappings[i]))
            {
                if(typeUri != null)
                {
                    haveMapping = true;
                    yield return typeUri;
                }
            }
            if (!haveMapping)
            {
                throw new MappingNotFoundException(type);
            }
        }

        /// <summary>
        /// Returns the entity implementation type for a specific entity interface type
        /// </summary>
        /// <param name="interfaceType">The entity interface type</param>
        /// <returns>The entity implementation type or <paramref name="interfaceType"/> is no mapping is found</returns>
        /// <remarks>This method returns the input <paramref name="interfaceType"/> to allow a caller to pass a type that may be either an interface or an implementation type in and get the interface type out
        /// without having to know if the parameter actually is an interface type already.</remarks>
        public Type GetImplType(Type interfaceType)
        {
            Type implType;
            if (_implMappings.TryGetValue(interfaceType, out implType))
            {
                return implType;
            }
            return interfaceType;
        }

        /// <summary>
        /// Returns true if the type is the generated implementation type for an entity interface
        /// </summary>
        /// <param name="t">The type to be tested</param>
        /// <returns>True if the type is an entity implementation type, false otherwise</returns>
        public bool IsMappedImplementation(Type t)
        {
            return _interfaceMappings.ContainsKey(t);
        }

        /// <summary>
        /// Returns true if the type is an entity interface type
        /// </summary>
        /// <param name="t">The type to be tested</param>
        /// <returns>True if the type is an entity interface type, false otherwise</returns>
        public bool IsKnownInterface(Type t)
        {
            return _implMappings.ContainsKey(t);
        }

        /// <summary>
        /// Returns the property that is mapped as the Identity property for the specified entity type
        /// </summary>
        /// <param name="t">The entity type to check. This can be either the entity interface type or the concrete implementation type</param>
        /// <returns>The mapped property info or null if no match is found</returns>
        public PropertyInfo GetIdentityProperty(Type t)
        {
            PropertyInfo ret;
            if (!_identityProperties.TryGetValue(t, out ret))
            {
                Type interfaceType;
                if (_interfaceMappings.TryGetValue(t, out interfaceType))
                {
                    _identityProperties.TryGetValue(interfaceType, out ret);
                }
            }
            return ret;
        }
    }
}
