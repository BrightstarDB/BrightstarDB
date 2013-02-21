using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Data.Services;
using System.Linq;
using System.Reflection;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData
{
    ///<summary>
    /// Exposes a BrightstarEntity Context and OData Data Service.
    ///</summary>
    ///<typeparam name="T">The Brightstar Entity Context Type</typeparam>
    public abstract class EntityDataService<T> : DataService<T>, IServiceProvider where T : BrightstarEntityContext 
    {
        private readonly DataServiceMetadataProvider _metadata;
        private readonly DataServiceQueryProvider<T> _query;

        /// <summary>
        /// Creates a new entity data service. This is the default constructor called by the ServiceHost.
        /// </summary>
        protected EntityDataService()
        {
            _metadata = GetMetadataProvider(typeof(T));
            _query = GetQueryProvider(_metadata);
        }

        /// <summary>
        /// Gets a specified service based on the service type.
        /// </summary>
        /// <param name="serviceType">The type of requested service</param>
        /// <returns>Service requested or null, or specifically not implemented.</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IDataServiceMetadataProvider))
            {
                return _metadata;
            }
            else if (serviceType == typeof(IDataServiceQueryProvider))
            {
                return _query;
            } else if (serviceType == typeof(IDataServiceUpdateProvider))
            {
                throw new NotImplementedException("Update is not supported");
            }
            
            else
            {
                return null;
            }      
        }

        private DataServiceQueryProvider<T> GetQueryProvider(DataServiceMetadataProvider metadata)
        {
            return new DataServiceQueryProvider<T>(_metadata); 
        }

        // This is all static analysis so cache what we find 
        private static DataServiceMetadataProvider _mdprovider;

        private static DataServiceMetadataProvider GetMetadataProvider(Type dataSourceType)
        {
            if (_mdprovider != null) return _mdprovider;

            var exposedTypes = GetExposedTypes(dataSourceType);
            var collectionNames = GetCollectionNames(dataSourceType);
            var mdprovider = new DataServiceMetadataProvider(collectionNames);
            var resourceTypes = new Dictionary<string, ResourceType>();

            foreach (var exposedType in exposedTypes)
            {
                // create type
                var odataType = new ResourceType(exposedType, ResourceTypeKind.EntityType, null, "BrightstarEntities",
                                                 NormaliseName(exposedType.Name), false);
                resourceTypes.Add(NormaliseName(exposedType.Name), odataType);

                // create primitive properties
                foreach (var propertyInfo in exposedType.GetProperties())
                {
                    var name = propertyInfo.Name;
                    if (!propertyInfo.PropertyType.IsGenericType && IsLiteral(propertyInfo))
                    {
                        if (propertyInfo.Name.ToLower().Equals("id"))
                        {
                            var idProperty = new ResourceProperty(
                                               propertyInfo.Name,
                                               ResourcePropertyKind.Key |
                                               ResourcePropertyKind.Primitive,
                                               ResourceType.GetPrimitiveResourceType(typeof(string))
                            );    
                            odataType.AddProperty(idProperty);
                        } else
                        {
                            var valueProperty = new ResourceProperty(
                                               propertyInfo.Name,
                                               ResourcePropertyKind.Primitive,
                                               ResourceType.GetPrimitiveResourceType(propertyInfo.PropertyType)
                            );
                            odataType.AddProperty(valueProperty);
                        }
                    }
                    else
                    {
                        if(IsNullableLiteral(propertyInfo))
                        {
                            var valueProperty = new ResourceProperty(
                                               propertyInfo.Name,
                                               ResourcePropertyKind.Primitive,
                                               ResourceType.GetPrimitiveResourceType(propertyInfo.PropertyType)
                            );
                            odataType.AddProperty(valueProperty);
                        }
                    }
                }
            }

            // make resource sets
            var resourceSets = new Dictionary<string, ResourceSet>();
            foreach (var resourceType in resourceTypes.Values)
            {
                resourceSets.Add(resourceType.Name, new ResourceSet(resourceType.Name, resourceType));                               
            }

            var associationSets = new Dictionary<string, ResourceAssociationSet>();

            foreach (var exposedType in exposedTypes)
            {                 
                foreach (var propertyInfo in exposedType.GetProperties())
                {
                    // ignore inverse properties as they get sorted out later.
                    if(IsInverseProperty(propertyInfo)) continue;

                    if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.Name.StartsWith("ICollection"))
                    {
                        var referencedType = propertyInfo.PropertyType.GetGenericArguments()[0];

                        var referencedTypeName = NormaliseName(referencedType.Name);
                        // skip non entity collections
                        if(!resourceTypes.ContainsKey(referencedTypeName)) continue;

                        // add reference set relationship
                        var referenceSetProperty = new ResourceProperty(
                                                        propertyInfo.Name,
                                                        ResourcePropertyKind.ResourceSetReference,
                                                        resourceTypes[NormaliseName(referencedType.Name)]);
                        
                        resourceTypes[NormaliseName(exposedType.Name)].AddProperty(referenceSetProperty);

                        var inversePropertyInfo = FindInversePropertyFor(propertyInfo);
                        if (inversePropertyInfo == null)
                        {
                            var assocSet = new ResourceAssociationSet(propertyInfo.Name,
                                                                      new ResourceAssociationSetEnd(
                                                                          resourceSets[NormaliseName(exposedType.Name)],
                                                                          resourceTypes[NormaliseName(exposedType.Name)],
                                                                          referenceSetProperty),
                                                                      new ResourceAssociationSetEnd(
                                                                          resourceSets[NormaliseName(referencedType.Name)],
                                                                          resourceTypes[NormaliseName(referencedType.Name)],
                                                                          null));
                            referenceSetProperty.CustomState = assocSet;
                        } else
                        {
                            // create the inverse resource property
                            ResourceProperty inverseReferenceProperty = null;
                            if (inversePropertyInfo.PropertyType.IsGenericType)
                            {
                                inverseReferenceProperty = new ResourceProperty(
                                                                    inversePropertyInfo.Name,
                                                                    ResourcePropertyKind.ResourceSetReference,
                                                                    resourceTypes[NormaliseName(inversePropertyInfo.PropertyType.GetGenericArguments()[0].Name)]);
                            }
                            else
                            {
                                inverseReferenceProperty = new ResourceProperty(
                                                                    inversePropertyInfo.Name,
                                                                    ResourcePropertyKind.ResourceReference,
                                                                    resourceTypes[NormaliseName(inversePropertyInfo.PropertyType.Name)]);
                            }

                            resourceTypes[NormaliseName(inversePropertyInfo.ReflectedType.Name)].AddProperty(inverseReferenceProperty);

                            // we need to create the other property as well as the association
                            var assocSet = new ResourceAssociationSet(referenceSetProperty.Name + "_" + inverseReferenceProperty.Name,
                                                                      new ResourceAssociationSetEnd(
                                                                          resourceSets[NormaliseName(exposedType.Name)],
                                                                          resourceTypes[NormaliseName(exposedType.Name)],
                                                                          referenceSetProperty),
                                                                      new ResourceAssociationSetEnd(
                                                                          resourceSets[NormaliseName(referencedType.Name)],
                                                                          resourceTypes[NormaliseName(referencedType.Name)],
                                                                          inverseReferenceProperty));
                            referenceSetProperty.CustomState = assocSet;
                            inverseReferenceProperty.CustomState = assocSet;

                        }
                    }
                    else if (resourceTypes.Keys.Contains(NormaliseName(propertyInfo.PropertyType.Name)))
                    {
                        var referenceProperty = new ResourceProperty(
                                                        propertyInfo.Name,
                                                        ResourcePropertyKind.ResourceReference,
                                                        resourceTypes[NormaliseName(propertyInfo.PropertyType.Name)]);
                        resourceTypes[NormaliseName(exposedType.Name)].AddProperty(referenceProperty);
                        var inversePropertyInfo = FindInversePropertyFor(propertyInfo);
                        if (inversePropertyInfo == null)
                        {
                            var assocSet = new ResourceAssociationSet(referenceProperty.Name,
                                                                      new ResourceAssociationSetEnd(
                                                                          resourceSets[NormaliseName(exposedType.Name)],
                                                                          resourceTypes[NormaliseName(exposedType.Name)],
                                                                          referenceProperty),
                                                                      new ResourceAssociationSetEnd(
                                                                          resourceSets[NormaliseName(propertyInfo.PropertyType.Name)],
                                                                          resourceTypes[NormaliseName(propertyInfo.PropertyType.Name)],
                                                                          null));
                            referenceProperty.CustomState = assocSet;
                        }
                        else
                        {
                            // create the inverse resource property
                            ResourceProperty inverseReferenceProperty = null;

                            if (inversePropertyInfo.PropertyType.IsGenericType)
                            {
                                inverseReferenceProperty = new ResourceProperty(
                                                                    inversePropertyInfo.Name,
                                                                    ResourcePropertyKind.ResourceSetReference,
                                                                    resourceTypes[NormaliseName(inversePropertyInfo.PropertyType.GetGenericArguments()[0].Name)]);                                
                            } else
                            {
                                inverseReferenceProperty = new ResourceProperty(
                                                                    inversePropertyInfo.Name,
                                                                    ResourcePropertyKind.ResourceReference,
                                                                    resourceTypes[NormaliseName(inversePropertyInfo.PropertyType.Name)]);                                                                
                            }

                            resourceTypes[NormaliseName(inversePropertyInfo.ReflectedType.Name)].AddProperty(inverseReferenceProperty);

                            // we need to create the other property as well as the association
                            var assocSet = new ResourceAssociationSet(referenceProperty.Name + "_" + inverseReferenceProperty.Name,
                                                                      new ResourceAssociationSetEnd(
                                                                          resourceSets[NormaliseName(exposedType.Name)],
                                                                          resourceTypes[NormaliseName(exposedType.Name)],
                                                                          referenceProperty),
                                                                      new ResourceAssociationSetEnd(
                                                                          resourceSets[NormaliseName(propertyInfo.PropertyType.Name)],
                                                                          resourceTypes[NormaliseName(propertyInfo.PropertyType.Name)],
                                                                          inverseReferenceProperty));
                            referenceProperty.CustomState = assocSet;
                            inverseReferenceProperty.CustomState = assocSet;
                        }
                    }
                }
            }


            foreach (var resourceAssociationSet in associationSets.Values)
            {
                mdprovider.AddAssociationSet(resourceAssociationSet);
            }

            foreach (var resourceType in resourceTypes.Values)
            {
                mdprovider.AddResourceType(resourceType);
                mdprovider.AddResourceSet(resourceSets[resourceType.Name]);                 
            }

            _mdprovider = mdprovider;
            return _mdprovider;
        }

        private static bool IsInverseProperty(PropertyInfo propertyInfo)
        {
            var customAttributes = propertyInfo.GetCustomAttributes(true);
            if (customAttributes.Count() > 0)
            {
                //check if inverse
                return customAttributes.Any(ca => ca is InversePropertyAttribute || ca is InversePropertyTypeAttribute);
            }
            return false;
        }

        private static string NormaliseName(string name)
        {
            if (name.StartsWith("I")) return name.Substring(1);
            return name;
        }

        private static PropertyInfo FindInversePropertyFor(PropertyInfo propertyInfo)
        {
            Type referencedType;
            if (propertyInfo.PropertyType.IsGenericType)
            {
                 referencedType = propertyInfo.PropertyType.GetGenericArguments()[0];
            } else
            {
                referencedType = propertyInfo.PropertyType;
            }

            // check for inverse property
            foreach (var property in referencedType.GetProperties())
            {
                var attributes = property.GetCustomAttributes(typeof (InversePropertyAttribute), false);
                if (attributes.Count() == 0) continue;

                var inverseAttribute = attributes[0] as InversePropertyAttribute;
                if (inverseAttribute.InversePropertyName.Equals(propertyInfo.Name))
                {
                    return property;
                }
            }

            return null;
        }

       
        private static bool IsLiteral(PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.Equals(typeof(byte)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(sbyte)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(SByte)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(Byte)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(string)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(Int32)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(Int64)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(DateTime)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(double)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(decimal)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(float)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(short)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(bool)))
            {
                return true;
            }
            return false;
        }

        private static bool IsNullableLiteral(PropertyInfo propertyInfo)
        {
            if (propertyInfo.PropertyType.Equals(typeof(byte?)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(Byte?)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(DateTime?)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(int?)))
            {
                return true;
            } if (propertyInfo.PropertyType.Equals(typeof(bool?)))
            {
                return true;
            }
            return false;
        }

        private static IEnumerable<Type> GetExposedTypes(Type context)
        {
            var types = new List<Type>();

            foreach (var propertyInfo in context.GetProperties())
            {
                var propertyType = propertyInfo.PropertyType;
                var genericArguments = propertyType.GetGenericArguments();

                if (propertyType.IsGenericType &&  propertyType.Name.StartsWith("IEntitySet"))
                {
                    if (genericArguments.Count() == 1)
                    {
                        var concreteType = genericArguments[0];
                        types.Add(concreteType);
                        //foreach (var efinterface in concreteType.GetInterfaces())
                        //{
                        //    if (efinterface.Name.Substring(1).Equals(concreteType.Name))
                        //    {
                        //        types.Add(efinterface);
                        //    }                            
                        //}
                    }
                }
            }

            return types;
        }

        /// <summary>
        /// Returns a dictionary that maps type names to the property on the context that has the relevant IQueryable.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static Dictionary<string, PropertyInfo> GetCollectionNames(Type context)
        {
            var collectionNames = new Dictionary<string, PropertyInfo>();

            foreach (var propertyInfo in context.GetProperties())
            {
                var propertyType = propertyInfo.PropertyType;
                var genericArguments = propertyType.GetGenericArguments();

                if (propertyType.IsGenericType && propertyType.Name.StartsWith("IEntitySet"))
                {
                    collectionNames.Add(genericArguments[0].Name.Substring(1), propertyInfo);
                }
            }

            return collectionNames;
        }

    }
}