using System;
using Remotion.Linq.Clauses;

namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// Represents a single clause in the ORDER BY part of a SPARQL Query
    /// </summary>
    public class SparqlOrdering
    {
        /// <summary>
        /// Get the expression used for sorting
        /// </summary>
        public string SelectorExpression { get; private set; }

        /// <summary>
        /// Get the direction of the sort
        /// </summary>
        public OrderingDirection OrderingDirection { get; private set; }

        /// <summary>
        /// Create a new ordering clause
        /// </summary>
        /// <param name="selectorExpression">The expression used for sorting</param>
        /// <param name="orderingDirection">The sort direction</param>
        public SparqlOrdering(string selectorExpression, OrderingDirection orderingDirection)
        {
            SelectorExpression = selectorExpression;
            OrderingDirection = orderingDirection;
        }

        /// <summary>
        /// Returns the SPARQL substring for the ordering clause
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format(
                OrderingDirection == OrderingDirection.Asc ? "ASC({0})" : "DESC({0})",
                SelectorExpression);
        }
    }
}