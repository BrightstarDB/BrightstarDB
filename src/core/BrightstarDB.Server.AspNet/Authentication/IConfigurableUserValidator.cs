using System.Xml;
using Nancy.Authentication.Basic;

namespace BrightstarDB.Server.AspNet.Authentication
{
    public interface IConfigurableUserValidator : IUserValidator
    {
        void Configure(XmlElement configurationElement);
    }
}