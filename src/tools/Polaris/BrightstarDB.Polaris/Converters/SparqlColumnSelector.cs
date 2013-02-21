using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Xml.Linq;
using BrightstarDB.Rdf;

namespace BrightstarDB.Polaris.Converters
{
    public class SparqlColumnSelector : IValueConverter
    {
        private static readonly XNamespace SparqlResultsNs = "http://www.w3.org/2005/sparql-results#";

        #region Implementation of IValueConverter

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var resultElement = value as XElement;
            if (resultElement != null)
            {
                var colName = parameter as string;
                var bindingElement =
                    resultElement.Elements(SparqlResultsNs + "binding").Where(
                        el => el.Attribute("name") != null && el.Attribute("name").Value.Equals(colName))
                        .Select(el=>el.Elements().FirstOrDefault())
                        .FirstOrDefault();
                if (bindingElement != null)
                {
                    if (bindingElement.Name.LocalName == "uri")
                    {
                        return String.Format("<{0}>", bindingElement.Value);
                    }
                    if (bindingElement.Name.LocalName == "literal")
                    {
                        if (bindingElement.Attribute("datatype") != null)
                        {
                            var datatype = bindingElement.Attribute("datatype").Value;
                            try
                            {
                                return RdfDatatypes.ParseLiteralString(bindingElement.Value, datatype);
                            }
                            catch (Exception)
                            {
                                // Ignore and fall through to return the string value
                            }
                        }
                        return bindingElement.Value;
                    }
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
