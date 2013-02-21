using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace BrightstarDB.SdShare
{
    public static class XDocumentSparqlExtensions
    {
        private static readonly XNamespace SparqlResultsNamespace = "http://www.w3.org/2005/sparql-results#";

        ///<summary>
        /// Returns the collection of elements that represent a single result row.
        ///</summary>
        ///<param name="document">The XDocument containing the result data</param>
        ///<returns>An enumeration of elements.</returns>
        public static IEnumerable<XElement> SparqlResultRows(this XDocument document)
        {
            return document.Descendants(SparqlResultsNamespace + "result");
        }

        /// <summary>
        /// Returns the boolean value encoded in the SPARQL results for an ASK query
        /// </summary>
        /// <param name="document">The XDocument containing the result data</param>
        /// <returns>Returns true if the result document contains a &lt;boolean&gt; element with a value of "true", false otherwise</returns>
        public static bool SparqlBooleanResult(this XDocument document)
        {
            if (document.Root == null) return false;
            var boolEl = document.Root.Element(SparqlResultsNamespace + "boolean");
            return (boolEl != null && boolEl.Value.Trim().ToLowerInvariant().Equals("true"));
        }

        /// <summary>
        /// Returns the value of the matching Sparql result column.
        /// </summary>
        /// <param name="row">The XElement that represents a result row</param>
        /// <param name="name">Name of the column value to return</param>
        /// <returns>The value of the column or null if no column exists.</returns>
        public static object GetColumnValue(this XElement row, string name)
        {
            var binding =
                row.Elements(SparqlResultsNamespace + "binding").Where(
                    e => (e.Attribute("name") != null && e.Attribute("name").Value.Equals(name))).FirstOrDefault();
            return binding == null ? null : GetTypedValue(binding);
        }

        /// <summary>
        /// Returns the string value of a SPARQL result column
        /// </summary>
        /// <param name="row">The XElement that represents a result row</param>
        /// <param name="columnIndex">The zero-based index of the column value to return</param>
        /// <returns>The value of the column or null if no column exists</returns>
        public static object GetColumnValue(this XElement row, int columnIndex)
        {
            var binding = row.Elements(SparqlResultsNamespace + "binding").ElementAtOrDefault(columnIndex);
            return binding == null ? null : GetTypedValue(binding);
        }

        /// <summary>
        /// Returns the variable column names in the Sparql result.
        /// </summary>
        /// <param name="doc">The XDocument containing the result data.</param>
        /// <returns>An IEnumerable string of column variable names.</returns>
        public static IEnumerable<string> GetVariableNames(this XDocument doc)
        {
            if (doc.Root != null)
            {
                var head = doc.Root.Element(SparqlResultsNamespace + "head");
                if (head != null)
                {
                    return
                        head.Elements(SparqlResultsNamespace + "variable").Where(e => (e.Attribute("name") != null)).Select(
                            e => e.Attribute("name").Value);
                }
            }
            return new string[0];
        }

        private static object GetTypedValue(XElement binding)
        {
            if (binding.Elements(SparqlResultsNamespace + "literal").FirstOrDefault() != null)
            {
                var datatype = GetBindingDataType(binding) ?? RdfDatatypes.PlainLiteral;
                object parsedValue;
                if (RdfDatatypes.TryParseLiteralString(binding.Value, datatype, out parsedValue)) return parsedValue;
                return binding.Value;
            }
            return new Uri(binding.Value);
        }

        /// <summary>
        /// Test if the specified Sparql result column is a literal.
        /// </summary>
        /// <param name="row">The XElement that represents a result row</param>
        /// <param name="name">Name of the column to check if its a literal</param>
        /// <returns>True if the column is a literal. False if the column doesn't exist.</returns>
        public static bool IsLiteral(this XElement row, string name)
        {
            var binding =
                row.Elements(SparqlResultsNamespace + "binding").Where(
                    e => (e.Attribute("name") != null && e.Attribute("name").Value.Equals(name))).FirstOrDefault();

            if (binding == null) return false;
            return binding.Elements(SparqlResultsNamespace + "literal").FirstOrDefault() != null;
        }


        /// <summary>
        /// Gets the datatype for the specified Sparql result column.
        /// </summary>
        /// <param name="row">The XElement that represents a result row</param>
        /// <param name="name">Name of the column to get the data type from</param>
        /// <returns>The literal datatype. Null if the column isn't present or the column is a URI</returns>
        public static string GetLiteralDatatype(this XElement row, string name)
        {
            var binding =
                row.Elements(SparqlResultsNamespace + "binding").Where(
                    e => (e.Attribute("name") != null && e.Attribute("name").Value.Equals(name))).FirstOrDefault();
            return binding == null ? null : GetBindingDataType(binding);
        }

        private static string GetBindingDataType(XElement binding)
        {
            var literal = binding.Elements(SparqlResultsNamespace + "literal").FirstOrDefault();
            if (literal == null) return null;

            var attribute = literal.Attribute("datatype");
            if (attribute == null) return null;

            return attribute.Value;
        }

        /// <summary>
        /// Gets the language code for the specified literal column
        /// </summary>
        /// <param name="row">The XElement that represents the sparql resul row</param>
        /// <param name="name">The name of the sparql result parameter</param>
        /// <returns>Language code of null if the named column doesnt exist, or there is no langusge code attribute</returns>
        public static string GetLiteralLanguageCode(this XElement row, string name)
        {
            var binding =
                row.Elements(SparqlResultsNamespace + "binding").Where(
                    e => (e.Attribute("name") != null && e.Attribute("name").Value.Equals(name))).FirstOrDefault();
            if (binding == null) return null;

            var literal = binding.Elements(SparqlResultsNamespace + "literal").FirstOrDefault();
            if (literal == null) return null;

            var attribute = literal.Attribute("lang");
            if (attribute == null) return null;

            return attribute.Value;
        }
    }
}
