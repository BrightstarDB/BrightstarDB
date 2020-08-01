using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;

namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// An implementation of <see cref="IQueryable"/> that translates LINQ to SPARQL
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityFrameworkQueryable<T> : QueryableBase<T>
    {
        private static IQueryExecutor CreateExecutor(EntityContext context)
        {
            return new EntityFrameworkQueryExecutor(context);
        }

        /// <summary>
        /// Create a new queryable context
        /// </summary>
        /// <param name="context">The entity framework context to be queried</param>
        public EntityFrameworkQueryable(EntityContext context) : base(QueryParser.CreateDefault(), CreateExecutor(context)){}

        /// <summary>
        /// Create a new queryable context
        /// </summary>
        /// <param name="provider">The query provider</param>
        /// <param name="expression">The query expression</param>
        public EntityFrameworkQueryable(IQueryProvider provider, Expression expression) : base(provider, expression){}

    }
}
