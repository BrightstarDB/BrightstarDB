using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BrightstarDB.Azure.Management;
using BrightstarDB.Client;

namespace BrightstarDB.Azure.Gateway
{
    public class AccessLevelAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly StoreAccessLevel _requiredAccessLevel;
        public AccessLevelAuthorizeAttribute(StoreAccessLevel requiredAccessLevel)
        {
            _requiredAccessLevel = requiredAccessLevel;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (_requiredAccessLevel == StoreAccessLevel.None) return true;
            SignatureType sigType;
            string accountId;
            string signature;
            if (RestClientHelper.GetAuthorizationInfo(httpContext.Request, out sigType, out accountId, out signature))
            {
                if (ValidateDate(httpContext.Request))
                {
                    var accessAccount = GetAccessAccount(accountId);
                    if (accessAccount != null)
                    {
                        httpContext.Request.RequestContext.RouteData.Values.Add("_accessAccount", accessAccount);
                        if (ValidateSignature(httpContext.Request, accessAccount, sigType, signature) &&
                            ValidateAccessPrivileges(httpContext.Request, accessAccount))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool ValidateDate(HttpRequestBase request)
        {
            var szDate = request.Headers["Date"];
            DateTime requestDate;
            if (!String.IsNullOrEmpty(szDate))
            {
                if (DateTime.TryParse(szDate, out requestDate))
                {
                    if (requestDate.ToUniversalTime().AddMinutes(15.0) >= DateTime.UtcNow)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private AccountDetails GetAccessAccount(string accountId)
        {
            var accRepo = AccountsRepositoryFactory.GetAccountsRepository();
            AccountDetails accessAccount;
            if ((_requiredAccessLevel & StoreAccessLevel.Superuser) == StoreAccessLevel.Superuser)
            {
                accessAccount = accRepo.GetSuperUserAccount();
            }
            else
            {
                accessAccount = accRepo.GetAccountDetails(accountId);
            }
            return accessAccount;
        }


        private static bool ValidateSignature(HttpRequestBase request, AccountDetails accessAccount, SignatureType sigType, string signature)
        {
            if (accessAccount != null)
            {
                if (signature.Equals(RestClientHelper.GenerateSignature(request, sigType,
                                                                        accessAccount.PrimaryKey)) ||
                    signature.Equals(RestClientHelper.GenerateSignature(request, sigType,
                                                                        accessAccount.SecondaryKey)))
                {
                    return true;
                }
            }
            return false;
        }

        private bool ValidateAccessPrivileges(HttpRequestBase request, AccountDetails accessAccount)
        {
            if (_requiredAccessLevel == StoreAccessLevel.Superuser)
            {
                // Already validated signature against super user credentials
                return true;
            }

            var routeData = request.RequestContext.RouteData;
            object tmp;
            if (routeData.Values != null && routeData.Values.TryGetValue("subscription", out tmp))
            {
                var subscriptionId = tmp.ToString();
                if (_requiredAccessLevel == StoreAccessLevel.Owner)
                {
                    // Owner access is checked against the subscription - the access account must own the subscription being accessed.
                    return accessAccount.Subscriptions.Any(s => s.Id.Equals(subscriptionId));
                }
                if (routeData.Values.TryGetValue("storeId", out tmp))
                {
                    var storeId = tmp.ToString();
                    var accRepo = AccountsRepositoryFactory.GetAccountsRepository();
                    var accessKey = accRepo.GetAccountAccessKey(accessAccount.AccountId, storeId);
                    if (accessKey != null && (accessKey.Access & _requiredAccessLevel) > 0)
                    {
                        routeData.Values["_accessLevel"] = accessKey.Access;
                        return true;
                    }
                } else
                {
                    // No store id - this should only be for listing stores
                    return true;
                }
            }
            return false;
        }

    }
}