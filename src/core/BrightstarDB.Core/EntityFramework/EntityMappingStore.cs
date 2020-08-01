using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Manages the interface to implementation class mappings, interface to resource type mappings
    /// and property to RDF property type mappings required by a <see cref="EntityContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a singleton instance to allow all entities to access mapping information
    /// even when not currently attached to a context. It is therefore important that if multiple
    /// contexts are concurrently actvie they all share the same type and property mapping
    /// information. This will always be the case using the default <see cref="ReflectionMappingProvider"/>
    /// as the mapping information comes from compile-time type specification. 
    /// </para>
    /// <para>Change made to a mapping will not affect pre-existing items in the context, it will only 
    /// affect those items when they are modified or when new items are created or retrieved from a context.</para>
    /// </remarks>
    public class EntityMappingStore
    {
        private readonly Dictionary<Type, string> _typeMappings;
        //private readonly Dictionary<Type, string> _identifierPrefixes;
        private readonly Dictionary<PropertyInfo, PropertyHint> _propertyHints;
        private readonly Dictionary<Type, Type> _implMappings;
        private readonly Dictionary<Type, Type> _interfaceMappings;
        private readonly Dictionary<Type, IdentityInfo> _identityInfo;

        /// <summary>
        /// Returns the singleton instance of this class that tracks all entity mapping
        /// information for the current application domain.
        /// </summary>
        public static readonly EntityMappingStore Instance = new EntityMappingStore();

        /// <summary>
        /// Creates a new mapping store with no mappings defined
        /// </summary>
        private EntityMappingStore()
        {
            _typeMappings = new Dictionary<Type, string>();
            //_identifierPrefixes = new Dictionary<Type, string>();
            _propertyHints = new Dictionary<PropertyInfo, PropertyHint>();
            _implMappings = new Dictionary<Type, Type>();
            _interfaceMappings = new Dictionary<Type, Type>();
            //_identityProperties = new Dictionary<Type, PropertyInfo>();
            _identityInfo = new Dictionary<Type, IdentityInfo>();
        }


        #region Instance Methods

        /// <summary>
        /// Adds a mapping between an entity definition interface and its implementation class
        /// </summary>
        /// <typeparam name="I">The entity definition interface type</typeparam>
        /// <typeparam name="T">The entity implementation class type</typeparam>
        public void SetImplMapping<I, T>()
            where I : class
            where T : I
        {
            _implMappings[typeof (I)] = typeof (T);
            _interfaceMappings[typeof (T)] = typeof (I);
        }

        /*
        /// <summary>
        /// Sets the prefix string for generated URI identifiers for the instances of an entity type
        /// </summary>
        /// <param name="mappedType">The entity implementation class type</param>
        /// <param name="prefix">The URI identifier prefix string</param>
        //public void SetIdentifierPrefix(Type mappedType, string prefix)
        //{
        //    _identifierPrefixes[mappedType] = prefix;
        //}
        */

        /// <summary>
        /// Gets the prefix string for the generated URI identifiers for the instances of an entity type
        /// </summary>
        /// <param name="mappedType">The entity implementation class type</param>
        /// <returns>The URI identifier prefix string for the entity implementation type or null if there is no mapping</returns>
        public static string GetIdentifierPrefix(Type mappedType)
        {
            var identityInfo = GetIdentityInfo(mappedType);
            return identityInfo == null ? null : identityInfo.BaseUri;
        }

        /*
        private string _GetIdentifierPrefix(Type mappedType)
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

            foreach (var iface in mappedType.GetInterfaces())
            {
                if (_identifierPrefixes.TryGetValue(iface, out prefix)) return prefix;
            }
            return null;
        }
        */

        /// <summary>
        /// Sets the URI identifier for the RDF schema type that is mapped to an entity type
        /// </summary>
        /// <param name="mappedType">The entity type</param>
        /// <param name="typeUri">The schema type URI</param>
        public void SetTypeMapping(Type mappedType, string typeUri)
        {
            _typeMappings[mappedType] = typeUri;
        }

        /// <summary>
        /// Sets the URI identifier for the RDF schema type that is mapped to an entity type
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="typeUri">The schema type URI</param>
        public void SetTypeMapping<T>(string typeUri) where T : class
        {
            _typeMappings[typeof (T)] = typeUri;
        }

        /// <summary>
        /// Sets the mapping hint for a .NET property
        /// </summary>
        /// <param name="propertyInfo">The property that the hint is set for</param>
        /// <param name="propertyHint">The property hint information</param>
        public void SetPropertyHint(PropertyInfo propertyInfo, PropertyHint propertyHint)
        {
            _propertyHints[propertyInfo] = propertyHint;
        }

        /// <summary>
        /// Sets the identity mapping information for a .NET type
        /// </summary>
        /// <param name="identityInfo">The entity identity mapping information</param>
        /// <param name="type">The entity type</param>
        public void SetIdentityInfo(Type type, IdentityInfo identityInfo)
        {
            _identityInfo[type] = identityInfo;
        }

        /// <summary>
        /// Gets the entity identifier information
        /// </summary>
        /// <param name="type">The entity type</param>
        /// <returns>The identifier information for the type or null if not information could be found</returns>
        public static IdentityInfo GetIdentityInfo(Type type)
        {
            return Instance._GetIdentityInfo(type);
        }

        private IdentityInfo _GetIdentityInfo(Type type)
        {
            Type baseType;
            if (_identityInfo.TryGetValue(type, out var ret)) return ret;
            if (_interfaceMappings.TryGetValue(type, out var interfaceType))
            {
                if (_identityInfo.TryGetValue(interfaceType, out ret))
                {
                    return ret;
                }

                baseType = interfaceType.GetTypeInfo().BaseType;
                if (baseType != null)
                {
                    return _GetIdentityInfo(baseType);
                }
            }

            baseType = type.GetTypeInfo().BaseType;
            return baseType != null ? _GetIdentityInfo(baseType) : null;
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
                foreach (
                    var @interface in
                        propertyInfo.DeclaringType.GetInterfaces().Where(i => _implMappings.ContainsKey(i)))
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
        public static string GetMappedInterfaceTypeUri(Type type)
        {
            return Instance._GetMappedInterfaceTypeUri(type);
        }

        private string _GetMappedInterfaceTypeUri(Type type)
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
        public static IEnumerable<string> MapTypeToUris(Type type)
        {
            return Instance._MapTypeToUris(type);
        }

        private IEnumerable<string> _MapTypeToUris(Type type)
        {
            bool haveMapping = false;
            if (_typeMappings.ContainsKey(type))
            {
                haveMapping = true;
                yield return _typeMappings[type];
            }
            foreach (var typeUri in
                type.GetInterfaces().Where(i => _typeMappings.ContainsKey(i)).Select(i => _typeMappings[i]))
            {
                if (typeUri != null)
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
        /// Returns the entity implementation type for a given uri
        /// </summary>
        /// <param name="typeUri">The uri representing a resource type.</param>
        /// <returns>The entity implementation type</returns>
        public Type GetImplTypeForUri(string typeUri)
        {
            Type domainType = _typeMappings.FirstOrDefault(x => x.Value == typeUri).Key;
            if (domainType == null) return null;
            Type implType = GetImplType(domainType);
            return implType;
        }

        /// <summary>
        /// Returns the entity implementation type for a specific entity interface type
        /// </summary>
        /// <param name="interfaceType">The entity interface type</param>
        /// <returns>The entity implementation type or <paramref name="interfaceType"/> is no mapping is found</returns>
        /// <remarks>This method returns the input <paramref name="interfaceType"/> to allow a caller to pass a type that may be either an interface or an implementation type in and get the interface type out
        /// without having to know if the parameter actually is an interface type already.</remarks>
        public static Type GetImplType(Type interfaceType)
        {
            return Instance._GetImplType(interfaceType);
        }

        private Type _GetImplType(Type interfaceType)
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
        public static bool IsMappedImplementation(Type t)
        {
            return Instance._interfaceMappings.ContainsKey(t);
        }

        /// <summary>
        /// Returns true if the type is an entity interface type
        /// </summary>
        /// <param name="t">The type to be tested</param>
        /// <returns>True if the type is an entity interface type, false otherwise</returns>
        public static bool IsKnownInterface(Type t)
        {
            return Instance._implMappings.ContainsKey(t);
        }

        /// <summary>
        /// Returns the property that is mapped as the Identity property for the specified entity type
        /// </summary>
        /// <param name="t">The entity type to check. This can be either the entity interface type or the concrete implementation type</param>
        /// <returns>The mapped property info or null if no match is found</returns>
        public PropertyInfo GetIdentityProperty(Type t)
        {
            IdentityInfo identityInfo;
            if (_identityInfo.TryGetValue(t, out identityInfo))
            {
                return identityInfo.IdentityProperty;
            }
            Type interfaceType;
            if (_interfaceMappings.TryGetValue(t, out interfaceType))
            {
                if (_identityInfo.TryGetValue(interfaceType, out identityInfo))
                {
                    return identityInfo.IdentityProperty;
                }
            }
            return null;
        }

        #endregion
    }
}
