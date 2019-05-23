using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Class for processing one or more assemblies to populate an <see cref="EntityMappingStore"/> based on the 
    /// interfaces and attributes found via reflection
    /// </summary>
    public class ReflectionMappingProvider
    {
        private readonly Dictionary<string, AssemblyMappingInfo> _assemblyMappings;
        private static readonly Uri DefaultBaseUri = new Uri("http://brightstardb.com/namespaces/default/");

        /// <summary>
        /// Creates a new provider instance
        /// </summary>
        public ReflectionMappingProvider()
        {
            _assemblyMappings = new Dictionary<string, AssemblyMappingInfo>();
        }

        /// <summary>
        /// Processes a <see cref="EntityContext"/> instance to extract mapping details and uses them to populate
        /// a <see cref="EntityMappingStore"/>
        /// </summary>
        /// <param name="mappingStore">The <see cref="EntityMappingStore"/> to be populated</param>
        /// <param name="context">The <see cref="EntityContext"/> to be processed</param>
        public void AddMappingsForContext(EntityMappingStore mappingStore, EntityContext context)
        {
            var contextType = context.GetType();
            var contextAssembly = contextType.GetTypeInfo().Assembly;
            AssemblyMappingInfo assemblyMappings;
            if (!_assemblyMappings.TryGetValue(contextAssembly.FullName, out assemblyMappings))
            {
                assemblyMappings = GetAssemblyMappingInfo(contextAssembly);
                _assemblyMappings[contextAssembly.FullName] = assemblyMappings;
            }
            var queryableGeneric = typeof (IQueryable<object>).GetGenericTypeDefinition();
            foreach(var p in contextType.GetProperties())
            {
                if (p.PropertyType.IsGenericType() &&
                    queryableGeneric.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition()))
                {
                    var genericParam = p.PropertyType.GetGenericArguments()[0];
                    AddMappingsForType(mappingStore, genericParam);
                }
            }
        }

        /// <summary>
        /// Populates a <see cref="EntityMappingStore"/> with the entity mapping and property mapping information 
        /// found on an entity implementation type
        /// </summary>
        /// <param name="mappingStore">The <see cref="EntityMappingStore"/> to be updated</param>
        /// <param name="mappedType">The entity implementation type to be processed</param>
        public void AddMappingsForType(EntityMappingStore mappingStore, Type mappedType)
        {
            var mappedTypeAssembly = mappedType.GetTypeInfo().Assembly;
            AssemblyMappingInfo assemblyMappings;
            if (!_assemblyMappings.TryGetValue(mappedTypeAssembly.FullName, out assemblyMappings))
            {
                assemblyMappings = GetAssemblyMappingInfo(mappedTypeAssembly);
                _assemblyMappings[mappedTypeAssembly.FullName] = assemblyMappings;
            }
            AddMappingsForType(mappingStore, assemblyMappings, mappedType);
        }

        /// <summary>
        /// Processes the specified assembly for entity implementation types and adds all mapping information 
        /// to the specified mapping store
        /// </summary>
        /// <param name="mappingStore">The <see cref="EntityMappingStore"/> to be updated</param>
        /// <param name="assembly">The assembly to be processed</param>
        public void AddMappingsForAssembly(EntityMappingStore mappingStore, Assembly assembly)
        {
            AssemblyMappingInfo assemblyMappings;
            if (!_assemblyMappings.TryGetValue(assembly.FullName, out assemblyMappings))
            {
                assemblyMappings = GetAssemblyMappingInfo(assembly);
                _assemblyMappings[assembly.FullName] = assemblyMappings;
            }
            foreach (var t in assembly.GetTypes())
            {
                AddMappingsForType(mappingStore, assemblyMappings, t);
            }
        }

        private static void AddMappingsForType(EntityMappingStore mappingStore, AssemblyMappingInfo assemblyMappingInfo, Type mappedType)
        {
            var entityAttribute =
                mappedType.GetTypeInfo().GetCustomAttributes(typeof (EntityAttribute), false).OfType<EntityAttribute>().
                    FirstOrDefault();
            
            if (entityAttribute != null)
            {
                var entityTypeIdentifier = entityAttribute.Identifier ?? GetImplTypeName(mappedType);

                mappingStore.SetTypeMapping(mappedType, assemblyMappingInfo.ResolveIdentifier(entityTypeIdentifier));
                var identityProperty = GetIdentityProperty(mappedType);
                if (identityProperty != null)
                {
                    var identifierAttr =
                        identityProperty.GetCustomAttributes(typeof (IdentifierAttribute), true).Cast
                            <IdentifierAttribute>().FirstOrDefault();
                    var identityInfo = GetIdentityInfo(assemblyMappingInfo, mappedType, identityProperty, identifierAttr);
                    mappingStore.SetIdentityInfo(mappedType, identityInfo);
                    mappingStore.SetPropertyHint(identityProperty, new PropertyHint(PropertyMappingType.Id));
                }
                else
                {
                    // Entity has no explicit Id property so use the defaults
                    var identityInfo = GetIdentityInfo(assemblyMappingInfo, mappedType, null, null);
                    mappingStore.SetIdentityInfo(mappedType, identityInfo);
                }

                foreach (var p in mappedType.GetProperties())
                {
                    if (p.Equals(identityProperty))
                    {
                        // Identity property is already mapped (above)
                        continue;
                    }

                    foreach (var attr in p.GetCustomAttributes(false))
                    {
                        /*
                        if (attr is IdentifierAttribute)
                        {
                            mappingStore.SetPropertyHint(p, new PropertyHint(PropertyMappingType.Address));
                            var idAttr = attr as IdentifierAttribute;
                            if (idAttr.BaseAddress != null)
                            {
                                mappingStore.SetIdentifierPrefix(mappedType, assemblyMappingInfo.ResolveIdentifier(idAttr.BaseAddress));
                            }
                        }
                        else*/
                        if (attr is PropertyTypeAttribute)
                        {
                            var propertyUri =
                                assemblyMappingInfo.ResolveIdentifier((attr as PropertyTypeAttribute).Identifier);
                            mappingStore.SetPropertyHint(p,
                                                         IsResource(p.PropertyType)
                                                             ? new PropertyHint(PropertyMappingType.Arc, propertyUri)
                                                             : new PropertyHint(PropertyMappingType.Property,
                                                                                propertyUri));
                        }
                        else if (attr is InversePropertyTypeAttribute)
                        {
                            var propertyUri =
                                assemblyMappingInfo.ResolveIdentifier((attr as InversePropertyTypeAttribute).Identifier);
                            var targetType = p.PropertyType;
                            if (targetType.IsGenericType())
                            {
                                targetType = targetType.GetGenericArguments().First();
                            }
                            if (targetType.GetTypeInfo().GetCustomAttributes(typeof(EntityAttribute), false).Any())
                            {
                                mappingStore.SetPropertyHint(p,
                                                             new PropertyHint(PropertyMappingType.InverseArc,
                                                                              propertyUri));
                            }
                            else
                            {
                                throw new ReflectionMappingException(
                                    String.Format(
                                        "The property '{0}' on type '{1}' is marked with the InverseProperty attribute but its referenced type ('{2}') is not marked with a Entity attribute.",
                                        p.Name, mappedType.FullName, p.PropertyType.FullName));
                            }
                        }
                        else if (attr is InversePropertyAttribute)
                        {
                            var inversePropertyAttr = attr as InversePropertyAttribute;
                            var targetType = p.PropertyType;
                            if (targetType.IsGenericType()) targetType = targetType.GetGenericArguments().First();
                            if (!targetType.GetTypeInfo().GetCustomAttributes(typeof(EntityAttribute), true).Any())
                            {
                                throw new ReflectionMappingException(
                                    String.Format(
                                        "The type of property '{0}' on interface '{1}' is not marked with an Entity attribute.",
                                        p.Name, mappedType.FullName));
                            }
                            var forwardProperty = targetType.GetProperty(inversePropertyAttr.InversePropertyName);
                            if (forwardProperty == null)
                            {
                                throw new ReflectionMappingException(String.Format("The property '{0}' does not exist on type '{1}'.", inversePropertyAttr.InversePropertyName, targetType.FullName));
                            }
                            var inversePropertyTypeUri =
                                assemblyMappingInfo.ResolveIdentifier(GetForwardPropertyTypeUri(forwardProperty, p));
                            mappingStore.SetPropertyHint(p, new PropertyHint(PropertyMappingType.InverseArc, inversePropertyTypeUri));
                        }
                    }
                    if (mappingStore.GetPropertyHint(p) == null)
                    {
                        // If there has been no mapping at all, then we create a property mapping
                        var propertyName = Char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1);
                        var propertyUri = assemblyMappingInfo.ResolveIdentifier(propertyName);

                        mappingStore.SetPropertyHint(p,
                            IsResource(p.PropertyType) ? 
                            new PropertyHint(PropertyMappingType.Arc, propertyUri) : 
                            new PropertyHint(PropertyMappingType.Property, propertyUri));
                    }
                }
            }
        }

        private static IdentityInfo GetIdentityInfo(AssemblyMappingInfo assemblyMappingInfo, Type entityType,
            PropertyInfo identityProperty, IdentifierAttribute identifierAttr)
        {
            string baseUri = identifierAttr == null || identifierAttr.BaseAddress == null
                                 ? Constants.GeneratedUriPrefix
                                 : assemblyMappingInfo.ResolveIdentifier(identifierAttr.BaseAddress);
            if (identifierAttr != null && identifierAttr.KeyProperties != null && identifierAttr.KeyProperties.Length > 0)
            {
                var keyProperties = new PropertyInfo[identifierAttr.KeyProperties.Length];
                for (var i = 0; i < identifierAttr.KeyProperties.Length; i++)
                {
                    var property = entityType.GetProperty(identifierAttr.KeyProperties[i]);
                    if (property == null)
                    {
                        throw new ReflectionMappingException(
                            String.Format("Could not find key property {0} on type {1}.", identifierAttr.KeyProperties[i], entityType.FullName));
                    }
                    keyProperties[i] = property;
                }
                IKeyConverter keyConverter = null;
                if (identifierAttr.KeyConverterType != null)
                {
                    keyConverter = Activator.CreateInstance(identifierAttr.KeyConverterType) as IKeyConverter;
                    if (keyConverter == null)
                    {
                        throw new ReflectionMappingException(
                            String.Format("Could not instantiate type {0} as a key converter for entity type {1}. Ensure that this type implements the IKeyConverter interface.",
                                          identifierAttr.KeyConverterType.FullName, entityType.FullName));
                    }
                }
                return new IdentityInfo(baseUri, identityProperty, keyProperties,
                                        identifierAttr.KeySeparator ?? Constants.DefaultKeySeparator,
                                        keyConverter ?? new DefaultKeyConverter());
            }
            return new IdentityInfo(baseUri, identityProperty, null, null, null);
        }

        private static string GetForwardPropertyTypeUri(PropertyInfo property, PropertyInfo inverseProperty)
        {
            if (property.GetCustomAttributes(typeof(InversePropertyAttribute), false).Any())
            {
                throw new ReflectionMappingException(
                    String.Format(
                        "Not allowed to map a property decorated with InversePropertyAttribute to another property decorated with the InversePropertyAttribute. Check the attributes of the property {0} on type {1} and the property {2} on type {3}.",
                        property.Name, property.DeclaringType.FullName, inverseProperty.Name,
                        inverseProperty.DeclaringType.FullName));
            }
            var propertyTypeAttr =
                property.GetCustomAttributes(typeof (PropertyTypeAttribute), false).OfType<PropertyTypeAttribute>().
                    FirstOrDefault();
            if (propertyTypeAttr == null)
            {
                return Char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
            }
            return propertyTypeAttr.Identifier;
        }

        /// <summary>
        /// Returns true if the specified type is flagged as a resource
        /// </summary>
        /// <param name="type"></param>
        private static bool IsResource(Type type)
        {
            var targetType = type;
            if (targetType.IsGenericType() && !targetType.GetTypeInfo().ContainsGenericParameters)
            {
                targetType = targetType.GetGenericArguments().FirstOrDefault();
            }
            return 
                targetType.GetTypeInfo().GetCustomAttributes(typeof(EntityAttribute), false).Any();
        }

        private static string GetImplTypeName(Type type)
        {
            return type.Name.StartsWith("I") ? type.Name.Substring(1) : type.Name;
        }

        private static PropertyInfo GetIdentityProperty(Type type)
        {
            var properties = GetPublicProperties(type);
            string identityPrefix = type.Name.StartsWith("I") ? type.Name.Substring(1) : type.Name;
            var identityProperty =
                properties.FirstOrDefault(p =>
                                          p.GetCustomAttributes(typeof (IdentifierAttribute), true)
                                           .OfType<IdentifierAttribute>().Any());
            if (identityProperty == null) identityProperty = properties.FirstOrDefault(p=>p.Name.Equals(identityPrefix + "Id"));
            if (identityProperty == null) identityProperty = properties.FirstOrDefault(p=>p.Name.Equals(identityPrefix + "ID"));
            if (identityProperty == null) identityProperty = properties.FirstOrDefault(p=>p.Name.Equals("Id"));
            if (identityProperty == null) identityProperty = properties.FirstOrDefault(p=>p.Name.Equals("ID"));
            if (identityProperty != null)
            {
                ValidateIdentityProperty(identityProperty);
            }
            return identityProperty;
        }


        private static PropertyInfo[] GetPublicProperties(Type type)
        {
            if (type.GetTypeInfo().IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }
            return type.GetProperties(BindingFlags.FlattenHierarchy
                                      | BindingFlags.Public | BindingFlags.Instance);
        }

        private static void ValidateIdentityProperty(PropertyInfo identityProperty)
        {
            if (!identityProperty.PropertyType.Equals(typeof(string)) ||
                identityProperty.CanWrite)
            {
                throw new InvalidIdentityPropertyException(identityProperty);
            }
        }

        private static AssemblyMappingInfo GetAssemblyMappingInfo(Assembly assembly)
        {
            var ret = new AssemblyMappingInfo();
            var baseIdentifierAttr =
                assembly.GetCustomAttributes(typeof (TypeIdentifierPrefixAttribute)).OfType<TypeIdentifierPrefixAttribute>().
                    FirstOrDefault();
            if (baseIdentifierAttr != null)
            {
                ret.BaseUri = new Uri(baseIdentifierAttr.BaseUri);
            }
            else
            {
                ret.BaseUri = DefaultBaseUri;
            }

            foreach(var prefixAttr in assembly.GetCustomAttributes(typeof(NamespaceDeclarationAttribute)).OfType<NamespaceDeclarationAttribute>())
            {
                ret.PrefixMappings[prefixAttr.Prefix] = prefixAttr.Reference;
            }
            return ret;
        }

        class AssemblyMappingInfo
        {
            public Uri BaseUri { get; set; }
            public Dictionary<string, string> PrefixMappings { get; private set; }

            internal AssemblyMappingInfo()
            {
                PrefixMappings = new Dictionary<string, string>();
            }

            public string ResolveIdentifier(string identifier)
            {
                // An explicit empty string identifier always ignores the global base URI
                if (String.Empty.Equals(identifier)) return String.Empty;
                var ix = identifier.IndexOf(':');
                if (ix > 0 && ix < identifier.Length - 1)
                {
                    var prefix = identifier.Substring(0, ix);
                    if (PrefixMappings.ContainsKey(prefix))
                    {
                        return PrefixMappings[prefix] + identifier.Substring(ix + 1);
                    }
                }
                else if (BaseUri != null)
                {
                    var resolvedUri = new Uri(BaseUri, identifier);
                    return resolvedUri.ToString();
                }
                return identifier;
            }
        }
    }
}
