using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace BrightstarDB.EntityFramework.Query
{
    class SparqlGeneratorWhereExpressionOptimisationVisitor : ExpressionTreeVisitor
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


        protected override Expression VisitBinaryExpression(BinaryExpression expression)
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
                            IsTrue(VisitExpression(expression.Left)) &&
                                                  IsTrue(VisitExpression(expression.Right)));
                    break;
 
                case ExpressionType.OrElse:
                case ExpressionType.AndAlso:
                    _inBooleanExpression = true;
                    ret = 
                        new BooleanFlagExpression(
                            // Check left and right expression content are optimisable
                            IsTrue(VisitExpression(expression.Left)) &&
                                                  IsTrue(VisitExpression(expression.Right)));
                    break;
            }
            _inBooleanExpression = currentlyInBooleanExpression;
            return ret;
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name.Equals("Equals"))
            {
                if (expression.Object is MemberExpression && expression.Arguments[0] is ConstantExpression)
                {
                    return
                        new BooleanFlagExpression(IsTrue(VisitExpression(expression.Object)) &&
                                                  IsTrue(VisitExpression(expression.Arguments[0])));
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

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            if (expression.Type == typeof (bool) && ((bool) expression.Value == false))
            {
                // False comparisons are more complex as they have to take into account unbound properties
                return new BooleanFlagExpression(false);
            }
            return new BooleanFlagExpression(true);
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
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

    internal class BooleanFlagExpression : ExtensionExpression 
    {
        public BooleanFlagExpression(bool value) : 
#if WINDOWS_PHONE || PORTABLE
            base(typeof(bool)) // Results in an Extension ExpressionType (according to the docs)
#else
            base(typeof(bool), ExpressionType.Extension)
#endif
        {
            Value = value;
        }

        public bool Value { get; set; }

#if !WINDOWS_PHONE && !PORTABLE
        public override ExpressionType NodeType
        {
            get
            {
                return ExpressionType.Extension;
            }
        }
#endif

        #region Overrides of ExtensionExpression

        /// <summary>
        /// Must be overridden by <see cref="T:Remotion.Linq.Clauses.Expressions.ExtensionExpression"/> subclasses by calling <see cref="M:Remotion.Linq.Parsing.ExpressionTreeVisitor.VisitExpression(System.Linq.Expressions.Expression)"/> on all 
        ///             children of this extension node. 
        /// </summary>
        /// <param name="visitor">The visitor to visit the child nodes with.</param>
        /// <returns>
        /// This <see cref="T:Remotion.Linq.Clauses.Expressions.ExtensionExpression"/>, or an expression that should replace it in the surrounding tree.
        /// </returns>
        /// <remarks>
        /// If the visitor replaces any of the child nodes, a new <see cref="T:Remotion.Linq.Clauses.Expressions.ExtensionExpression"/> instance should
        ///             be returned holding the new child nodes. If the node has no children or the visitor does not replace any child node, the method should
        ///             return this <see cref="T:Remotion.Linq.Clauses.Expressions.ExtensionExpression"/>. 
        /// </remarks>
        protected internal override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            return this;
        }

        #endregion
    }
}
