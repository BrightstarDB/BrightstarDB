using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses;

namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// Manages the context information required during the processing of an entity framework LINQ query into SPARQL
    /// </summary>
    public class SparqlLinqQueryContext : SparqlQueryContext
    {
        /// <summary>
        /// Get the flag that indicates if this query is a simple type-instance query
        /// which can have optimised processing
        /// </summary>
        public bool IsInstanceQuery { get; private set; }
        /// <summary>
        /// Get the instance URI in the type-instance query
        /// </summary>
        /// <remarks>This property is valid iff <see cref="IsInstanceQuery"/> is true</remarks>
        public string InstanceUri { get; private set; }
        /// <summary>
        /// Get the type URI in the type-instance query
        /// </summary>
        /// <remarks>This property is valid iff <see cref="IsInstanceQuery"/> is true</remarks>
        public string TypeUri { get; private set; }
       
        /// <summary>
        /// Gets the constructor to be invoked when binding SPARQL query results to LINQ query results
        /// </summary>
        public ConstructorInfo Constructor { get; private set; }
        /// <summary>
        /// Gets the list of SPARQL query variables whose values are to be passed as variables into <see cref="Constructor"/> when binding SPARQL query results to LINQ query results
        /// </summary>
        public List<string> ConstructorArgs { get; private set; }
        /// <summary>
        /// Gets the list of tuples that map LINQ result instance members to the SPARQL variable that provides the value for that instance
        /// </summary>
        public List<Tuple<MemberInfo, string>> MemberAssignment { get; private set; }

        private readonly Expression _memberInitExpression;

        internal SparqlLinqQueryContext(string instanceUri, string typeUri) :base()
        {
            IsInstanceQuery = true;
            InstanceUri = instanceUri;
            TypeUri = typeUri;
        }

        ///<summary>
        /// Creates a new query context instance
        ///</summary>
        ///<param name="sparqlQuery">The SPARQL query to be executed</param>
        ///<param name="anonymousMembersMap">A list of tuples that bind the names of the anonymous result types members to the SPARQL variable that provides the value for that member</param>
        ///<param name="constructor">The constructor to invoke to bind a SPARQL results row to a result object</param>
        ///<param name="constructorArgs">A list of the SPARQL bindings that are to be passed into the constructor</param>
        ///<param name="memberMap">A list of tuples that bind the names of result object members to the SPARQL variables that provides the value for the member</param>
        ///<param name="memberInitExpression">A LINQ expression that is used to initialize the members of the results object from a SPARQL results row</param>
        /// <param name="orderingDirections">An enumeration of the orderings for each of the sort variables in the SPARQL query</param>
        public SparqlLinqQueryContext(string sparqlQuery, IEnumerable<Tuple<string, string>> anonymousMembersMap, 
            ConstructorInfo constructor, List<string> constructorArgs, List<Tuple<MemberInfo, string>> memberMap,
            Expression memberInitExpression,
            IEnumerable<OrderingDirection> orderingDirections 
            ) :base(sparqlQuery, anonymousMembersMap, orderingDirections, SparqlResultsFormat.Xml, RdfFormat.NTriples)
        {
            IsInstanceQuery = false;
            Constructor = constructor;
            ConstructorArgs = constructorArgs;
            MemberAssignment = memberMap;
            _memberInitExpression = memberInitExpression;
        }

        /// <summary>
        /// Returns true if the LINQ query uses an expression to initialize the results members
        /// </summary>
        public bool HasMemberInitExpression { get { return _memberInitExpression != null; } }

        /// <summary>
        /// Applies the LINQ expression used to initialize result members to a SPARQL result binding
        /// </summary>
        /// <typeparam name="T">The type of item to be initialized</typeparam>
        /// <param name="parameters">The SPARLQ result binding values</param>
        /// <param name="converter">A function that given a string value from the SPARQL binding and a target type is capable of returning a new instance of the target type bound to the string value</param>
        /// <returns>The generated member instance</returns>
        public object ApplyMemberInitExpression<T>(Dictionary<string, object> parameters, Func<string, string, Type, object> converter )
        {
            var exprBuilder = new SparqlGeneratorSelectExpressionBuilder(parameters, converter);
            var expressionBody = exprBuilder.VisitExpression(_memberInitExpression);
            Expression<Func<T>> lambdaWithoutParameters = Expression.Lambda<Func<T>>(expressionBody);
            return lambdaWithoutParameters.Compile()();
        }

        /// <summary>
        /// Applies the <see cref="Constructor"/> and <see cref="MemberAssignment"/> information to bind
        /// a SPARQL results row to a new LINQ result object
        /// </summary>
        /// <param name="values">The SPARQL results row</param>
        /// <returns>The new LINQ result object</returns>
        public object MapRow(Dictionary<string, object> values)
        {
            object ret = null;
            if (Constructor != null)
            {
                if (Constructor.GetParameters().Count() == 0)
                {
                    ret = Constructor.Invoke(new object[0]);
                }
                else
                {
                    var ctorParams = ConstructorArgs.Select(ca => values[ca]).ToArray();
                    ret = Constructor.Invoke(ctorParams);
                }
            }
            if (ret != null)
            {
                foreach (var mapping in MemberAssignment)
                {
                    if (values.ContainsKey(mapping.Item2))
                    {
#if PORTABLE
                        var memberInfo =
                            Constructor.DeclaringType.GetMember(mapping.Item1.Name, BindingFlags.Public).FirstOrDefault();
                        if (memberInfo != null)
                        {
                            var methodInfo = memberInfo as MethodInfo;
                            methodInfo.Invoke(ret, new []{values[mapping.Item2]});
                        }
#else
                        Constructor.DeclaringType.InvokeMember(mapping.Item1.Name, BindingFlags.Public, null, ret,
                                                               new object[] {values[mapping.Item2]});
#endif
                    }
                }
            }
            return ret;
        }
    }

}
