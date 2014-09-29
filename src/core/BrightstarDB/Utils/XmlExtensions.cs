using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BrightstarDB.Utils
{
    /// <summary>
    /// Provides utility methods for handling XML data
    /// </summary>
    public static class XmlExtensions
    {
        static bool IsRunningOnMono
        {
            get
            {
                return Type.GetType("Mono.Runtime") != null;
            }
        }

#if !PORTABLE && !WINDOWS_PHONE
        /// <summary>
        /// Utility method to convert and XDocument to an XmlDocument
        /// </summary>
        /// <param name="document">The XDocument to be converted</param>
        /// <returns>The XmlDocument representation of the provided XDocument</returns>
        public static XmlDocument AsXmlDocument(this XDocument document)
        {
            var xmlDoc = new XmlDocument();
            if (IsRunningOnMono)
            {
                xmlDoc.LoadXml(document.ToString());
            }
            else
            {
                using (var xmlReader = document.CreateReader())
                {
                    xmlDoc.Load(xmlReader);
                }
            }

            return xmlDoc;
        }
#endif
    }
}