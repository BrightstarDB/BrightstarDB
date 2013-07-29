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
        private readonly Expression _elementVar;
        private readonly List<SelectVariableNameExpression> _groupVars;
 
        public SparqlGroupingExpression(Expression elementVar, IEnumerable<SelectVariableNameExpression> groupVars, Type groupingType) :
            base(groupingType, ExpressionType.Extension)
        {
            _elementVar = elementVar; 
            _groupVars = groupVars.ToList();
        }

        public Expression ElementExpression { get { return _elementVar; } }
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