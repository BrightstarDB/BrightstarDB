using System;
using System.Collections.Generic;
using System.Web.Security;
using Nancy.Security;

namespace BrightstarDB.Server.AspNet.Authentication
{
    public class MembershipUserIdentity : IUserIdentity
    {
        private readonly MembershipUser _user;
        private readonly string[] _roles;
 
        public MembershipUserIdentity(MembershipUser user)
        {
            _user = user;
            if (Roles.Enabled)
            {
                _roles = Roles.GetRolesForUser(user.UserName);
            }
        }

        public string UserName
        {
            get { return _user.UserName; }
        }

        public IEnumerable<string> Claims 
        {
            get { return _roles; }
        }

    }
}