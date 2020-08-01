using System.Xml;
using System.Xml.Linq;

namespace BrightstarDB.Utils
{
    /// <summary>
    /// Provides utility methods for handling XML data
    /// </summary>
    public static class XmlExtensions
    {
        

#if !PORTABLE && !WINDOWS_PHONE
        /// <summary>
        /// Utility method to convert and XDocument to an XmlDocument
        /// </summary>
        /// <param name="document">The XDocument to be converted</param>
        /// <returns>The XmlDocument representation of the provided XDocument</returns>
        public static XmlDocument AsXmlDocument(this XDocument document)
        {
            var xmlDoc = new XmlDocument();
            if (Configuration.IsRunningOnMono)
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