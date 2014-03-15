using System;
using System.Configuration;
using System.Linq;
using System.Xml;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;

namespace BrightstarDB.Server.Modules.Authentication
{
    public class BasicAuthAuthenticationProvider : IAuthenticationProvider
    {
        private BasicAuthenticationConfiguration _configuration;

        public BasicAuthAuthenticationProvider()
        {
            // Expects to be configured through a call to Configure
        }

        public BasicAuthAuthenticationProvider(BasicAuthenticationConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(XmlElement configurationElement)
        {
            var validatorEl = configurationElement.GetElementsByTagName("validator")
                                                  .OfType<XmlElement>().FirstOrDefault(x => x.HasAttribute("type"));
            
            if (validatorEl == null)
            {
                throw new ConfigurationErrorsException("Missing required validator element for Basic Authentication authentication provider.");
            }
            
            var validatorType = Type.GetType(validatorEl.GetAttribute("type"));
            if (validatorType == null)
            {
                throw new ConfigurationErrorsException(String.Format("Cannot resolve validator type '{0}'", validatorEl.GetAttribute("type")));
            }

            var userValidator = Activator.CreateInstance(validatorType) as IUserValidator;
            if (userValidator == null)
            {
                throw new ConfigurationErrorsException(String.Format("Type {0} does not implement the IUserValidator interface.", validatorType.FullName));
            }

            if (userValidator is IConfigurableUserValidator)
            {
                (userValidator as IConfigurableUserValidator).Configure(validatorEl);
            }

            var realmEl = configurationElement.GetElementsByTagName("realm").OfType<XmlElement>().FirstOrDefault();
            var realm = realmEl == null ? "BrightstarDB" : realmEl.InnerText;
            _configuration= new BasicAuthenticationConfiguration(userValidator, realm);
        }

        public void Enable(IPipelines pipelines)
        {
            BasicAuthentication.Enable(pipelines, _configuration);
        }
    }
}