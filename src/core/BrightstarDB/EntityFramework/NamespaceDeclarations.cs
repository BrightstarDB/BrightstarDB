using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Provides an enumeration over the collection of <see cref="NamespaceDeclarationAttribute"/>s found in an assembly,
    /// along with convenience methods for serializing them in a number of different syntaxes.
    /// </summary>
    public class NamespaceDeclarations : IEnumerable<NamespaceDeclarationAttribute>
    {
        private readonly Assembly _assembly;
        private NamespaceDeclarations(Assembly srcAssembly)
        {
            _assembly = srcAssembly;
        }


        /// <summary>
        /// Returns an enumerator that iterates through the collection  of <see cref="NamespaceDeclarationAttribute"/> instances exposed by this object.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<NamespaceDeclarationAttribute> GetEnumerator()
        {
            return _assembly.GetCustomAttributes(typeof (NamespaceDeclarationAttribute), false).Cast<NamespaceDeclarationAttribute>().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of <see cref="NamespaceDeclarationAttribute"/> instances exposed by this object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Return the collection of namespace declarations declared by the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to search for namespace declarations.</param>
        /// <remarks>This method finds all of the custom attributes of type <see cref="NamespaceDeclarationAttribute"/>
        /// in <paramref name="assembly"/>. If <paramref name="assembly"/> is null then the method <see cref="Assembly.GetCallingAssembly"/>
        /// is used to retrieve the assembly of the method that calls this method.</remarks>
        /// <returns>A <see cref="NamespaceDeclarations"/> instance that can be used to iterate or
        /// to format the colleciton of namespace declarations found in <paramref name="assembly"/>.</returns>
        public static NamespaceDeclarations ForAssembly(Assembly assembly = null)
        {
            if (assembly == null) assembly = Assembly.GetCallingAssembly();
            return new NamespaceDeclarations(assembly);
        }

        /// <summary>
        /// Returns a string containing all of the namespace prefix declarations formatted
        /// for a SPARQL query or update expression
        /// </summary>
        /// <returns>A string consisting of one PREFIX statement for each namespace declaration, with each
        /// declaration on a separate line.</returns>
        public string AsSparql()
        {
            var sb = new StringBuilder();
            foreach (var nsDecl in this)
            {
                sb.Append("PREFIX ");
                sb.Append(nsDecl.Prefix);
                sb.Append(": <");
                sb.Append(nsDecl.Reference);
                sb.AppendLine(">");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a string containing all of the namespace prefixes formatted as Turtle
        /// @prefix directives
        /// </summary>
        /// <remarks>RDF 1.1 compliant Turtle parsers should also be able to process the SPARQL format
        /// produced by the <see cref="AsSparql"/> method. This method is provided to support older
        /// Turtle parsers.
        /// </remarks>
        /// <returns>A string consisting of one @prefix statement for each namespace declaration. Each on a separate line.</returns>
        public string AsTurtle()
        {
            var sb = new StringBuilder();
            foreach (var nsDecl in this)
            {
                sb.Append("@prefix ");
                sb.Append(nsDecl.Prefix);
                sb.Append(": <");
                sb.Append(nsDecl.Reference);
                sb.AppendLine("> .");
            }
            return sb.ToString();
        }


        /// <summary>
        /// Returns a dictionary mapping prefix to namespace URI for all of the namespace declarations
        /// </summary>
        /// <returns>A new Dictionary instance</returns>
        public Dictionary<string, string> AsDictionary()
        {
            return this.ToDictionary(x => x.Prefix, x => x.Reference);
        } 
    }
}
