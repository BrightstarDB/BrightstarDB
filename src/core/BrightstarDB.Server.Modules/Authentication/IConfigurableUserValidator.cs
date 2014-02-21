using System.Xml;
using Nancy.Authentication.Basic;

namespace BrightstarDB.Server.Modules.Authentication
{
    public interface IConfigurableUserValidator : IUserValidator
    {
        void Configure(XmlElement configurationElement);
    }
}