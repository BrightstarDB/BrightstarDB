using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using VDS.RDF.Parsing.Events.RdfXml;

namespace BrightstarDB.EntityFramework.Query
{
    class SparqlGeneratorWhereExpressionOptimisationVisitor : ExpressionVisitor
    {
        private readonly SparqlQueryBuilder _queryBuilder;
        private bool _inBooleanExpression = false;

        public SparqlGeneratorWhereExpressionOptimisationVisitor(SparqlQueryBuilder queryBuilder)
        {
            _queryBuilder = queryBuilder;
        }

        protected bool IsTrue(Expression expr)
        {
            return (expr is BooleanFlagExpression) && ((BooleanFlagExpression) expr).Value;
        }

        public override Expression Visit(Expression node)
        {
            if (node is SubQueryExpression subQuery) return VisitSubQueryExpression(subQuery);
            return base.Visit(node);
        }

        protected Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            return expression;
            //return this.Visit(expression.QueryModel.MainFromClause.FromExpression);
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            var currentlyInBooleanExpression = _inBooleanExpression;
            var ret = new BooleanFlagExpression(false);
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    _inBooleanExpression = false;
                    ret =
                        new BooleanFlagExpression(
                            (
                            // Allowed: x.prop=constant
                            (expression.Left is MemberExpression && expression.Right is ConstantExpression) ||
                            // Allowed: x.prop=y.other_prop
                            (expression.Left is MemberExpression && expression.Right is MemberExpression)
                            ) &&
                            // Check left and right expression content are optimisable
                            IsTrue(Visit(expression.Left)) && IsTrue(Visit(expression.Right)));
                    break;
 
                case ExpressionType.OrElse:
                case ExpressionType.AndAlso:
                    _inBooleanExpression = true;
                    ret = 
                        new BooleanFlagExpression(
                            // Check left and right expression content are optimisable
                            IsTrue(Visit(expression.Left)) && IsTrue(Visit(expression.Right)));
                    break;
            }
            _inBooleanExpression = currentlyInBooleanExpression;
            return ret;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            if (expression.Method.Name.Equals("Equals"))
            {
                if (expression.Object is MemberExpression && expression.Arguments[0] is ConstantExpression)
                {
                    return
                        new BooleanFlagExpression(IsTrue(Visit(expression.Object)) &&
                                                  IsTrue(Visit(expression.Arguments[0])));
                }
            }
            return new BooleanFlagExpression(false);
        }

        //protected override Expression VisitUnaryExpression(UnaryExpression expression)
        //{
        //    switch (expression.NodeType)
        //    {
        //        case ExpressionType.Not:
        //            return new BooleanFlagExpression(true);
        //        default:
        //            return new BooleanFlagExpression(false);
        //    }
        //}

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            if (expression.Type == typeof (bool) && ((bool) expression.Value == false))
            {
                // False comparisons are more complex as they have to take into account unbound properties
                return new BooleanFlagExpression(false);
            }
            return new BooleanFlagExpression(true);
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            var propertyInfo = expression.Member as PropertyInfo;
            if (propertyInfo != null)
            {
                var propertyHint = _queryBuilder.Context.GetPropertyHint(propertyInfo);
                if (propertyHint != null)
                {
                    if (propertyHint.MappingType == PropertyMappingType.Arc ||
                        propertyHint.MappingType == PropertyMappingType.InverseArc ||
                        propertyHint.MappingType == PropertyMappingType.Property)
                    {
                        return new BooleanFlagExpression(true);
                    }
                }
            }
            return new BooleanFlagExpression(false);
        }
    }

    internal class BooleanFlagExpression : Expression 
    {
        public BooleanFlagExpression(bool value)
        {
            Value = value;
            Type = typeof(bool);
        }

        public bool Value { get; set; }
        public override Type Type { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;

        #region Overrides of ExtensionExpression

        protected override Expression  VisitChildren(ExpressionVisitor visitor)
        {
            return this;
        }

        #endregion
    }
}
