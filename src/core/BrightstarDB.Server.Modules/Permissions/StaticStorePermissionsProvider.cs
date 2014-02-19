using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    internal class StaticStorePermissionsProvider : AbstractStorePermissionsProvider
    {
        private const string StoreEl = "store";
        private const string UserEl = "user";
        private const string ClaimEl = "claim";
        private const string PermissionsAttr = "permissions";
        private const string NameAttr = "name";

        private readonly Dictionary<string, Dictionary<string, StorePermissions>> _storeUsers;
        private readonly Dictionary<string, Dictionary<string, StorePermissions>> _storeClaims;
 
        public StaticStorePermissionsProvider(XmlNode configEl)
        {
            _storeUsers = new Dictionary<string, Dictionary<string, StorePermissions>>();
            _storeClaims = new Dictionary<string, Dictionary<string, StorePermissions>>();
            if (configEl == null) throw new ArgumentNullException("configEl");
            foreach (var el in configEl.ChildNodes.OfType<XmlElement>().Where(x=>x.LocalName.Equals(StoreEl)))
            {
                ProcessStorePermissions(el);
            }
        }

        private void ProcessStorePermissions(XmlElement storePermissionsElement)
        {
            if (!storePermissionsElement.HasAttribute(NameAttr))
            {
                return;
            }

            var storeName = storePermissionsElement.GetAttribute(NameAttr);
            Dictionary<string, StorePermissions> storeUsers, storeClaims;
            if (!_storeUsers.TryGetValue(storeName, out storeUsers))
            {
                storeUsers = new Dictionary<string, StorePermissions>();
                _storeUsers[storeName] = storeUsers;
            }
            if (!_storeClaims.TryGetValue(storeName, out storeClaims))
            {
                storeClaims = new Dictionary<string, StorePermissions>();
                _storeClaims[storeName] = storeClaims;
            }
            foreach (var el in storePermissionsElement.ChildNodes.OfType<XmlElement>())
            {
                if (el.LocalName.Equals(UserEl))
                {
                    ProcessPermissionsElement(el, storeUsers);
                }
                else if (el.LocalName.Equals(ClaimEl))
                {
                    ProcessPermissionsElement(el, storeClaims);
                }
            }
        }

        private static void ProcessPermissionsElement(XmlElement permissionsElement, IDictionary<string, StorePermissions> permissonsDict)
        {
            StorePermissions permissions;
            if (permissionsElement.HasAttribute(NameAttr) && permissionsElement.HasAttribute(PermissionsAttr) &&
                BrightstarServiceConfigurationSectionHandler.TryGetStorePermissionsAttributeValue(permissionsElement,
                                                                                                  PermissionsAttr,
                                                                                                  out permissions))
            {
                permissonsDict[permissionsElement.GetAttribute(NameAttr)] = permissions;
            }
        }

        public override StorePermissions GetStorePermissions(IUserIdentity currentUser, string storeName)
        {
            if (currentUser == null || !currentUser.IsAuthenticated())
            {
                return StorePermissions.None;
            }

            var calculatedPermissions = StorePermissions.None;
            if (!String.IsNullOrEmpty(currentUser.UserName))
            {
                // See if there are user-specific permissions
                Dictionary<string, StorePermissions> storeUserPermissions;
                if (_storeUsers.TryGetValue(storeName, out storeUserPermissions))
                {
                    StorePermissions userPermissions;
                    if (storeUserPermissions.TryGetValue(currentUser.UserName, out userPermissions))
                    {
                        calculatedPermissions |= userPermissions;
                    }
                }
            }

            Dictionary<string, StorePermissions> storeClaimPermissions;
            if (_storeClaims.TryGetValue(storeName, out storeClaimPermissions))
            {
                foreach (var claim in currentUser.Claims)
                {
                    StorePermissions claimPermissions;
                    if (storeClaimPermissions.TryGetValue(claim, out claimPermissions))
                    {
                        calculatedPermissions |= claimPermissions;
                    }
                }
            }
            return calculatedPermissions;
        }
    }
}
