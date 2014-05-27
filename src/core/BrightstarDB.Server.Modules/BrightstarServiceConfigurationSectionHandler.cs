using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Configuration;
using BrightstarDB.Server.Modules.Permissions;

namespace BrightstarDB.Server.Modules
{
    public class BrightstarServiceConfigurationSectionHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            var configuration = new BrightstarServiceConfiguration();

            var connectionStringAttr = section.Attributes["connectionString"];
            if (connectionStringAttr != null) configuration.ConnectionString = connectionStringAttr.Value;

            if (section is XmlElement)
            {
                var sectionEl = section as XmlElement;
                
                var authenticationProviders =
                    sectionEl.GetElementsByTagName("authenticationProviders").Item(0) as XmlElement;
                if (authenticationProviders != null)
                {
                    configuration.AuthenticationProviders = ProcessAuthenticationProviders(authenticationProviders);
                }

                var storePermissions = sectionEl.GetElementsByTagName("storePermissions").Item(0) as XmlElement;
                if (storePermissions != null)
                {
                    configuration.StorePermissionsProvider = ProcessStorePermissions(storePermissions);
                }
                
                var systemPermissions = sectionEl.GetElementsByTagName("systemPermissions").Item(0) as XmlElement;
                if (systemPermissions != null)
                {
                    configuration.SystemPermissionsProvider = ProcessSystemPermissions(systemPermissions);
                }

            }
            return configuration;
        }

        private static ICollection<IAuthenticationProvider> ProcessAuthenticationProviders(XmlElement authenticationProviders)
        {
            var providers = new List<IAuthenticationProvider>();
            foreach (
                var addElement in
                    authenticationProviders.GetElementsByTagName("add")
                                           .OfType<XmlElement>()
                                           .Where(x => x.HasAttribute("type")))
            {
                var typeRef = addElement.GetAttribute("type");
                var providerType = Type.GetType(typeRef, true);
                var provider = Activator.CreateInstance(providerType) as IAuthenticationProvider;
                if (provider != null)
                {
                    provider.Configure(addElement);
                    providers.Add(provider);
                }
            }
            return providers;
        }

        private AbstractSystemPermissionsProvider ProcessSystemPermissions(XmlElement systemPermissions)
        {
            var childElements = systemPermissions.ChildNodes.OfType<XmlElement>().ToList();
            if (childElements.Count != 1)
            {
                throw new ConfigurationErrorsException(String.Format("Expected a single element inside the '{0}' element", systemPermissions.Name));
            }
            return ProcessSystemPermissionsProvider(childElements[0]);
        }

        private AbstractSystemPermissionsProvider ProcessSystemPermissionsProvider(XmlElement providerElement)
        {
            switch (providerElement.LocalName)
            {
                case "combine":
                    var childProviderElements = providerElement.ChildNodes.OfType<XmlElement>().ToList();
                    if (childProviderElements.Count != 2)
                    {
                        throw new ConfigurationErrorsException(
                            "Expected exactly two children of the 'combine' element.");
                    }
                    return new CombiningSystemPermissionsProvider(
                        ProcessSystemPermissions(childProviderElements[0]),
                        ProcessSystemPermissions(childProviderElements[1]));

                case "fallback":
                    var authenticatedPermissions = GetSystemPermissionsAttributeValue(providerElement, "authenticated");
                    SystemPermissions anonymousPermissions;
                    return TryGetSystemPermissionsAttributeValue(providerElement, "anonymous", out anonymousPermissions)
                               ? new FallbackSystemPermissionsProvider(authenticatedPermissions, anonymousPermissions)
                               : new FallbackSystemPermissionsProvider(authenticatedPermissions);
                    
                case "static":
                    var staticPermissions = new StaticSystemPermissionsProvider(providerElement);
                    return staticPermissions;

                default:
                    throw new ConfigurationErrorsException(
                        String.Format(
                            "Unexecpted configuration element '{0}' inside element '{1}'.",
                            providerElement.Name, providerElement.ParentNode.Name));
            }
        }

        private AbstractStorePermissionsProvider ProcessStorePermissions(XmlElement storePermissions)
        {
            var childElements = storePermissions.ChildNodes.OfType<XmlElement>().ToList();
            if (childElements.Count != 1)
                throw new ConfigurationErrorsException("Expected a single child of the storePermissions element");
            return ProcessStorePermissionsProvider(childElements[0]);
        }

        private AbstractStorePermissionsProvider ProcessStorePermissionsProvider(XmlElement providerElement)
        {
            switch (providerElement.LocalName)
            {
                case "combine":
                    var childProviderElements = providerElement.ChildNodes.OfType<XmlElement>().ToList();
                    if (childProviderElements.Count != 2)
                    {
                        throw new ConfigurationErrorsException(
                            "Expected exactly two children of the combine element.");
                    }
                    return new CombiningStorePermissionsProvider(
                        ProcessStorePermissionsProvider(childProviderElements[0]),
                        ProcessStorePermissionsProvider(childProviderElements[1]));
                case "fallback":
                    var authPermissions = GetStorePermissionsAttributeValue(providerElement, "authenticated");
                    StorePermissions anonPermissions;
                    return TryGetStorePermissionsAttributeValue(providerElement, "anonymous", out anonPermissions)
                               ? new FallbackStorePermissionsProvider(authPermissions, anonPermissions)
                               : new FallbackStorePermissionsProvider(authPermissions);
                case "static":
                    var staticPermissions = new StaticStorePermissionsProvider(providerElement);
                    return staticPermissions;

                default:
                    throw new ConfigurationErrorsException(
                        "Unexecpted configuration element inside 'storePermissions' element. Cannot process element '" +
                        providerElement.LocalName + "'");
            }
        }

        
        private static StorePermissions GetStorePermissionsAttributeValue(XmlElement providerElement, string attrName)
        {
            var szAttrValue = providerElement.GetAttribute(attrName);
            if (string.IsNullOrEmpty(szAttrValue))
            {
                throw new ConfigurationErrorsException("Could not find required attribute '" + attrName + "' on element '" + providerElement.Name + "'.");
            }
            StorePermissions ret;
            if (!Enum.TryParse(szAttrValue, out ret))
            {
                throw new ConfigurationErrorsException(String.Format("Could not parse the value '{0}' of attribute '{1}' on element '{2}' as a store permissions flags.",
                    szAttrValue, attrName, providerElement.Name));
            }
            return ret;
        }

        public static bool TryGetStorePermissionsAttributeValue(XmlElement providerElement, string attrName, out StorePermissions storePermissions)
        {
            try
            {
                storePermissions = GetStorePermissionsAttributeValue(providerElement, attrName);
                return true;
            }
            catch (ConfigurationErrorsException)
            {
                storePermissions = StorePermissions.None;
                return false;
            }
        }

        private static SystemPermissions GetSystemPermissionsAttributeValue(XmlElement providerElement, string attrName)
        {
            var szAttrValue = providerElement.GetAttribute(attrName);
            if (string.IsNullOrEmpty(szAttrValue))
            {
                throw new ConfigurationErrorsException("Could not find required attribute '" + attrName + "' on element '" + providerElement.Name + "'.");
            }
            SystemPermissions ret;
            if (!Enum.TryParse(szAttrValue, out ret))
            {
                throw new ConfigurationErrorsException(String.Format("Could not parse the value '{0}' of attribute '{1}' on element '{2}' as a system permissions flags.",
                    szAttrValue, attrName, providerElement.Name));
            }
            return ret;
        }

        public static bool TryGetSystemPermissionsAttributeValue(XmlElement providerElement, string attrName,
                                                                  out SystemPermissions systemPermissions)
        {
            try
            {
                systemPermissions = GetSystemPermissionsAttributeValue(providerElement, attrName);
                return true;
            }
            catch (ConfigurationErrorsException)
            {
                systemPermissions = SystemPermissions.None;
                return false;
            }
        }

    }
}
