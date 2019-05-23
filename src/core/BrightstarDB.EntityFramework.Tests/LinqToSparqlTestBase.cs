using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    public class LinqToSparqlTestBase
    {
        protected MockContext Context;

        protected void InitializeContext()
        {
            var rmp = new ReflectionMappingProvider();
            rmp.AddMappingsForAssembly(EntityMappingStore.Instance, typeof(LinqToSparqlTests).GetTypeInfo().Assembly);
            Context = new MockContext();
        }

        protected void AssertQuerySparql(string expected)
        {
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(NormalizeSparql(expected), NormalizeSparql(lastSparql));
        }

        protected static string NormalizeSparql(string input)
        {
            input = Regex.Replace(input, @"\s*\.\s*", ".", RegexOptions.Multiline);
            input = Regex.Replace(input, @"\s*,\s*", ",", RegexOptions.Multiline);
            input = Regex.Replace(input, @"\s+", " ", RegexOptions.Multiline);
            input = Regex.Replace(input, @"\s*\{\s*", "{", RegexOptions.Multiline);
            input = Regex.Replace(input, @"\s*\(\s*", "(", RegexOptions.Multiline);
            input = Regex.Replace(input, @"\s*=\s*", "=", RegexOptions.Multiline);
            input = Regex.Replace(input, @"\s*}\s*", "}", RegexOptions.Multiline);
            return input;
        }

    }
}