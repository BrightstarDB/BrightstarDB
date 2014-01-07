using System;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;

namespace BrightstarDB.EntityFramework.Query
{
    internal class SparqlGeneratorQueryModelVisitor : QueryModelVisitorBase
    {
        private readonly EntityContext _context;
        private readonly SparqlQueryBuilder _queryBuilder;
        private bool _isInstanceQuery;
        private string _instanceUri;
        private string _typeUri;

        public static SparqlLinqQueryContext GenerateSparqlLinqQuery(EntityContext context, QueryModel queryModel)
        {
            var visitor = new SparqlGeneratorQueryModelVisitor(context);
            visitor.VisitQueryModel(queryModel);
            var resultType = queryModel.GetResultType();
            if (resultType.IsGenericType)
            {
                resultType = resultType.GetGenericArguments()[0];
            }
            var bindType = context.GetImplType(resultType);
            bool useDescribe = typeof (IEntityObject).IsAssignableFrom(bindType);
            return visitor.GetSparqlQuery(useDescribe);
        }

        SparqlGeneratorQueryModelVisitor(EntityContext context)
        {
            _context = context;
            _queryBuilder  = new SparqlQueryBuilder(context);
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            if (queryModel.BodyClauses.Count == 1 
                && queryModel.BodyClauses[0] is Remotion.Linq.Clauses.WhereClause
                && queryModel.SelectClause.Selector is QuerySourceReferenceExpression
                && (queryModel.SelectClause.Selector as QuerySourceReferenceExpression).ReferencedQuerySource.Equals(queryModel.MainFromClause))
            {
                var whereClause = queryModel.BodyClauses[0] as Remotion.Linq.Clauses.WhereClause;
                string instanceId = null;
                string typeId = null;
                if (queryModel.MainFromClause.FromExpression.NodeType == ExpressionType.Constant)
                {
                    var c = queryModel.MainFromClause;
                    typeId = _context.MapTypeToUri(c.ItemType);
                    if (whereClause.Predicate.NodeType == ExpressionType.Equal)
                    {
                        var equals = whereClause.Predicate as BinaryExpression;
                        if (equals.Left.NodeType == ExpressionType.MemberAccess &&
                            equals.Right.NodeType == ExpressionType.Constant)
                        {
                            var constExpression = equals.Right as ConstantExpression;
                            var memberExpression = equals.Left as MemberExpression;
                            if (constExpression != null && memberExpression != null && memberExpression.Expression is QuerySourceReferenceExpression)
                            {
                                var qsr = memberExpression.Expression as QuerySourceReferenceExpression;
                                if (qsr.ReferencedQuerySource.Equals(queryModel.MainFromClause) &&
#if PORTABLE
                                    memberExpression.Member is PropertyInfo)
#else
                                    memberExpression.Member.MemberType == MemberTypes.Property)
#endif
                                {
                                    var propertyHint =
                                        _queryBuilder.Context.GetPropertyHint(memberExpression.Member as PropertyInfo);
                                    if (propertyHint != null)
                                    {
                                        if (propertyHint.MappingType == PropertyMappingType.Address)
                                        {
                                            instanceId = constExpression.Value.ToString();
                                        }
                                        else if (propertyHint.MappingType == PropertyMappingType.Id)
                                        {
                                            instanceId =
                                                _queryBuilder.Context.MapIdToUri(
                                                    memberExpression.Member as PropertyInfo,
                                                    constExpression.Value.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    } 
                    else if (whereClause.Predicate.NodeType == ExpressionType.Call)
                    {
                        var call = whereClause.Predicate as MethodCallExpression;
                        if (call.Method.Name.Equals("Equals") && call.Arguments.Count == 1 &&
                            call.Arguments[0] is ConstantExpression)
                        {
                            if (call.Object.NodeType == ExpressionType.MemberAccess)
                            {
                                var member = call.Object as MemberExpression;
                                if (member != null && member.Expression is QuerySourceReferenceExpression)
                                {
                                    if (
                                        (member.Expression as QuerySourceReferenceExpression).ReferencedQuerySource.
                                            Equals(
                                                queryModel.MainFromClause))
                                    {
#if PORTABLE
                                        if (member.Member is PropertyInfo)
#else
                                        if (member.Member.MemberType == MemberTypes.Property)
#endif
                                        {
                                            var propertyHint =
                                                _queryBuilder.Context.GetPropertyHint(member.Member as PropertyInfo);
                                            if (propertyHint != null)
                                            {
                                                if (propertyHint.MappingType == PropertyMappingType.Address)
                                                {
                                                    instanceId = (call.Arguments[0] as ConstantExpression).Value as string;
                                                }
                                                else if (propertyHint.MappingType == PropertyMappingType.Id)
                                                {
                                                    instanceId = _context.MapIdToUri(member.Member as PropertyInfo,
                                                                                   (call.Arguments[0] as
                                                                                    ConstantExpression)
                                                                                       .Value as string);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (typeId != null && instanceId != null)
                {
                    _isInstanceQuery = true;
                    _typeUri = typeId;
                    _instanceUri = instanceId;
                }
            }

            base.VisitQueryModel(queryModel);
        }

        public override void VisitMainFromClause(Remotion.Linq.Clauses.MainFromClause fromClause, QueryModel queryModel)
        {
            if (fromClause.FromExpression is SubQueryExpression)
            {
                var subquery = fromClause.FromExpression as SubQueryExpression;
                VisitQueryModel(subquery.QueryModel);
            }
            else
            {
                _queryBuilder.AddFromPart(fromClause);
            }
            base.VisitMainFromClause(fromClause, queryModel);
        }

        public override void VisitAdditionalFromClause(Remotion.Linq.Clauses.AdditionalFromClause fromClause, QueryModel queryModel, int index)
        {
            _isInstanceQuery = false;
            var fromVar = _queryBuilder.AddFromPart(fromClause);
            if (!(fromClause.FromExpression is ConstantExpression))
            {
                var fromExpression =
                    SparqlGeneratorWhereExpressionTreeVisitor.GetSparqlExpression(fromClause.FromExpression,
                                                                                  _queryBuilder);
                if (fromExpression is SelectVariableNameExpression)
                {
                    _queryBuilder.RenameVariable((fromExpression as SelectVariableNameExpression).Name,fromVar);
                }
            }
            base.VisitAdditionalFromClause(fromClause, queryModel, index);
        }

        public override void VisitSelectClause(Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel)
        {
            // referenceClaused -> fromCluase1
            SparqlGeneratorSelectExpressionTreeVisitor.GetSparqlExpression(selectClause.Selector, _queryBuilder);
            base.VisitSelectClause(selectClause, queryModel);
        }

        public override void VisitWhereClause(Remotion.Linq.Clauses.WhereClause whereClause, QueryModel queryModel, int index)
        {
            SparqlGeneratorWhereExpressionTreeVisitor.GetSparqlExpression(whereClause.Predicate, _queryBuilder);
            base.VisitWhereClause(whereClause, queryModel, index);
        }

        public override void VisitJoinClause(Remotion.Linq.Clauses.JoinClause joinClause, QueryModel queryModel, int index)
        {
            _isInstanceQuery = false;
            _queryBuilder.AddFromPart(joinClause);
            var inner = SparqlGeneratorWhereExpressionTreeVisitor.GetSparqlExpression(joinClause.InnerKeySelector, _queryBuilder);
            var outer = SparqlGeneratorWhereExpressionTreeVisitor.GetSparqlExpression(joinClause.OuterKeySelector, _queryBuilder);
            if (inner is SelectVariableNameExpression && outer is SelectVariableNameExpression)
            {
                var innerVar = inner as SelectVariableNameExpression;
                var outerVar = outer as SelectVariableNameExpression;
                if (innerVar.BindingType == VariableBindingType.Literal ||
                    outerVar.BindingType == VariableBindingType.Literal)
                {
                    _queryBuilder.AddFilterExpression(string.Format("(?{0}=?{1})", innerVar.Name, outerVar.Name));
                }
                else
                {
                    _queryBuilder.AddFilterExpression(string.Format("(sameTerm(?{0}, ?{1}))", innerVar.Name, outerVar.Name));
                }
            }
            else
            {
                throw new NotSupportedException(
                    String.Format("No support for joining expressions of type {0} and {1}", inner.NodeType,
                                  outer.NodeType));
            }
        }

        public override void VisitOrdering(Remotion.Linq.Clauses.Ordering ordering, QueryModel queryModel, Remotion.Linq.Clauses.OrderByClause orderByClause, int index)
        {
            _isInstanceQuery = false;
            var selector = SparqlGeneratorWhereExpressionTreeVisitor.GetSparqlExpression(ordering.Expression,
                                                                                         _queryBuilder);
            if (selector is SelectVariableNameExpression)
            {
                var selectorVar = selector as SelectVariableNameExpression;
                _queryBuilder.AddOrdering(new SparqlOrdering("?" + selectorVar.Name, ordering.OrderingDirection));
            }
            else
            {
                throw new NotSupportedException(
                    String.Format("LINQ-to-SPARQL does not currently support ordering by the expression '{0}'", ordering.Expression));
            }
            base.VisitOrdering(ordering, queryModel, orderByClause, index);
        }

        public override void VisitResultOperator(Remotion.Linq.Clauses.ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            if (index > 0) _isInstanceQuery = false;
            if (resultOperator is TakeResultOperator)
            {
                var take = resultOperator as TakeResultOperator;
                _queryBuilder.Limit = take.GetConstantCount();
                if (_queryBuilder.Limit != 1) _isInstanceQuery = false;
                return;
            }
            if (resultOperator is SkipResultOperator)
            {
                var skip = resultOperator as SkipResultOperator;
                _queryBuilder.Offset = skip.GetConstantCount();
                return;
            }
            if (resultOperator is AverageResultOperator && index == 0)
            {
                var varName = GetExpressionVariable(queryModel.SelectClause.Selector);
                if (varName != null)
                {
                    _queryBuilder.ApplyAggregate("AVG", varName);
                    return;
                }
            }
            if (resultOperator is SumResultOperator && index == 0)
            {
                var varName = GetExpressionVariable(queryModel.SelectClause.Selector);
                if (varName != null)
                {
                    _queryBuilder.ApplyAggregate("SUM", varName);
                    return;
                }
            }
            if (resultOperator is CountResultOperator && index == 0)
            {
                var varName = GetExpressionVariable(queryModel.SelectClause.Selector);
                if (varName != null)
                {
                    _queryBuilder.ApplyAggregate("COUNT", varName);
                    return;
                }
            }
            if (resultOperator is LongCountResultOperator && index == 0)
            {
                var varName = GetExpressionVariable(queryModel.SelectClause.Selector);
                if (varName != null)
                {
                    _queryBuilder.ApplyAggregate("COUNT", varName);
                    return;
                }
            }
            if (resultOperator is MinResultOperator && index == 0)
            {
                var varName = GetExpressionVariable(queryModel.SelectClause.Selector);
                if (varName != null)
                {
                    _queryBuilder.ApplyAggregate("MIN", varName);
                    return;
                }
            }
            if (resultOperator is MaxResultOperator && index == 0)
            {
                var varName = GetExpressionVariable(queryModel.SelectClause.Selector);
                if (varName != null)
                {
                    _queryBuilder.ApplyAggregate("MAX", varName);
                    return;
                }
            }
            if (resultOperator is SingleResultOperator && index == 0)
            {
                // Grab first 2 rows. If there are two then the outer wrapper will fail.
                _queryBuilder.Limit = 2;
                return;
            }
            if (resultOperator is FirstResultOperator)
            {
                _queryBuilder.Limit = 1;
                return;
            }
            if (resultOperator is OfTypeResultOperator)
            {
                var ofType = resultOperator as OfTypeResultOperator;
                var varName = GetExpressionVariable(queryModel.SelectClause.Selector);
                if (varName != null)
                {
                    var typeUri = _queryBuilder.Context.MapTypeToUri(ofType.SearchedItemType);
                    if (typeUri != null)
                    {
                        _queryBuilder.AddTripleConstraint(
                            GraphNode.Variable, varName,
                            GraphNode.Raw, "a",
                            GraphNode.Iri, typeUri);
                        return;
                    }
                    throw new EntityFrameworkException("No URI mapping found for type '{0}'",
                                                       ofType.SearchedItemType);
                }
            }
            if (resultOperator is DistinctResultOperator)
            {
                _queryBuilder.IsDistinct = true;
                return;
            }
            if (resultOperator is GroupResultOperator)
            {
                var groupOperator = resultOperator as GroupResultOperator;
                var keyExpr = groupOperator.KeySelector;
                var exprVar = GetExpressionVariable(keyExpr);
                if (exprVar == null)
                {
                    throw new EntityFrameworkException("Unable to convert GroupBy '{0}' operator to SPARQL.",
                                                       groupOperator);
                }
                _queryBuilder.AddGroupByExpression("?" + exprVar);
                return;
            }
            if (resultOperator is CastResultOperator)
            {
                var castOperator = resultOperator as CastResultOperator;
                var mappedUri = _queryBuilder.Context.MapTypeToUri(castOperator.CastItemType);
                if (mappedUri == null)
                {
                    throw new EntityFrameworkException("Unable to cast to type '{0}' as it is not a valid entity type.", castOperator.CastItemType);
                }
                return;
            }
            if (index > 0)
            {
                throw new NotSupportedException(
                    String.Format(
                        "LINQ-to-SPARQL does not currently support the result operator '{0}' as a second or subsequent result operator.",
                        resultOperator));
            }
            throw new NotSupportedException(
                String.Format("LINQ-to-SPARQL does not currently support the result operator '{0}'", resultOperator));
        }

        private string GetExpressionVariable(Expression expression)
        {
            if (expression is QuerySourceReferenceExpression)
            {
                var querySource = expression as QuerySourceReferenceExpression;
                Expression mappedExpression;
                _queryBuilder.TryGetQuerySourceMapping(querySource.ReferencedQuerySource, out mappedExpression);
                if (mappedExpression is SelectVariableNameExpression)
                    return (mappedExpression as SelectVariableNameExpression).Name;
            }
            var selector = SparqlGeneratorWhereExpressionTreeVisitor.GetSparqlExpression(expression, _queryBuilder);
            if (selector is SelectVariableNameExpression) return (selector as SelectVariableNameExpression).Name;
            return null;
        }

        public SparqlLinqQueryContext GetSparqlQuery(bool useDescribe)
        {
            if (_isInstanceQuery)
            {
                return new SparqlLinqQueryContext(_instanceUri, _typeUri);
            }
            return
                new SparqlLinqQueryContext(
                    useDescribe && !_queryBuilder.IsDistinct && !_queryBuilder.IsOrdered
                        ? _queryBuilder.GetSparqlDescribeString()
                        : _queryBuilder.GetSparqlString(),
                    _queryBuilder.AnonymousMembersMap,
                    _queryBuilder.Constructor,
                    _queryBuilder.ConstructorArgs,
                    _queryBuilder.MembersMap, _queryBuilder.MemberInitExpression);
        }

    }
}
