using System;
using System.Collections.Generic;
using System.Web.Security;
using Nancy.Security;

namespace BrightstarDB.Server.AspNet.Authentication
{
    public class MembershipUserIdentity : IUserIdentity
    {
        private readonly MembershipUser _user;
        public MembershipUserIdentity(MembershipUser user)
        {
            _user = user;
        }

        public string UserName
        {
            get { return _user.UserName; }
        }

        public IEnumerable<string> Claims 
        {
            get
            {
                try
                {
                    return Roles.GetRolesForUser(_user.UserName);
                }
                catch (Exception)
                {
                    return new string[0];
                }
            }
        }
    }
}