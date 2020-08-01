using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;

namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// Relinq query provider that returns a query provider for the elements of a collection rather than the collection itself
    /// </summary>
    internal class EntityFrameworkCollectionQueryProvider : QueryProviderBase
    {
        private readonly IQueryParser _queryParser;
        private readonly IQueryExecutor _queryExecutor;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryParser"></param>
        /// <param name="executor"></param>
        public EntityFrameworkCollectionQueryProvider(IQueryParser queryParser, IQueryExecutor executor) : base(queryParser, executor)
        {
            _queryParser = queryParser;
            _queryExecutor = executor;
        }

        #region Overrides of QueryProviderBase

        /// <summary>
        /// Constructs an <see cref="T:System.Linq.IQueryable`1"/> object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Linq.IQueryable`1"/> that can evaluate the query represented by the specified expression tree.
        /// </returns>
        /// <param name="expression">An expression tree that represents a LINQ query.</param><typeparam name="TElement">The type of the elements of the <see cref="T:System.Linq.IQueryable`1"/> that is returned.</typeparam>
        public override IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var queryProvider = new DefaultQueryProvider(typeof (EntityFrameworkQueryable<>), _queryParser, _queryExecutor);
            return new EntityFrameworkQueryable<TElement>(queryProvider, expression);
        }

        #endregion
    }
}