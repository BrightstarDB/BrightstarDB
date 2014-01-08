using System.Collections.Generic;
using System.Linq;
using Remotion.Linq;

namespace BrightstarDB.EntityFramework.Query
{
    /*
    internal class EntityFrameworkCollectionQueryExecutor : IQueryExecutor
    {
        private EntityContext _context;
        private BrightstarEntityObject _parent;
        private string _propertyType;
        private bool _isInverse;

        public EntityFrameworkCollectionQueryExecutor(BrightstarEntityContext context, BrightstarEntityObject parent, string propertyType, bool isInverse)
        {
            _context = context;
            _parent = parent;
            _propertyType = propertyType;
            _isInverse = isInverse;
        }

        #region Implementation of IQueryExecutor

        /// <summary>
        /// Executes the given <paramref name="queryModel"/> as a scalar query, i.e. as a query returning a scalar value of type <typeparamref name="T"/>.
        ///             The query ends with a scalar result operator, for example a <see cref="T:Remotion.Linq.Clauses.ResultOperators.CountResultOperator"/> or a <see cref="T:Remotion.Linq.Clauses.ResultOperators.SumResultOperator"/>.
        /// </summary>
        /// <typeparam name="T">The type of the scalar value returned by the query.</typeparam><param name="queryModel">The <see cref="T:Remotion.Linq.QueryModel"/> representing the query to be executed. Analyze this via an 
        ///             <see cref="T:Remotion.Linq.IQueryModelVisitor"/>.</param>
        /// <returns>
        /// A scalar value of type <typeparamref name="T"/> that represents the query's result.
        /// </returns>
        /// <remarks>
        /// The difference between <see cref="M:Remotion.Linq.IQueryExecutor.ExecuteSingle``1(Remotion.Linq.QueryModel,System.Boolean)"/> and <see cref="M:Remotion.Linq.IQueryExecutor.ExecuteScalar``1(Remotion.Linq.QueryModel)"/> is in the kind of object that is returned.
        ///             <see cref="M:Remotion.Linq.IQueryExecutor.ExecuteSingle``1(Remotion.Linq.QueryModel,System.Boolean)"/> is used when a query that would otherwise return a collection result set should pick a single value from the 
        ///             set, for example the first, last, minimum, maximum, or only value in the set. <see cref="M:Remotion.Linq.IQueryExecutor.ExecuteScalar``1(Remotion.Linq.QueryModel)"/> is used when a value is 
        ///             calculated or aggregated from all the values in the collection result set. This applies to, for example, item counts, average calculations,
        ///             checks for the existence of a specific item, and so on.
        /// </remarks>
        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            return ExecuteCollection<T>(queryModel).SingleOrDefault();
        }

        /// <summary>
        /// Executes the given <paramref name="queryModel"/> as a single object query, i.e. as a query returning a single object of type 
        ///             <typeparamref name="T"/>.
        ///             The query ends with a single result operator, for example a <see cref="T:Remotion.Linq.Clauses.ResultOperators.FirstResultOperator"/> or a <see cref="T:Remotion.Linq.Clauses.ResultOperators.SingleResultOperator"/>.
        /// </summary>
        /// <typeparam name="T">The type of the single value returned by the query.</typeparam><param name="queryModel">The <see cref="T:Remotion.Linq.QueryModel"/> representing the query to be executed. Analyze this via an 
        ///             <see cref="T:Remotion.Linq.IQueryModelVisitor"/>.</param><param name="returnDefaultWhenEmpty">If <see langword="true"/>, the executor must return a default value when its result set is empty; 
        ///             if <see langword="false"/>, it should throw an <see cref="T:System.InvalidOperationException"/> when its result set is empty.</param>
        /// <returns>
        /// A single value of type <typeparamref name="T"/> that represents the query's result.
        /// </returns>
        /// <remarks>
        /// The difference between <see cref="M:Remotion.Linq.IQueryExecutor.ExecuteSingle``1(Remotion.Linq.QueryModel,System.Boolean)"/> and <see cref="M:Remotion.Linq.IQueryExecutor.ExecuteScalar``1(Remotion.Linq.QueryModel)"/> is in the kind of object that is returned.
        ///             <see cref="M:Remotion.Linq.IQueryExecutor.ExecuteSingle``1(Remotion.Linq.QueryModel,System.Boolean)"/> is used when a query that would otherwise return a collection result set should pick a single value from the 
        ///             set, for example the first, last, minimum, maximum, or only value in the set. <see cref="M:Remotion.Linq.IQueryExecutor.ExecuteScalar``1(Remotion.Linq.QueryModel)"/> is used when a value is 
        ///             calculated or aggregated from all the values in the collection result set. This applies to, for example, item counts, average calculations,
        ///             checks for the existence of a specific item, and so on.
        /// </remarks>
        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            if (queryModel.ResultOperators.Any(x => x is Remotion.Linq.Clauses.ResultOperators.FirstResultOperator))
            {
                return returnDefaultWhenEmpty
                           ? ExecuteCollection<T>(queryModel).FirstOrDefault()
                           : ExecuteCollection<T>(queryModel).First();
            }
            return returnDefaultWhenEmpty
                       ? ExecuteCollection<T>(queryModel).SingleOrDefault()
                       : ExecuteCollection<T>(queryModel).Single();
        }

        /// <summary>
        /// Executes the given <paramref name="queryModel"/> as a collection query, i.e. as a query returning objects of type <typeparamref name="T"/>. 
        ///             The query does not end with a scalar result operator, but it can end with a single result operator, for example 
        ///             <see cref="T:Remotion.Linq.Clauses.ResultOperators.SingleResultOperator"/> or <see cref="T:Remotion.Linq.Clauses.ResultOperators.FirstResultOperator"/>. In such a case, the returned enumerable must yield exactly 
        ///             one object (or none if the last result operator allows empty result sets).
        /// </summary>
        /// <typeparam name="T">The type of the items returned by the query.</typeparam><param name="queryModel">The <see cref="T:Remotion.Linq.QueryModel"/> representing the query to be executed. Analyze this via an 
        ///             <see cref="T:Remotion.Linq.IQueryModelVisitor"/>.</param>
        /// <returns>
        /// A scalar value of type <typeparamref name="T"/> that represents the query's result.
        /// </returns>
        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var sparqlQuery = SparqlGeneratorQueryModelVisitor.GenerateSparqlLinqQuery(_context);
            return sparqlQuery.IsInstanceQuery
                       ? _context.ExecuteInstanceQuery<T>(sparqlQuery.InstanceUri, sparqlQuery.TypeUri)
                       : _context.ExecuteQuery<T>(sparqlQuery);
        }

        #endregion
    }
     */
}   