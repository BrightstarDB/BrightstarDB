using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// Traverses the select expression tree converting SelectVariableNameExpressions to a lookup in an input dictionary
    /// </summary>
    internal class SparqlGeneratorSelectExpressionBuilder : ExpressionTreeVisitor
    {
        private readonly Dictionary<string, object> _values;
        private readonly Func<string, Type, object> _converter; 
        public SparqlGeneratorSelectExpressionBuilder(Dictionary<string, object> values, Func<string, Type, object> converter)
        {
            _values = values;
            _converter = converter;
        }

#if WINDOWS_PHONE
        protected internal override Expression VisitExtensionExpression(ExtensionExpression expression)
#else
        protected override Expression  VisitExtensionExpression(ExtensionExpression expression)
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
                var converted = _converter(v, siv.Type);
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
                var converted = _converter(v.ToString(), svn.Type);
                return Expression.Constant(converted, svn.Type);
            }
            return expression;
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            if (expression.Expression is SelectVariableNameExpression)
            {
                // Extracting a value from a resource that should be in the _values dictionary
                var svn = expression.Expression as SelectVariableNameExpression;
                var parent = _converter(_values[svn.Name].ToString(), svn.Type);
                if (expression.Member is PropertyInfo)
                {
                    var property = expression.Member as PropertyInfo;
                    var value = property.GetValue(parent, null);
                    return Expression.Constant(value);
                }
            }
            return base.VisitMemberExpression(expression);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding memberBinding)
        {
            if(memberBinding.BindingType == MemberBindingType.Assignment)
            {
                if (memberBinding.Member.MemberType == MemberTypes.Property)
                {
                    var propertyInfo = memberBinding.Member as PropertyInfo;
                    var assignment = memberBinding as MemberAssignment;
                    if (assignment.Expression.Type.IsValueType && !propertyInfo.PropertyType.IsValueType)
                    {
                        var valueExpression = Expression.TypeAs(VisitExpression(assignment.Expression), propertyInfo.PropertyType);
                        return Expression.Bind(memberBinding.Member, valueExpression);
                    }
                }
            }
            return base.VisitMemberBinding(memberBinding);
        }

    }
}
