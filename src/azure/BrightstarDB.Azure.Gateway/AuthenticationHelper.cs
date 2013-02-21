using System.Linq;
using Microsoft.IdentityModel.Claims;

namespace BrightstarDB.Azure.Gateway
{
    internal static class AuthenticationHelper
    {
        public static string GetAuthenticatedUserToken(ClaimsIdentity identity)
        {
            var provider =
                identity.Claims.Where(
                    c =>
                    c.ClaimType.Equals(
                        "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider")).Select(
                            c => c.Value).FirstOrDefault();
            var nameidentity =
                identity.Claims.Where(
                    c => c.ClaimType.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")).
                    Select(c => c.Value).FirstOrDefault();

            if (provider == null || nameidentity == null) return null;
            return provider.ToLowerInvariant() + ":" + nameidentity;
        }
    }
}