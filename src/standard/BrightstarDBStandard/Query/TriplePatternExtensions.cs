using System.Linq;
using VDS.RDF.Query.Patterns;

namespace BrightstarDB.Query
{
    /// <summary>
    /// Extension methods for <see cref="VDS.RDF.Query.Patterns.TriplePattern"/>
    /// </summary>
    public static class TriplePatternExtensions
    {
        /// <summary>
        /// Returns the number of pattern items in the triple pattern that have a 
        /// non-null VariableName
        /// </summary>
        /// <param name="tp"></param>
        /// <returns></returns>
        /// <remarks>This method is provided as an alternative to using TriplePattern.Variables.Count() 
        /// as in some cases the Variables list repeats a variable that is only actually bound once 
        /// in the triple pattern. Perhaps this is a dotNetRdf bug that will get fixed ?
        /// </remarks>
        public static int GetVariableCount(this TriplePattern  tp)
        {
            return (new[] {tp.Subject, tp.Predicate, tp.Object}).Count(i => i.VariableName != null);
        }
    }
}
