using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Parsing;

namespace BrightstarDB.EntityFramework.Query
{
    internal class SparqlGeneratorSelectExpressionTreeVisitor 
        : ExpressionTreeVisitorBase
    {
        private readonly SparqlQueryBuilder _queryBuilder;

        public static void GetSparqlExpression(Expression expression, SparqlQueryBuilder queryBuilder)
        {
            var visitor = new SparqlGeneratorSelectExpressionTreeVisitor(queryBuilder);
            var resultExpression = visitor.VisitExpression(expression);
            if (resultExpression is SelectVariableNameExpression)
            {
                queryBuilder.AddSelectVariable((resultExpression as SelectVariableNameExpression).Name);
            }
        }

        private SparqlGeneratorSelectExpressionTreeVisitor(SparqlQueryBuilder queryBuilder) : base(queryBuilder)
        {
            _queryBuilder = queryBuilder;
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            string sourceVarName = null;
            if (expression.Expression is QuerySourceReferenceExpression)
            {
                var source = expression.Expression as QuerySourceReferenceExpression;
                Expression mappedSourceExpression;
                sourceVarName = source.ReferencedQuerySource.ItemName;
                if (!_queryBuilder.TryGetQuerySourceMapping(source.ReferencedQuerySource, out mappedSourceExpression))
                {
                    mappedSourceExpression = VisitExpression(expression.Expression);
                }
                if (mappedSourceExpression is SelectVariableNameExpression)
                {
                    sourceVarName = (mappedSourceExpression as SelectVariableNameExpression).Name;
                }
                else if (mappedSourceExpression is SparqlGroupingExpression)
                {
                    var groupingExpression = mappedSourceExpression as SparqlGroupingExpression;
                    if (expression.Member.Name.Equals("Key"))
                    {
                        return groupingExpression.GroupVars.First();
                    }
                }
            }
            else if (expression.Expression is MemberExpression)
            {
                var memberExpression = VisitExpression(expression.Expression);
                if (memberExpression is SelectVariableNameExpression)
                {
                    sourceVarName = (memberExpression as SelectVariableNameExpression).Name;
                }
            }
            else if (expression.Expression is UnaryExpression)
            {
                var unary = expression.Expression as UnaryExpression;
                if (unary.NodeType == ExpressionType.TypeAs &&
                    unary.Operand is QuerySourceReferenceExpression)
                {
                    var targetType = unary.Type;
                    var source = unary.Operand as QuerySourceReferenceExpression;
                    if (source != null)
                    {
                        var itemType = source.ReferencedQuerySource.ItemType;
                        if (targetType.IsAssignableFrom(itemType))
                        {
                            expression = Expression.MakeMemberAccess(unary.Operand, expression.Member);
                            return VisitExpression(expression);
                        }
                    }
                }
                var updatedExpression = VisitExpression(expression.Expression) as UnaryExpression;
                if (updatedExpression != null && updatedExpression.Operand is SelectVariableNameExpression)
                {
#if WINDOWS_PHONE
                    return Expression.MakeMemberAccess(updatedExpression, expression.Member);
#else
                    return expression.Update(updatedExpression);
#endif
                }
            }
            if (!String.IsNullOrEmpty(sourceVarName))
            {
                if (expression.Member.MemberType == MemberTypes.Property)
                {
                    var propertyInfo = expression.Member as PropertyInfo;
                    var propertyHint = _queryBuilder.Context.GetPropertyHint(propertyInfo);
                    if (propertyHint != null)
                    {
                        switch (propertyHint.MappingType)
                        {
                            case PropertyMappingType.Arc:
                                {
                                    var memberVarName = _queryBuilder.GetVariableForObject(
                                        GraphNode.Variable, sourceVarName,
                                        GraphNode.Iri, propertyHint.SchemaTypeUri);
                                    if (memberVarName == null)
                                    {
                                        memberVarName = _queryBuilder.NextVariable();
                                        _queryBuilder.AddTripleConstraint(
                                            GraphNode.Variable, sourceVarName,
                                            GraphNode.Iri, propertyHint.SchemaTypeUri,
                                            GraphNode.Variable, memberVarName);
                                    }
                                    return new SelectVariableNameExpression(memberVarName, VariableBindingType.Resource, propertyInfo.PropertyType );
                                }
                            case PropertyMappingType.InverseArc:
                                {
                                    var memberVarName = _queryBuilder.GetVariableForSubject(
                                        GraphNode.Iri, propertyHint.SchemaTypeUri,
                                        GraphNode.Variable, sourceVarName);
                                    if (memberVarName == null)
                                    {
                                        memberVarName = _queryBuilder.NextVariable();
                                        _queryBuilder.AddTripleConstraint(
                                            GraphNode.Variable, memberVarName,
                                            GraphNode.Iri, propertyHint.SchemaTypeUri,
                                            GraphNode.Variable, sourceVarName);
                                    }
                                    return new SelectVariableNameExpression(memberVarName, VariableBindingType.Resource, propertyInfo.PropertyType);
                                }
                            case PropertyMappingType.Property:
                                {
                                    var propertyValueVarName = _queryBuilder.GetVariableForObject(GraphNode.Variable,
                                                                                                  sourceVarName,
                                                                                                  GraphNode.Iri,
                                                                                                  propertyHint.
                                                                                                      SchemaTypeUri);
                                    if (propertyValueVarName == null)
                                    {
                                        propertyValueVarName = _queryBuilder.NextVariable();
                                        _queryBuilder.AddTripleConstraint(
                                            GraphNode.Variable, sourceVarName,
                                            GraphNode.Iri, propertyHint.SchemaTypeUri,
                                            GraphNode.Variable, propertyValueVarName);
                                    }
                                    return new SelectVariableNameExpression(propertyValueVarName,
                                                                            VariableBindingType.Literal, propertyInfo.PropertyType);
                                }
                            case PropertyMappingType.Address:
                                {
                                    return new SelectVariableNameExpression(sourceVarName, VariableBindingType.Resource, propertyInfo.PropertyType);
                                }
                                case PropertyMappingType.Id:
                                {
                                    var prefix =
                                        _queryBuilder.Context.Mappings.GetIdentifierPrefix(
                                        propertyInfo.DeclaringType);
                                    return new SelectIdentifierVariableNameExpression(sourceVarName, prefix);
                                    //return new SelectVariableNameExpression(sourceVarName, VariableBindingType.Resource, propertyInfo.PropertyType);
                                }
                        }
                    }
                }
            }
            return base.VisitMemberExpression(expression);
        }

        protected override Expression VisitMemberInitExpression(MemberInitExpression expression)
        {
            _queryBuilder.MemberInitExpression = expression;
            _queryBuilder.Constructor = expression.NewExpression.Constructor;
            foreach(var a in expression.NewExpression.Arguments)
            {
                var mappedExpression = VisitExpression(a);
                if (mappedExpression is SelectVariableNameExpression)
                {
                    _queryBuilder.ConstructorArgs.Add((mappedExpression as SelectVariableNameExpression).Name);
                    _queryBuilder.AddSelectVariable((mappedExpression as SelectVariableNameExpression).Name);
                }
                else
                {
                    throw new NotSupportedException(
                        String.Format(
                            "Unable to map constructor expression to a SPARQL results variable. Only simple property expressions are currently supported. Invalid expression is: {0}",
                            a));
                }
            }

            var updatedBindings = new List<MemberBinding>();
            foreach(var b in expression.Bindings)
            {
                updatedBindings.Add(VisitMemberBinding(b));
            }
#if WINDOWS_PHONE
            var updatedExpression = Expression.MemberInit(expression.NewExpression, updatedBindings);
#else
            var updatedExpression =  expression.Update(expression.NewExpression, updatedBindings);
#endif
            _queryBuilder.MemberInitExpression = updatedExpression;
            return updatedExpression;
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding memberBinding)
        {
            if (memberBinding.BindingType == MemberBindingType.Assignment)
            {
                var ma = memberBinding as MemberAssignment;
                if (ma != null)
                {
                    var targetType = GetMemberType(ma.Member);
                    if (targetType.IsGenericCollection())
                    {
                        // Trying to bind a value to a collection property
                        // Currently we only support doing this if the the value to be bound is a property of an entity
                        var valueExpression = ma.Expression;
                        if (valueExpression is MemberExpression)
                        {
                            var memberExpresion = valueExpression as MemberExpression;
                            if (memberExpresion.Expression is QuerySourceReferenceExpression)
                            {
                                // QuerySourceReferenceExpression should resolve to a resource that will be returned by the query
                                var qsr = memberExpresion.Expression as QuerySourceReferenceExpression;
                                var updatedQuerySourceExpression = VisitExpression(qsr);
                                if (updatedQuerySourceExpression is SelectVariableNameExpression)
                                {
                                    if ((updatedQuerySourceExpression as SelectVariableNameExpression).BindingType == VariableBindingType.Resource)
                                    {
                                        // Got a match, so rewrite this binding as a binding to a property on the resource
                                        // returned by the SPARQL results column named in the SelectVariableNameExpression
                                        return Expression.Bind(ma.Member,
                                                               Expression.MakeMemberAccess(updatedQuerySourceExpression,
                                                                                           memberExpresion.Member));
                                    }
                                }
                            }
                        }
                        return base.VisitMemberBinding(memberBinding);
                    }
                    else
                    {
                        // Handle a single property
                        var valueExpression = VisitExpression(ma.Expression);
                        if (valueExpression is SelectVariableNameExpression)
                        {
                            _queryBuilder.MembersMap.Add(new Tuple<MemberInfo, string>(ma.Member,
                                                                                       (valueExpression as
                                                                                        SelectVariableNameExpression).
                                                                                           Name));
                            _queryBuilder.AddSelectVariable((valueExpression as SelectVariableNameExpression).Name);
                            return Expression.Bind(ma.Member, valueExpression);
                        }
                    }
                }
            }
            return base.VisitMemberBinding(memberBinding);
        }

        private static Type GetMemberType(MemberInfo m)
        {
            if (m is PropertyInfo)
            {
                var propertyInfo = m as PropertyInfo;
                return propertyInfo.PropertyType;
            }
            if (m is FieldInfo)
            {
                var fieldInfo = m as FieldInfo;
                return fieldInfo.FieldType;
            }
            throw new ArgumentException(String.Format("Unexpected member type: {0}", m.MemberType), "m");
        }

        protected override MemberBinding VisitMemberAssignment(MemberAssignment memberAssignment)
        {
#if WINDOWS_PHONE
            return Expression.Bind(memberAssignment.Member, VisitExpression(memberAssignment.Expression));
#else
            return memberAssignment.Update(VisitExpression(memberAssignment.Expression));
#endif
        }

        protected override Expression VisitConditionalExpression(ConditionalExpression expression)
        {
#if WINDOWS_PHONE
            return Expression.Condition(VisitExpression(expression.Test),
                                        VisitExpression(expression.IfTrue),
                                        VisitExpression(expression.IfFalse));
#else
            return expression.Update(VisitExpression(expression.Test), VisitExpression(expression.IfTrue),
                              VisitExpression(expression.IfFalse));
#endif
        }

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
#if WINDOWS_PHONE
            return Expression.MakeBinary(expression.NodeType,
                                         VisitExpression(expression.Left), VisitExpression(expression.Right),
                                         expression.IsLiftedToNull, expression.Method,
                                         expression.NodeType == ExpressionType.Coalesce
                                             ? null
                                             : VisitExpression(expression.Conversion) as LambdaExpression);
            /* KA: Replaced this long switch with MakeBinary() call above - needs checking that this works though.
            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                    return Expression.Add(VisitExpression(expression.Left), VisitExpression(expression.Right));
                case ExpressionType.AddChecked:
                    return Expression.AddChecked(VisitExpression(expression.Left), VisitExpression(expression.Right));
                case ExpressionType.And:
                    return Expression.And(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                          expression.Method);
                case ExpressionType.AndAlso:
                    return Expression.AndAlso(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                              expression.Method);
                case ExpressionType.Coalesce:
                    return Expression.Coalesce(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                               VisitExpression(expression.Conversion) as LambdaExpression);
                case ExpressionType.Divide:
                    return Expression.Divide(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                             expression.Method);
                case ExpressionType.Equal:
                    return Expression.Equal(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                            expression.IsLiftedToNull,
                                            expression.Method);
                case ExpressionType.ExclusiveOr:
                    return Expression.ExclusiveOr(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                                  expression.Method);
                case ExpressionType.GreaterThan:
                    return Expression.GreaterThan(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                                  expression.IsLiftedToNull,
                                                  expression.Method);
                case ExpressionType.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(VisitExpression(expression.Left),
                                                         VisitExpression(expression.Right), expression.IsLiftedToNull,
                                                         expression.Method);
                case ExpressionType.LeftShift:
                    return Expression.LeftShift(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                                expression.Method);
                case ExpressionType.LessThan:
                    return Expression.LessThan(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                               expression.IsLiftedToNull, expression.Method);
                case ExpressionType.LessThanOrEqual:
                    return Expression.LessThanOrEqual(VisitExpression(expression.Left),
                                                      VisitExpression(expression.Right), expression.IsLiftedToNull,
                                                      expression.Method);
                case ExpressionType.Modulo:
                    return Expression.Modulo(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                             expression.Method);
                case ExpressionType.Multiply:
                    return Expression.Multiply(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                               expression.Method);
                case ExpressionType.MultiplyChecked:
                    return Expression.MultiplyChecked(VisitExpression(expression.Left),
                                                      VisitExpression(expression.Right), expression.Method);
                case ExpressionType.NotEqual:
                    return Expression.NotEqual(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                               expression.IsLiftedToNull,
                                               expression.Method);
                case ExpressionType.Or:
                    return Expression.Or(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                         expression.Method);
                case ExpressionType.OrElse:
                    return Expression.OrElse(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                             expression.Method);
                case ExpressionType.Power:
                    return Expression.Power(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                            expression.Method);
                case ExpressionType.RightShift:
                    return Expression.RightShift(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                                 expression.Method);
                case ExpressionType.Subtract:
                    return Expression.Subtract(VisitExpression(expression.Left), VisitExpression(expression.Right),
                                               expression.Method);
                case ExpressionType.SubtractChecked:
                    return Expression.SubtractChecked(VisitExpression(expression.Left),
                                                      VisitExpression(expression.Right), expression.Method);
                default:
                    return base.VisitBinaryExpression(expression);
            }
             */
#else
            return expression.Update(VisitExpression(expression.Left), VisitExpression(expression.Conversion) as LambdaExpression,
                                     VisitExpression(expression.Right));
#endif
        }

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            return expression;
        }

        public Expression VisitSelectVariableNameExpression(SelectVariableNameExpression expression)
        {
            _queryBuilder.AddSelectVariable(expression.Name);
            return expression;
        }

        protected override Expression VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
        {
            Expression mappedExpression;
            if (_queryBuilder.TryGetQuerySourceMapping(expression.ReferencedQuerySource, out mappedExpression))
            {
                if (mappedExpression is SelectVariableNameExpression)
                {
                    _queryBuilder.AddSelectVariable((mappedExpression as SelectVariableNameExpression).Name);
                    return mappedExpression;
                }
                return VisitExpression(mappedExpression);
            }
            else
            {
                if (expression.ReferencedQuerySource is AdditionalFromClause)
                {
                    return VisitExpression((expression.ReferencedQuerySource as AdditionalFromClause).FromExpression);
                }
                else if (expression.ReferencedQuerySource is MainFromClause)
                {
                    return VisitExpression((expression.ReferencedQuerySource as MainFromClause).FromExpression);
                }
                else
                {
                    _queryBuilder.AddSelectVariable(expression.ReferencedQuerySource.ItemName);
                }
            }
            return expression;
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
            {
                return VisitExpression(expression.Operand);
            }
#if WINDOWS_PHONE
            return Expression.MakeUnary(expression.NodeType, expression.Operand, expression.Type, expression.Method);
#else
            return expression.Update(VisitExpression(expression.Operand));
#endif
        }

        protected override Expression VisitNewExpression(NewExpression expression)
        {
            var members = expression.Members.ToArray();
            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                var argument = expression.Arguments[i];
                var member = members[i];
                _queryBuilder.StartOptional();
                var resultExpression = VisitExpression(argument);
                _queryBuilder.EndOptional();
                if (resultExpression is SelectVariableNameExpression)
                {
                    var varName = (resultExpression as SelectVariableNameExpression).Name;
                    _queryBuilder.AddSelectVariable(varName);
                    _queryBuilder.AddAnonymousMemberMapping(member.Name, varName);
                }
            }
            return expression;
        }

        protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            if (expression.QueryModel.ResultOperators.Any(r => r is GroupResultOperator))
            {
                // Handle grouped subquery
                SparqlGroupingExpression sparqlGroupingExpression;
                if (_queryBuilder.TryGetGroupingExpression(expression.QueryModel, out sparqlGroupingExpression))
                {
                    return sparqlGroupingExpression;
                }
            }
            else
            {
                var subqueryVistor = new SparqlGeneratorQueryModelVisitor(_queryBuilder.Context, _queryBuilder);
                subqueryVistor.VisitQueryModel(expression.QueryModel);
                return expression;
            }
            return base.VisitSubQueryExpression(expression);
        }

        #region Overrides of ThrowingExpressionTreeVisitor

        // Called when a LINQ expression type is not handled above.
        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            string itemText = FormatUnhandledItem(unhandledItem);
            var message = string.Format("The expression '{0}' (type: {1}) is not supported by this LINQ provider.", itemText, typeof(T));
            return new NotSupportedException(message);
        }

        #endregion

    }
}
