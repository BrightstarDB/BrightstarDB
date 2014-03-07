using System.Collections.Generic;
using Nancy.Security;

namespace BrightstarDB.Server.Modules.Tests
{
    public class MockUserIdentity : IUserIdentity
    {
        private readonly string _name;
        private readonly string[] _claims;

        public MockUserIdentity(string userName, string[] claims)
        {
            _name = userName;
            _claims = claims;
        }


        public string UserName
        {
            get { return _name; }
        }

        public IEnumerable<string> Claims { get { return _claims; } }
    }
}