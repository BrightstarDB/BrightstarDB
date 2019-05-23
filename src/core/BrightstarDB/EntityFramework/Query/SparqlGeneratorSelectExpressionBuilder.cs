using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using BrightstarDB.Rdf;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// Traverses the select expression tree converting SelectVariableNameExpressions to a lookup in an input dictionary
    /// </summary>
    internal class SparqlGeneratorSelectExpressionBuilder : ExpressionVisitor
    {
        private readonly Dictionary<string, object> _values;
        private readonly Func<string, string, Type, object> _converter; 
        public SparqlGeneratorSelectExpressionBuilder(Dictionary<string, object> values, Func<string, string, Type, object> converter)
        {
            _values = values;
            _converter = converter;
        }

#if WINDOWS_PHONE || PORTABLE
        protected internal override Expression VisitExtensionExpression(ExtensionExpression expression)
#else
        protected override Expression  VisitExtension(Expression expression)
#endif
        {
            if (expression is SelectIdentifierVariableNameExpression)
            {
                var siv = expression as SelectIdentifierVariableNameExpression;
                var v = _values[siv.Name].ToString();
                if (!String.IsNullOrEmpty(siv.IdentifierPrefix) && v.StartsWith(siv.IdentifierPrefix))
                {
                    v = v.Substring(siv.IdentifierPrefix.Length);
                }
                if (siv.Type.IsInstanceOfType(v))
                {
                    return Expression.Constant(v, siv.Type);
                }
                var converted = _converter(v, RdfDatatypes.GetLiteralLanguageTag(_values[siv.Name]), siv.Type);
                return Expression.Constant(converted, siv.Type);
            }
            if (expression is SelectVariableNameExpression)
            {
                var svn = expression as SelectVariableNameExpression;
                var v = _values[svn.Name];
                if (svn.Type.IsInstanceOfType(v))
                {
                    return Expression.Constant(v, svn.Type);
                }
                var converted = _converter(v.ToString(), RdfDatatypes.GetLiteralLanguageTag(v), svn.Type);
                return Expression.Constant(converted, svn.Type);
            }
            return expression;
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            if (expression.Expression is SelectVariableNameExpression)
            {
                // Extracting a value from a resource that should be in the _values dictionary
                var svn = expression.Expression as SelectVariableNameExpression;
                var parent = _converter(_values[svn.Name].ToString(), RdfDatatypes.GetLiteralLanguageTag(_values[svn.Name]), svn.Type);
                if (expression.Member is PropertyInfo)
                {
                    var property = expression.Member as PropertyInfo;
                    var value = property.GetValue(parent, null);
                    return Expression.Constant(value);
                }
            }
            return base.VisitMember(expression);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding memberBinding)
        {
            if(memberBinding.BindingType == MemberBindingType.Assignment)
            {
#if PORTABLE
                var propertyInfo = memberBinding.Member as PropertyInfo;
                var assignment = memberBinding as MemberAssignment;
                if (propertyInfo != null 
                    && assignment != null 
                    && assignment.Expression.Type.IsValueType 
                    && !propertyInfo.PropertyType.IsValueType)
                {
                    var valueExpression = Expression.TypeAs(VisitExpression(assignment.Expression), propertyInfo.PropertyType);
                    return Expression.Bind(memberBinding.Member, valueExpression);
                }
#else
                if (memberBinding.Member.MemberType == MemberTypes.Property)
                {
                    var propertyInfo = memberBinding.Member as PropertyInfo;
                    var assignment = memberBinding as MemberAssignment;
                    if (assignment.Expression.Type.IsValueType() && !propertyInfo.PropertyType.IsValueType())
                    {
                        var valueExpression = Expression.TypeAs(Visit(assignment.Expression), propertyInfo.PropertyType);
                        return Expression.Bind(memberBinding.Member, valueExpression);
                    }
                }
#endif
            }
            return base.VisitMemberBinding(memberBinding);
        }

    }
}
