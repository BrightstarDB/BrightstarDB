using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace BrightstarDB.EntityFramework.Query
{
    internal class SparqlGroupingExpression : ExtensionExpression
    {
        private List<SelectVariableNameExpression> _groupVars;
 
        public SparqlGroupingExpression(IEnumerable<SelectVariableNameExpression> groupVars, Type groupingType) :
            base(groupingType, ExpressionType.Extension)
        {
            _groupVars = groupVars.ToList();
        }

        public IList<SelectVariableNameExpression> GroupVars { get { return _groupVars; } }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            foreach (var g in _groupVars)
            {
                visitor.VisitExpression(g);
            }
            return this;
        }
    }
}