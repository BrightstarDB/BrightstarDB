using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
using BrightstarDB.Query;
using BrightstarDB.Storage.Persistence;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;

namespace BrightstarDB.EntityFramework.Query
{
    internal class SparqlGeneratorWhereExpressionTreeVisitor : ExpressionTreeVisitorBase
    {
        private FilterWriter _filterWriter;
        private readonly bool _optimizeFilter;
        private bool _inBooleanExpression = false;

        private SparqlGeneratorWhereExpressionTreeVisitor(SparqlQueryBuilder queryBuilder, bool optimizeFilter, bool isBoolean)
            : base(queryBuilder)
        {
            QueryBuilder = queryBuilder;
            _filterWriter = new FilterWriter(this, queryBuilder, new StringBuilder());
            _optimizeFilter = optimizeFilter;
            _inBooleanExpression = isBoolean;
        }

        public static Expression GetSparqlExpression(Expression expression, SparqlQueryBuilder queryBuilder, bool filterOptimizationEnabled)
        {
            var canOptimizeFilter = filterOptimizationEnabled && CanOptimizeFilter(expression, queryBuilder);
            var visitor = new SparqlGeneratorWhereExpressionTreeVisitor(queryBuilder, canOptimizeFilter, expression.Type == typeof(bool));
            var returnedExpression = visitor.VisitExpression(expression);
            var svn = returnedExpression as SelectVariableNameExpression;
            if (svn != null && expression.Type == typeof (bool))
            {
                // Single boolean member expression requires a special case addition to the filter
                queryBuilder.AddFilterExpression("(?" + svn.Name + " = true)");
            }
            queryBuilder.AddFilterExpression(visitor.FilterExpression);
            return returnedExpression;
        }

        public static bool CanOptimizeFilter(Expression expression, SparqlQueryBuilder queryBuilder)
        {
            var optimisationChecker = new SparqlGeneratorWhereExpressionOptimisationVisitor(queryBuilder);
            var visitResult = optimisationChecker.VisitExpression(expression);
            return (visitResult is BooleanFlagExpression && ((BooleanFlagExpression) visitResult).Value);
        }

        public string FilterExpression
        {
            get { return _filterWriter.FilterExpression; }
        }

        private static ConstantExpression ExtractConstantExpression(BinaryExpression binaryExpression)
        {
            if (binaryExpression.Left.NodeType == ExpressionType.Constant)
                return binaryExpression.Left as ConstantExpression;
            if (binaryExpression.Right.NodeType == ExpressionType.Constant)
                return binaryExpression.Right as ConstantExpression;
            return null;
        }

        private bool HandleEqualsNotEquals(BinaryExpression equalityExpression)
        {
            var constantExpression = ExtractConstantExpression(equalityExpression);
            if (constantExpression == null) return false;
            var variableExpression = equalityExpression.Left.Equals(constantExpression)
                ? equalityExpression.Right
                : equalityExpression.Left;
            var variablePropertyHint = GetPropertyHint(variableExpression);
            var variableIsAddress = variablePropertyHint != null &&
                                    (variablePropertyHint.MappingType == PropertyMappingType.Address ||
                                     variablePropertyHint.MappingType == PropertyMappingType.Id);
            var value = constantExpression.Value;
            var defValue = variableExpression.Type.GetDefaultValue();
            if ((value == null && defValue == null) || (value != null && value.Equals(defValue)))
            {
                if (variableIsAddress)
                {
                    var querySourceReferenceExpression =
                        ((MemberExpression) variableExpression).Expression as QuerySourceReferenceExpression;
                    if (querySourceReferenceExpression != null)
                    {
                        var varname = querySourceReferenceExpression.ReferencedQuerySource.ItemName;

                        var filter = equalityExpression.NodeType == ExpressionType.NotEqual
                            ? "( bound(?{0}))"
                            : "(!bound(?{0}))";
                        _filterWriter.AppendFormat(filter, varname);
                        return true;
                    }
                }
                else
                {
                    string varname = null;
                    QueryBuilder.StartOptional();
                    var convertedExpression = VisitExpression(variableExpression);
                    QueryBuilder.EndOptional();
                    if (convertedExpression is SelectVariableNameExpression)
                    {
                        varname = (convertedExpression as SelectVariableNameExpression).Name;
                    }
                    if (varname != null)
                    {
                        string filter;
                        if (variableExpression.Type.IsValueType && defValue != null)
                        {
                            filter = equalityExpression.NodeType == ExpressionType.NotEqual
                                ? "(bound(?{0}) && (?{0} != {1}))"
                                : "(!bound(?{0}) || (?{0} = {1}))";
                        }
                        else
                        {
                            filter = equalityExpression.NodeType == ExpressionType.NotEqual
                                ? "( bound(?{0}))"
                                : "(!bound(?{0}))";
                        }
                        if (defValue != null)
                        {
                            _filterWriter.AppendFormat(filter, varname,
                                _filterWriter.MakeSparqlConstant(defValue));
                        }
                        else
                        {
                            _filterWriter.AppendFormat(filter, varname);
                        }
                        return true;
                    }
                }
            }
            if (variableIsAddress && value != null)
            {
                var querySourceReferenceExpression =
                    ((MemberExpression) variableExpression).Expression as QuerySourceReferenceExpression;
                if (querySourceReferenceExpression != null)
                {
                    var varname = querySourceReferenceExpression.ReferencedQuerySource.ItemName;

                    var filter = equalityExpression.NodeType == ExpressionType.Equal
                        ? "( sameTerm(?{0}, <{1}>))"
                        : "(!sameTerm(?{0}, <{1}>))";

                    _filterWriter.AppendFormat(filter, varname,
                        MakeResourceAddress(GetPropertyInfo(constantExpression), value.ToString()));
                    return true;
                }
            }

            return false;
        }

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            var currentlyInBooleanExpression = _inBooleanExpression;
            _inBooleanExpression = false;
            var ret = HandleBinaryExpression(expression);
            _inBooleanExpression = currentlyInBooleanExpression;
            return ret;
        }

        private Expression HandleBinaryExpression(BinaryExpression expression)
        {
            // handle special cases
            if (expression.NodeType == ExpressionType.Equal)
            {
                if (HandleAddressOrIdEquals(expression.Left, expression.Right))
                {
                    return expression;
                }
            }
            if (expression.NodeType == ExpressionType.Equal || expression.NodeType == ExpressionType.NotEqual)
            {
                if (HandleEqualsNotEquals(expression)) return expression;
                var leftPropertyHint = GetPropertyHint(expression.Left);
                var rightPropertyHint = GetPropertyHint(expression.Right);
                var leftIsAddress = leftPropertyHint != null &&
                                    (leftPropertyHint.MappingType == PropertyMappingType.Address ||
                                     leftPropertyHint.MappingType == PropertyMappingType.Id);
                var rightIsAddress = rightPropertyHint != null &&
                                     (rightPropertyHint.MappingType == PropertyMappingType.Address ||
                                      rightPropertyHint.MappingType == PropertyMappingType.Id);
                if (leftIsAddress && rightIsAddress)
                {
                    // Comparing two resource addresses - use the sameTerm function rather then simple equality
                    _filterWriter.Append(expression.NodeType == ExpressionType.Equal
                        ? "(sameTerm("
                        : "(!sameTerm(");
                    _filterWriter.VisitExpression(expression.Left);
                    _filterWriter.Append(",");
                    _filterWriter.VisitExpression(expression.Right);
                    _filterWriter.Append("))");
                    return expression;
                }
            }

            var mce = expression.Left as MethodCallExpression;
            if (mce != null && mce.Method.Name.Equals("Compare"))
            {
                //handle compare (bug 5441)
                HandleCompareExpression(mce, expression.NodeType);
                return expression;
            }

            // Process operators that translate to SPARQL functions
            switch (expression.NodeType)
            {
                case ExpressionType.Or:
                    _filterWriter.WriteFunction(BrightstarFunctionFactory.BrightstarFunctionsNamespace,
                        BrightstarFunctionFactory.BitOr, expression.Left, expression.Right);
                    return expression;
                case ExpressionType.And:
                    _filterWriter.WriteFunction(BrightstarFunctionFactory.BrightstarFunctionsNamespace,
                        BrightstarFunctionFactory.BitAnd, expression.Left, expression.Right);
                    return expression;
            }

            // Process in-fix operator
            if (_optimizeFilter)
            {
                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        QueryBuilder.StartBgpGroup();
                        AppendOptimisedEqualityExpression(expression.Left, expression.Right);
                        QueryBuilder.EndBgpGroup();
                        break;

                    case ExpressionType.NotEqual:
                        QueryBuilder.StartMinus();
                        AppendOptimisedEqualityExpression(expression.Left, expression.Right);
                        QueryBuilder.EndMinus();
                        break;

                    case ExpressionType.OrElse:
                        _inBooleanExpression = true;
                        QueryBuilder.StartBgpGroup();
                        VisitExpression(expression.Left);
                        QueryBuilder.EndBgpGroup();
                        
                        QueryBuilder.Union();
                        
                        QueryBuilder.StartBgpGroup();
                        VisitExpression(expression.Right);
                        QueryBuilder.EndBgpGroup();
                        _inBooleanExpression = false;
                        break;

                    case ExpressionType.AndAlso:
                        _inBooleanExpression = true;
                        VisitExpression(expression.Left);
                        VisitExpression(expression.Right);
                        _inBooleanExpression = false;
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                _filterWriter.Append('(');
                _filterWriter.VisitExpression(expression.Left);
                switch (expression.NodeType)
                {
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        _filterWriter.Append(" + ");
                        break;
                    case ExpressionType.AndAlso:
                        _filterWriter.Append(" && ");
                        break;
                    case ExpressionType.Divide:
                        _filterWriter.Append(" / ");
                        break;
                    case ExpressionType.Equal:
                        _filterWriter.Append(" = ");
                        break;
                    case ExpressionType.GreaterThan:
                        _filterWriter.Append(" > ");
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        _filterWriter.Append(" >= ");
                        break;
                    case ExpressionType.LessThan:
                        _filterWriter.Append(" < ");
                        break;
                    case ExpressionType.LessThanOrEqual:
                        _filterWriter.Append(" <= ");
                        break;
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                        _filterWriter.Append(" * ");
                        break;
                    case ExpressionType.NotEqual:
                        _filterWriter.Append(" != ");
                        break;
                    case ExpressionType.OrElse:
                        _filterWriter.Append(" || ");
                        break;
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                        _filterWriter.Append(" - ");
                        break;
                    default:
                        base.VisitBinaryExpression(expression);
                        break;
                }
                _filterWriter.VisitExpression(expression.Right);
                _filterWriter.Append(')');
            }
            return expression;
        }

        private void AppendOptimisedEqualityExpression(Expression left, Expression right)
        {
            MemberExpression leftMemberExpression = left as MemberExpression;
            if (leftMemberExpression != null)
            {
                if (right is MemberExpression)
                {
                    // Optimise to a join between two BGPs
                    AppendBgpJoin(leftMemberExpression, (MemberExpression) right);
                }
                else if (right is ConstantExpression)
                {
                    AppendPropertyConstraint(GetSourceVarName(leftMemberExpression),
                        GetPropertyHint(leftMemberExpression),
                        (ConstantExpression)right);
                }
            }
            else
            {
                VisitExpression(left);
                VisitExpression(right);
            }
        }

        private void AppendBgpJoin(MemberExpression left, MemberExpression right)
        {
            var joinVar = QueryBuilder.NextVariable();
            var leftSourceVar = GetSourceVarName(left);
            var leftProperty = GetPropertyHint(left);
            var rightSourceVar = GetSourceVarName(right);
            var rightProperty = GetPropertyHint(right);

            AppendPropertyConstraint(leftSourceVar, leftProperty, joinVar);
            AppendPropertyConstraint(rightSourceVar, rightProperty, joinVar);
        }

        private void AppendPropertyConstraint(string sourceVar, PropertyHint propertyHint, string valueVar)
        {
            if (propertyHint.MappingType == PropertyMappingType.InverseArc)
            {
                QueryBuilder.AddTripleConstraint(GraphNode.Variable, valueVar, GraphNode.Iri, propertyHint.SchemaTypeUri,
                    GraphNode.Variable, sourceVar);
            }
            else
            {
                QueryBuilder.AddTripleConstraint(GraphNode.Variable, sourceVar, GraphNode.Iri, propertyHint.SchemaTypeUri,
                    GraphNode.Variable, valueVar);
            }
        }

        private void AppendPropertyConstraint(string sourceVar, PropertyHint propertyHint,
            ConstantExpression constantExpression)
        {
           QueryBuilder.AddTripleConstraint(GraphNode.Variable, sourceVar, GraphNode.Iri, propertyHint.SchemaTypeUri, GraphNode.Raw, QueryBuilder.MakeSparqlConstant(constantExpression.Value, false));
        }

        /// <summary>
        /// Convert a "Compare" expression into a SPARQL greater than / less than
        /// </summary>
        /// <param name="compareExpression"></param>
        /// <param name="nodeType"></param>
        /// <remarks>The Compare expression is used by Microsoft WCF Data Services when a OData call is made with a $skiptoken query option. This token is used to find the starting point in a collection of entities</remarks>
        private void HandleCompareExpression(MethodCallExpression compareExpression, ExpressionType nodeType)
        {
            var filter = nodeType == ExpressionType.GreaterThan
                ? "(?{0} > '{1}')"
                : "(?{0} < '{1}')";
            var expression = compareExpression.Arguments[0] as MemberExpression;
            if (expression == null)
            {
                throw new NotSupportedException(
                    "LINQ to SPARQL does not currently support a comparison expression where the first argument is not a member expression.");
            }
            var sourceVarName = GetSourceVarName(expression);
            var constantExpression = compareExpression.Arguments[1] as ConstantExpression;
            if (constantExpression == null)
            {
                throw new NotSupportedException(
                    "LINQ to SPARQL does not currently support a comparison expression where the second argument is not a constant.");
            }
            var value = constantExpression.Value.ToString();

            if (sourceVarName != null)
            {
#if PORTABLE
                if (expression.Member is PropertyInfo)
#else
                if (expression.Member.MemberType == MemberTypes.Property)
#endif
                {
                    var propertyInfo = expression.Member as PropertyInfo;
                    if (propertyInfo != null)
                    {
                        var hint = QueryBuilder.Context.GetPropertyHint(propertyInfo);

                        if (hint != null)
                        {
                            switch (hint.MappingType)
                            {
                                case PropertyMappingType.Id:
                                    filter = nodeType == ExpressionType.GreaterThan
                                        ? "(str(?{0}) > '{1}')"
                                        : "(str(?{0}) < '{1}')";
                                    var prefix = EntityMappingStore.GetIdentifierPrefix(propertyInfo.DeclaringType);
                                    _filterWriter.AppendFormat(filter, sourceVarName, prefix + value);
                                    break;

                                case PropertyMappingType.Arc:
                                case PropertyMappingType.Property:
                                {
                                    var existingVarName = QueryBuilder.GetVariableForObject(GraphNode.Variable,
                                        sourceVarName,
                                        GraphNode.Iri,
                                        hint.SchemaTypeUri);
                                    if (!string.IsNullOrEmpty(existingVarName))
                                    {

                                        _filterWriter.AppendFormat(filter, existingVarName, value);
                                    }
                                    else
                                    {
                                        var varName = QueryBuilder.NextVariable();
                                        QueryBuilder.AddTripleConstraint(
                                            GraphNode.Variable, sourceVarName,
                                            GraphNode.Iri, hint.SchemaTypeUri,
                                            GraphNode.Variable, varName);
                                        _filterWriter.AppendFormat(filter, varName, value);
                                    }
                                    break;

                                }
                                case PropertyMappingType.InverseArc:
                                {
                                    var existingVarName = QueryBuilder.GetVariableForSubject(GraphNode.Iri,
                                        hint.SchemaTypeUri,
                                        GraphNode.Variable,
                                        sourceVarName);
                                    if (!String.IsNullOrEmpty(existingVarName))
                                    {
                                        _filterWriter.AppendFormat(filter, existingVarName, value);
                                    }
                                    else
                                    {
                                        var varName = QueryBuilder.NextVariable();
                                        QueryBuilder.AddTripleConstraint(GraphNode.Variable, varName,
                                            GraphNode.Iri, hint.SchemaTypeUri,
                                            GraphNode.Variable, sourceVarName);
                                        _filterWriter.AppendFormat(filter, varName, value);
                                    }
                                }
                                    break;
                                case PropertyMappingType.Address:
                                    _filterWriter.AppendFormat(filter, sourceVarName, value);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name.Equals("Equals"))
            {
                if (HandleAddressOrIdEquals(expression.Object, expression.Arguments[0]))
                {
                    return expression;
                }
                if (_optimizeFilter)
                {
                    AppendOptimisedEqualityExpression(expression.Object, expression.Arguments[0]);
                }
                else
                {
                    _filterWriter.VisitExpression(expression);
                }
                return expression;
            }
            if (expression.Object != null && expression.Object.Type == typeof (string))
            {
                if (expression.Method.Name.Equals("StartsWith"))
                {
                    var constantExpression = expression.Arguments[0] as ConstantExpression;
                    if (constantExpression != null)
                    {
                        if (expression.Arguments.Count == 1)
                        {
                            _filterWriter.WriteFunction("STRSTARTS", expression.Object, expression.Arguments[0]);
                        }
                        else
                        {
                            var flags = GetStringComparisonFlags(expression.Arguments[1]);
                            _filterWriter.WriteRegexFilter(expression.Object,
                                "^" + Regex.Escape(constantExpression.Value.ToString()),
                                flags);
                            return expression;
                        }
                        return expression;
                    }
                }
                if (expression.Method.Name.Equals("EndsWith"))
                {
                    var constantExpression = expression.Arguments[0] as ConstantExpression;
                    if (constantExpression != null)
                    {
                        if (expression.Arguments.Count == 1)
                        {
                            _filterWriter.WriteFunction("STRENDS", expression.Object, expression.Arguments[0]);
                        }
                        if (expression.Arguments.Count > 1 && expression.Arguments[1] is ConstantExpression)
                        {
                            var flags = GetStringComparisonFlags(expression.Arguments[1]);
                            _filterWriter.WriteRegexFilter(expression.Object,
                                Regex.Escape(constantExpression.Value.ToString()) + "$",
                                flags);
                        }
                        return expression;
                    }
                }
                if (expression.Method.Name.Equals("Contains"))
                {
                    var seekValue = expression.Arguments[0] as ConstantExpression;
                    if (seekValue != null)
                    {
                        if (expression.Arguments.Count == 1)
                        {
                            _filterWriter.WriteFunction("CONTAINS", expression.Object, seekValue);
                        }
                        else if (expression.Arguments[1] is ConstantExpression)
                        {
                            var flags = GetStringComparisonFlags(expression.Arguments[1]);
                            _filterWriter.WriteRegexFilter(expression.Object, Regex.Escape(seekValue.Value.ToString()),
                                flags);
                        }
                        return expression;
                    }
                }
                if (expression.Method.Name.Equals("Substring"))
                {
                    Expression start;
                    ConstantExpression constantExpression = expression.Arguments[0] as ConstantExpression;
                    if (constantExpression != null && constantExpression.Type == typeof (int))
                    {
                        start = Expression.Constant((int) (constantExpression.Value) + 1);
                    }
                    else
                    {
                        start = Expression.Add(expression.Arguments[0], Expression.Constant(1));
                    }
                    if (expression.Arguments.Count == 1)
                    {
                        _filterWriter.WriteFunction("SUBSTR", expression.Object, start);
                    }
                    else
                    {
                        _filterWriter.WriteFunction("SUBSTR", expression.Object, start, expression.Arguments[1]);
                    }
                }
                if (expression.Method.Name.Equals("ToUpper"))
                {
                    _filterWriter.WriteFunction("UCASE", expression.Object);
                    return expression;
                }
                if (expression.Method.Name.Equals("ToLower"))
                {
                    _filterWriter.WriteFunction("LCASE", expression.Object);
                    return expression;
                }
            }
            if (expression.Object == null)
            {
                // Static method
                var declType = expression.Method.DeclaringType;
                if (declType != null)
                {
                    if (declType == typeof (Regex))
                    {
                        if (expression.Method.Name.Equals("IsMatch"))
                        {
                            var sourceExpression = expression.Arguments[0];
                            var regexExpression = expression.Arguments[1] as ConstantExpression;
                            var flagsExpression = expression.Arguments.Count > 2
                                ? expression.Arguments[2] as ConstantExpression
                                : null;
                            if (regexExpression != null)
                            {
                                var regex = regexExpression.Value.ToString();
                                var flags = string.Empty;
                                if (flagsExpression != null && flagsExpression.Type == typeof (RegexOptions))
                                {
                                    var regexOptions = (RegexOptions) flagsExpression.Value;
                                    if ((regexOptions & RegexOptions.IgnoreCase) == RegexOptions.IgnoreCase)
                                        flags += "i";
                                    if ((regexOptions & RegexOptions.Multiline) == RegexOptions.Multiline) flags += "m";
                                    if ((regexOptions & RegexOptions.Singleline) == RegexOptions.Singleline)
                                        flags += "s";
                                    if ((regexOptions & RegexOptions.IgnorePatternWhitespace) ==
                                        RegexOptions.IgnorePatternWhitespace)
                                        flags += "x";
                                }
                                _filterWriter.WriteRegexFilter(sourceExpression, regex,
                                    string.Empty.Equals(flags) ? null : flags);
                                return expression;
                            }
                        }
                    }
                    if (typeof (string) == declType)
                    {
                        if (expression.Method.Name.Equals("Concat"))
                        {
                            _filterWriter.WriteFunction("CONCAT", expression.Arguments.ToArray());
                            return expression;
                        }
                    }
                    if (typeof (Math) == declType)
                    {
                        string fnName = null;
                        switch (expression.Method.Name)
                        {
                            case "Round":
                                fnName = "ROUND";
                                break;
                            case "Floor":
                                fnName = "FLOOR";
                                break;
                            case "Ceiling":
                                fnName = "CEIL";
                                break;
                        }
                        if (fnName != null)
                        {
                            _filterWriter.WriteFunction(fnName, expression.Arguments[0]);
                            return expression;
                        }
                    }
                }
            }
            return base.VisitMethodCallExpression(expression);
        }

        internal static string GetStringComparisonFlags(Expression comparisonArgument)
        {
            var arg1 = comparisonArgument as ConstantExpression;
            if (arg1 != null)
            {
                if ((arg1.Type == typeof (bool) && (bool) arg1.Value) ||
                    (arg1.Type == typeof (StringComparison) &&
                     ((StringComparison) arg1.Value == StringComparison.CurrentCultureIgnoreCase ||
#if !PORTABLE
                         (StringComparison) arg1.Value == StringComparison.InvariantCultureIgnoreCase ||
#endif
                         (StringComparison) arg1.Value == StringComparison.OrdinalIgnoreCase)))
                {
                    return "i";
                }
            }
            return null;
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                return VisitExpression(expression.Operand);
            }
            if (expression.NodeType == ExpressionType.Not)
            {
                //VisitExpression(expression.Operand);
                return _filterWriter.VisitExpression(expression);
            }
            return base.VisitUnaryExpression(expression);
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            string sourceVarName = GetSourceVarName(expression);

            if (!String.IsNullOrEmpty(sourceVarName))
            {
#if PORTABLE
                if (expression.Member is PropertyInfo)
#else
                if (expression.Member.MemberType == MemberTypes.Property)
#endif
                {
                    var propertyInfo = expression.Member as PropertyInfo;
                    if (propertyInfo != null)
                    {
                        var hint = QueryBuilder.Context.GetPropertyHint(propertyInfo);

                        if (hint != null)
                        {
                            switch (hint.MappingType)
                            {
                                case PropertyMappingType.Id:
                                    return new SelectVariableNameExpression(sourceVarName, VariableBindingType.Resource,
                                        propertyInfo.DeclaringType);

                                case PropertyMappingType.Arc:
                                case PropertyMappingType.Property:
                                {
                                    if (_optimizeFilter && _inBooleanExpression)
                                    {
                                        if (expression.Type == typeof (bool))
                                        {
                                            // Explicitly bind to true
                                            QueryBuilder.AddTripleConstraint(
                                                GraphNode.Variable, sourceVarName, 
                                                GraphNode.Iri, hint.SchemaTypeUri, 
                                                GraphNode.Raw, "true");
                                        }
                                        else
                                        {
                                            // Any binding is acceptable
                                            QueryBuilder.AddTripleConstraint(
                                                GraphNode.Variable, sourceVarName,
                                                GraphNode.Iri, hint.SchemaTypeUri,
                                                GraphNode.Variable, QueryBuilder.NextVariable());
                                        }
                                        return expression;
                                    }
                                    var varName = QueryBuilder.GetVariableForObject(
                                        GraphNode.Variable, sourceVarName,
                                        GraphNode.Iri, hint.SchemaTypeUri);
                                    if (varName == null)
                                    {
                                        varName = QueryBuilder.NextVariable();
                                        QueryBuilder.AddTripleConstraint(
                                            GraphNode.Variable, sourceVarName,
                                            GraphNode.Iri, hint.SchemaTypeUri,
                                            GraphNode.Variable, varName);
                                    }
                                    return new SelectVariableNameExpression(varName,
                                        hint.MappingType == PropertyMappingType.Arc
                                            ? VariableBindingType.Resource
                                            : VariableBindingType.Literal,
                                        propertyInfo.PropertyType);
                                }

                                case PropertyMappingType.Address:
                                    return new SelectVariableNameExpression(sourceVarName, VariableBindingType.Resource,
                                        propertyInfo.PropertyType);

                                case PropertyMappingType.InverseArc:
                                {
                                    var varName = QueryBuilder.GetVariableForSubject(
                                        GraphNode.Iri, hint.SchemaTypeUri,
                                        GraphNode.Variable, sourceVarName);
                                    if (varName == null)
                                    {
                                        varName = QueryBuilder.NextVariable();
                                        QueryBuilder.AddTripleConstraint(
                                            GraphNode.Variable, varName,
                                            GraphNode.Iri, hint.SchemaTypeUri,
                                            GraphNode.Variable, sourceVarName);
                                    }
                                    return new SelectVariableNameExpression(varName,
                                        VariableBindingType.Resource,
                                        propertyInfo.PropertyType);
                                }
                            }
                        }
                    }
                }
            }
            return base.VisitMemberExpression(expression);
        }

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            if (typeof (String).IsAssignableFrom(expression.Type))
            {
                _filterWriter.Append((expression.Value as String));
                return expression;
            }
            return base.VisitConstantExpression(expression);
        }

        protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            if (expression.QueryModel.ResultOperators.Count == 1 &&
                expression.QueryModel.ResultOperators[0] is ContainsResultOperator)
            {
                var contains = (ContainsResultOperator) expression.QueryModel.ResultOperators[0];
                if (expression.QueryModel.MainFromClause.FromExpression.NodeType == ExpressionType.Constant &&
                    contains.Item is MemberExpression)
                {
                    var memberExpression = (MemberExpression) contains.Item;
                    var itemExpression = VisitExpression(contains.Item);
                    var varNameExpression = itemExpression as SelectVariableNameExpression;
                    if (varNameExpression != null)
                    {
                        if (IsIdInArraySubQuery(memberExpression, expression.QueryModel.MainFromClause.FromExpression))
                        {
                            var varName = varNameExpression.Name;
                            // The subquery is a filter on a resource IRI
                            // It is more efficient to use a UNION of BIND triple patterns than a FILTER
                            var values =
                                ((ConstantExpression) expression.QueryModel.MainFromClause.FromExpression).Value as
                                    IEnumerable;
                            if (values != null)
                            {
                                var identifierProperty = memberExpression.Member as PropertyInfo;
                                QueryBuilder.StartUnion();
                                foreach (var value in values)
                                {
                                    QueryBuilder.StartUnionElement();
                                    QueryBuilder.AddBindExpression(
                                        "<" + QueryBuilder.Context.MapIdToUri(identifierProperty, value.ToString()) +
                                        ">", varName);
                                    QueryBuilder.EndUnionElement();
                                }
                                QueryBuilder.EndUnion();
                                return expression;
                            }
                            return base.VisitSubQueryExpression(expression);
                        }
                        else
                        {
                            // The subquery is a filter on a resource property expression
                            // We can translate the subquery to an IN expression
                            _filterWriter.WriteInFilter(itemExpression,
                                expression.QueryModel.MainFromClause.FromExpression);
                            return expression;
                        }
                    }
                }
                else if (expression.QueryModel.MainFromClause.FromExpression.NodeType == ExpressionType.MemberAccess)
                {
                    var itemExpression = VisitExpression(expression.QueryModel.MainFromClause.FromExpression);
                    if (itemExpression is SelectVariableNameExpression)
                    {
                        _filterWriter.WriteInFilter(expression.QueryModel.MainFromClause.FromExpression,
                            contains.Item);
                        return expression;
                    }
                }
            }
            if (expression.QueryModel.ResultOperators.Count == 1 &&
                expression.QueryModel.ResultOperators[0] is AllResultOperator)
            {

                var all = (AllResultOperator) expression.QueryModel.ResultOperators[0];
                var existingWriter = _filterWriter;
                _filterWriter = new FilterWriter(this, QueryBuilder, new StringBuilder()); // TODO: Could check FromExpression to see if it is optimisable
                QueryBuilder.StartNotExists();
                var mappedExpression = VisitExpression(expression.QueryModel.MainFromClause.FromExpression);
                QueryBuilder.AddQuerySourceMapping(expression.QueryModel.MainFromClause, mappedExpression);
                _filterWriter.WriteInvertedFilterPredicate(all.Predicate);
                QueryBuilder.AddFilterExpression(_filterWriter.FilterExpression);
                QueryBuilder.EndNotExists();
                _filterWriter = existingWriter;
                return expression;
            }
            if (expression.QueryModel.ResultOperators.Count == 1 &&
                expression.QueryModel.ResultOperators[0] is AnyResultOperator)
            {
                QueryBuilder.StartExists();
                var outerFilterWriter = _filterWriter;
                _filterWriter = new FilterWriter(this, QueryBuilder, new StringBuilder() );
                var itemVarName = SparqlQueryBuilder.SafeSparqlVarName(expression.QueryModel.MainFromClause.ItemName);

                var mappedFromExpression = VisitExpression(expression.QueryModel.MainFromClause.FromExpression);
                QueryBuilder.AddQuerySourceMapping(expression.QueryModel.MainFromClause, mappedFromExpression);
                if (mappedFromExpression is SelectVariableNameExpression)
                {
                    QueryBuilder.RenameVariable((mappedFromExpression as SelectVariableNameExpression).Name,
                        itemVarName);
                }

                foreach (var bodyClause in expression.QueryModel.BodyClauses)
                {
                    if (bodyClause is WhereClause)
                    {
                        var whereClause = bodyClause as WhereClause;
                        VisitExpression(whereClause.Predicate);
                    }
                    else
                    {
                        CreateUnhandledItemException(bodyClause, "VisitSubQueryExpression");
                    }
                }
                var innerFilter = _filterWriter.ToString();
                if (!string.IsNullOrEmpty(innerFilter))
                {
                    QueryBuilder.AddFilterExpression(innerFilter);
                }
                _filterWriter = outerFilterWriter;
                QueryBuilder.EndExists();
                return expression;
            }
            else if (expression.QueryModel.ResultOperators.Count == 0)
            {
                var itemVarName = SparqlQueryBuilder.SafeSparqlVarName(expression.QueryModel.MainFromClause.ItemName);
                var mappedFromExpression = VisitExpression(expression.QueryModel.MainFromClause.FromExpression);
                QueryBuilder.AddQuerySourceMapping(expression.QueryModel.MainFromClause, mappedFromExpression);
                if (mappedFromExpression is SelectVariableNameExpression)
                {
                    // Rename the variable in the SPARQL so that it matches the LINQ variable
                    QueryBuilder.RenameVariable((mappedFromExpression as SelectVariableNameExpression).Name, itemVarName);
                    (mappedFromExpression as SelectVariableNameExpression).Name = itemVarName;
                }
                foreach (var bodyClause in expression.QueryModel.BodyClauses)
                {
                    if (bodyClause is WhereClause)
                    {
                        var whereClause = bodyClause as WhereClause;
                        VisitExpression(whereClause.Predicate);
                    }
                    else
                    {
                        CreateUnhandledItemException(bodyClause, "VisitSubQueryExpression");
                    }
                }
                return mappedFromExpression;
            }
            return base.VisitSubQueryExpression(expression);
        }

        private bool IsIdInArraySubQuery(MemberExpression memberExpression,
            Expression fromExpression)
        {
            var propertyHint = QueryBuilder.Context.GetPropertyHint(memberExpression.Member as PropertyInfo);
            if (propertyHint.MappingType != PropertyMappingType.Id) return false;
            var constantExpression = fromExpression as ConstantExpression;
            if (constantExpression == null) return false;
            if (!(constantExpression.Value is IEnumerable)) return false;
            return true;
        }

        protected override Expression VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
        {
            Expression mappedExpression;
            if (QueryBuilder.TryGetQuerySourceMapping(expression.ReferencedQuerySource, out mappedExpression))
            {
                return mappedExpression;
            }
            return base.VisitQuerySourceReferenceExpression(expression);
        }

        #region Overrides of ThrowingExpressionTreeVisitor

        // Called when a LINQ expression type is not handled above.
        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            var itemText = FormatUnhandledItem(unhandledItem);
            var message = string.Format("The expression '{0}' (type: {1}) is not supported by this LINQ provider.",
                itemText, typeof (T));
            return new NotSupportedException(message);
        }

        #endregion

        internal Expression VisitExpression(BinaryExpression expression, bool inBooleanExpression)
        {
            var currentlyInBooleanExpression = _inBooleanExpression;
            _inBooleanExpression = inBooleanExpression;
            var ret = VisitExpression(expression);
            _inBooleanExpression = currentlyInBooleanExpression;
            return ret;
        }
    }
}
