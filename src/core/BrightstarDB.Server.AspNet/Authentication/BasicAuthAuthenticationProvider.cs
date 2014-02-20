using System;
using System.Configuration;
using System.Linq;
using System.Xml;
using BrightstarDB.Server.Modules.Authentication;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;

namespace BrightstarDB.Server.AspNet.Authentication
{
    public class BasicAuthAuthenticationProvider : IAuthenticationProvider
    {
        private BasicAuthenticationConfiguration _configuration;

        public void Configure(XmlElement configurationElement)
        {
            var validatorEl = configurationElement.GetElementsByTagName("validator")
                                                  .OfType<XmlElement>().FirstOrDefault(x => x.HasAttribute("type"));
            
            IUserValidator userValidator;
            if (validatorEl == null)
            {
                userValidator = new MembershipValidator();
            }
            else
            {
                var validatorType = Type.GetType(validatorEl.GetAttribute("type"));
                if (validatorType == null)
                {
                    throw new ConfigurationErrorsException(String.Format("Cannot resolve validator type '{0}'", validatorEl.GetAttribute("type")));
                }

                userValidator = Activator.CreateInstance(validatorType) as IUserValidator;
                if (userValidator == null)
                {
                    throw new ConfigurationErrorsException(String.Format("Type {0} does not implement the IUserValidator interface.", validatorType.FullName));
                }

                if (userValidator is IConfigurableUserValidator)
                {
                    (userValidator as IConfigurableUserValidator).Configure(validatorEl);
                }
            }

            var realmEl = configurationElement.GetElementsByTagName("realm").OfType<XmlElement>().FirstOrDefault();
            var realm = realmEl == null ? String.Empty : realmEl.InnerText;
            _configuration= new BasicAuthenticationConfiguration(userValidator, realm);
        }

        public void Enable(IPipelines pipelines)
        {
            BasicAuthentication.Enable(pipelines, _configuration);
        }
    }
}