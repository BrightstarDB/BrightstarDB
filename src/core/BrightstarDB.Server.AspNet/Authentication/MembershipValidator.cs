using System.Web.Security;
using Nancy.Authentication.Basic;
using Nancy.Security;

namespace BrightstarDB.Server.AspNet.Authentication
{
    public class MembershipValidator : IUserValidator
    {
        public IUserIdentity Validate(string username, string password)
        {
            return Membership.ValidateUser(username, password) ? new MembershipUserIdentity(Membership.GetUser(username)) : null;
        }
    }
}