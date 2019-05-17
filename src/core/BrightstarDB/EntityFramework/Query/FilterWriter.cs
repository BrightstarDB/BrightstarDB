using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BrightstarDB.Rdf;
using Remotion.Linq.Clauses.Expressions;

namespace BrightstarDB.EntityFramework.Query
{
    internal class FilterWriter : ExpressionTreeVisitorBase
    {
        private readonly SparqlGeneratorWhereExpressionTreeVisitor _parent;
        private readonly StringBuilder _filterExpressionBuilder;
        private bool _castAsResourceType;
        private string _identifierPrefix;

        /// <summary>
        /// Boolean flag that indicates if constant strings should be escaped using Regex.Escape when appending the to the filter.
        /// Defaults to false (off)
        /// </summary>
        private bool _regexEscaping;

        /// <summary>
        /// Flag to ensure that we don't append the variables generated for parent items in a member expression as 
        /// a result of multiple nested calls to VisitMemberExpression
        /// </summary>
        private bool _appendExpressionVariable = true;

        public FilterWriter(SparqlGeneratorWhereExpressionTreeVisitor parentVisitor, SparqlQueryBuilder queryBuilder, 
            StringBuilder filterExpressionBuilder) : base(queryBuilder)
        {
            _parent = parentVisitor;
            _filterExpressionBuilder = filterExpressionBuilder;
        }

        /// <summary>
        /// Returns the current filter expression
        /// </summary>
        public string FilterExpression { get { return _filterExpressionBuilder.ToString(); } }

        public bool InBooleanExpression { get; set; }

        public void Append(string value)
        {
            _filterExpressionBuilder.Append(value);
        }

        public void Append(char value)
        {
            _filterExpressionBuilder.Append(value);
        }

        public void AppendFormat(string fmt, params object[] args)
        {
            _filterExpressionBuilder.AppendFormat(fmt, args);
        }

        #region Overrides of ThrowingExpressionTreeVisitor

        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            string itemText = FormatUnhandledItem(unhandledItem);
            var message = string.Format("The expression '{0}' (type: {1}) is not supported in a FILTER expression.", itemText, typeof(T));
            return new NotSupportedException(message);
        }

        #endregion

        protected override Expression VisitMember(MemberExpression expression)
        {
            bool inAppendMode = _appendExpressionVariable;
            _appendExpressionVariable = false;
            var sourceVarName = GetSourceVarName(expression);
            _appendExpressionVariable = inAppendMode;

            if (sourceVarName != null)
            {
#if PORTABLE
                if (expression.Member is PropertyInfo)
#else
                if (expression.Member.MemberType == MemberTypes.Property)
#endif
                {
                    var propertyInfo = expression.Member as PropertyInfo;
                    if (propertyInfo == null) return base.VisitMember(expression);

                    var hint = QueryBuilder.Context.GetPropertyHint(propertyInfo);
                    if (hint != null)
                    {
                        switch (hint.MappingType)
                        {
                            case PropertyMappingType.Id:
                                //throw new NotSupportedException("Properties that map to a topic ID cannot be used as part of a filter expression");
                                if (_appendExpressionVariable)
                                {
                                    AppendFormat("?{0}", sourceVarName);
                                    return expression;
                                }
                                break;

                            case PropertyMappingType.Arc:
                            case PropertyMappingType.Property:
                            {
                                var existingVarName = QueryBuilder.GetVariableForObject(GraphNode.Variable,
                                    sourceVarName,
                                    GraphNode.Iri,
                                    hint.SchemaTypeUri);
                                if (!String.IsNullOrEmpty(existingVarName))
                                {
                                    if (_appendExpressionVariable)
                                    {
                                        AppendFormat("?{0}", existingVarName);
                                    }
                                    return new SelectVariableNameExpression(existingVarName,
                                        hint.MappingType == PropertyMappingType.Arc
                                            ? VariableBindingType.Resource
                                            : VariableBindingType.Literal,
                                        propertyInfo.PropertyType);
                                }
                                else
                                {
                                    
                                        var varName = QueryBuilder.NextVariable();
                                        QueryBuilder.AddTripleConstraint(
                                            GraphNode.Variable, sourceVarName,
                                            GraphNode.Iri, hint.SchemaTypeUri,
                                            GraphNode.Variable, varName);
                                        if (_appendExpressionVariable)
                                        {
                                            AppendFormat("?{0}", varName);
                                        }
                                        return new SelectVariableNameExpression(
                                            varName,
                                            hint.MappingType == PropertyMappingType.Arc
                                                ? VariableBindingType.Resource
                                                : VariableBindingType.Literal,
                                            propertyInfo.PropertyType);
                                    
                                }
                            }
                            case PropertyMappingType.InverseArc:
                            {
                                var existingVarName = QueryBuilder.GetVariableForSubject(GraphNode.Iri,
                                    hint.SchemaTypeUri,
                                    GraphNode.Variable,
                                    sourceVarName);
                                if (!String.IsNullOrEmpty(existingVarName))
                                {
                                    if (_appendExpressionVariable)
                                    {
                                        AppendFormat("?{0}", existingVarName);
                                    }
                                    return new SelectVariableNameExpression(existingVarName,
                                        VariableBindingType.Resource,
                                        propertyInfo.PropertyType);
                                }
                                var varName = QueryBuilder.NextVariable();
                                QueryBuilder.AddTripleConstraint(GraphNode.Variable, varName,
                                    GraphNode.Iri, hint.SchemaTypeUri,
                                    GraphNode.Variable, sourceVarName);
                                if (_appendExpressionVariable)
                                {
                                    AppendFormat("?{0}", varName);
                                }
                                return new SelectVariableNameExpression(varName, VariableBindingType.Resource,
                                    propertyInfo.PropertyType);
                            }
                            case PropertyMappingType.Address:
                                if (_appendExpressionVariable)
                                {
                                    AppendFormat("?{0}", sourceVarName);
                                }
                                return expression;
                        }
                    }
                    else
                    {
                        if (typeof (PlainLiteral) == (propertyInfo.DeclaringType))
                        {
                            if ("Language".Equals(propertyInfo.Name))
                            {
                                WriteFunction("LANG", expression.Expression);
                                return expression;
                            }
                            if ("Value".Equals(propertyInfo.Name))
                            {
                                WriteFunction("STR", expression.Expression);
                                return expression;
                            }
                        }
                        else if (typeof (string) == propertyInfo.DeclaringType)
                        {
                            if ("Length".Equals(propertyInfo.Name))
                            {
                                WriteFunction("STRLEN", expression.Expression);
                                return expression;
                            }
                        }
                        else if (typeof (DateTime) == propertyInfo.DeclaringType)
                        {
                            string fnName = null;
                            switch (propertyInfo.Name)
                            {
                                case "Day":
                                    fnName = "DAY";
                                    break;
                                case "Hour":
                                    fnName = "HOURS";
                                    break;
                                case "Minute":
                                    fnName = "MINUTES";
                                    break;
                                case "Month":
                                    fnName = "MONTH";
                                    break;
                                case "Second":
                                    fnName = "SECONDS";
                                    break;
                                case "Year":
                                    fnName = "YEAR";
                                    break;
                            }
                            if (fnName != null)
                            {
                                WriteFunction(fnName, expression.Expression);
                                return expression;
                            }
                        }
                    }
                }
            }
            return base.VisitMember(expression);
        }

        

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            try
            {
                Append(QueryBuilder.MakeSparqlConstant(expression.Value, _regexEscaping));
                return expression;
            }
            catch (Exception)
            {
                // Failed to handle the provided expression.
                // TODO: Log the exception?
            }
            return base.VisitConstant(expression);
        }

        internal string MakeSparqlConstant(object o)
        {
            if (_castAsResourceType)
            {
                var u = o as Uri;
                if (u == null)
                {
                    if (QueryBuilder.Context.IsOfMappedType(o))
                    {
                        var resourceAddress = QueryBuilder.Context.GetResourceAddress(o);
                        if (string.IsNullOrEmpty(resourceAddress))
                        {
                            throw new EntityFrameworkException(
                                "Unable to retrieve resource address from object passed in for related resource filtering.");
                        }
                        u = new Uri(resourceAddress);
                    }
                    else if (!string.IsNullOrEmpty(_identifierPrefix))
                    {
                        if (!Uri.TryCreate(_identifierPrefix + o, UriKind.Absolute, out u))
                        {
                            throw new EntityFrameworkException(
                                string.Format(
                                    "Unable to convert constant {0}{1} to a URI string as required for related resource filtering.",
                                    _identifierPrefix, o));
                        }
                    }
                    else if (!Uri.TryCreate(o.ToString(), UriKind.Absolute, out u))
                    {
                        throw new EntityFrameworkException(
                            string.Format(
                                "Unable to convert constant {0} to a URI string as required for related resource filtering.",
                                o));
                    }
                }
                return "<" + u + ">";
            }
            return QueryBuilder.MakeSparqlConstant(o, _regexEscaping);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            var currentlyInBooleanExpression = InBooleanExpression;
            InBooleanExpression = false;
            var ret = HandleMethodCallExpression(expression);
            InBooleanExpression = currentlyInBooleanExpression;
            return ret;
        }

        private Expression HandleMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name.Equals("Equals") && expression.Object != null)
            {
                if(EntityMappingStore.IsKnownInterface(expression.Object.Type))
                {
                    // Query is testing equality on a property that maps to an entity type
                    if (expression.Arguments[0].NodeType == ExpressionType.Constant)
                    {
                        _filterExpressionBuilder.Append("sameterm(");
                        Visit(expression.Object);
                        _filterExpressionBuilder.Append(",");
                        Visit(expression.Arguments[0]);
                        _filterExpressionBuilder.Append(")");
                        return expression;
                    }
                }
                if (expression.Object.NodeType == ExpressionType.MemberAccess)
                {
                    var ma = expression.Object as MemberExpression;
                    if (ma != null)
                    {
                        var targetProperty = ma.Member as PropertyInfo;
                        if (targetProperty != null)
                        {
                            var propertyHint = QueryBuilder.Context.GetPropertyHint(targetProperty);
                            if (propertyHint != null && propertyHint.MappingType == PropertyMappingType.Id)
                            {
                                var constant = expression.Arguments[0] as ConstantExpression;
                                if (constant == null)
                                {
                                    throw new NotSupportedException(
                                        "Entity ID properties can only be compared to constant values");
                                }
                                _filterExpressionBuilder.Append("sameterm(");
                                Visit(expression.Object);
                                _filterExpressionBuilder.Append(",");
                                _filterExpressionBuilder.AppendFormat(
                                    "<{0}>",
                                    QueryBuilder.Context.MapIdToUri(targetProperty, constant.Value.ToString()));
                                _filterExpressionBuilder.Append(")");
                                return expression;
                            }
                            // Otherwise fall through to default handling
                        }
                    }
                }
                _filterExpressionBuilder.Append('(');
                Visit(expression.Object);
                _filterExpressionBuilder.Append("=");
                Visit(expression.Arguments[0]);
                _filterExpressionBuilder.Append(')');
                return expression;
            }
            if (expression.Object != null && expression.Object.Type == typeof(string))
            {
                if (expression.Method.Name.Equals("StartsWith"))
                {
                    var constantExpression = expression.Arguments[0] as ConstantExpression;
                    if (constantExpression != null)
                    {
                        if (expression.Arguments.Count == 1)
                        {
                            WriteFunction("STRSTARTS", expression.Object, expression.Arguments[0]);
                            return expression;
                        }
                        var flags =
                            SparqlGeneratorWhereExpressionTreeVisitor.GetStringComparisonFlags(expression.Arguments[1]);
                        WriteRegexFilter(expression.Object, "^" + Regex.Escape(constantExpression.Value.ToString()),
                            flags);
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
                            WriteFunction("STRENDS", expression.Object, expression.Arguments[0]);
                        }
                        if (expression.Arguments.Count > 1 && expression.Arguments[1] is ConstantExpression)
                        {
                            var flags = SparqlGeneratorWhereExpressionTreeVisitor.GetStringComparisonFlags(expression.Arguments[1]);
                            WriteRegexFilter(expression.Object,
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
                            WriteFunction("CONTAINS", expression.Object, seekValue);
                        }
                        else if (expression.Arguments[1] is ConstantExpression)
                        {
                            var flags = SparqlGeneratorWhereExpressionTreeVisitor.GetStringComparisonFlags(expression.Arguments[1]);
                            WriteRegexFilter(expression.Object, Regex.Escape(seekValue.Value.ToString()),
                                                           flags);
                        }
                        return expression;
                    }
                }
                if (expression.Method.Name.Equals("Substring"))
                {
                    Expression start;
                    var constantExpression = expression.Arguments[0] as ConstantExpression;
                    if (constantExpression != null && expression.Arguments[0].Type == typeof(int))
                    {
                        start = Expression.Constant(((int)constantExpression.Value) + 1);
                    }
                    else
                    {
                        start = Expression.Add(expression.Arguments[0], Expression.Constant(1));
                    }
                    if (expression.Arguments.Count == 1)
                    {
                        WriteFunction("SUBSTR", expression.Object, start);
                        return expression;
                    }
                    if (expression.Arguments.Count == 2)
                    {
                        WriteFunction("SUBSTR", expression.Object, start, expression.Arguments[1]);
                        return expression;
                    }
                }
                if (expression.Method.Name.Equals("ToUpper"))
                {
                    WriteFunction("UCASE", expression.Object);
                    return expression;
                }
                if (expression.Method.Name.Equals("ToLower"))
                {
                    WriteFunction("LCASE", expression.Object);
                    return expression;
                }
                if (expression.Method.Name.Equals("Replace"))
                {
                    _regexEscaping = true;
                    WriteFunction("REPLACE", expression.Object,  expression.Arguments[0], expression.Arguments[1]);
                    _regexEscaping = false;
                    return expression;
                }
            }
            if (expression.Object == null)
            {
                // Static method
                if (expression.Method.DeclaringType == typeof(Regex))
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
                            string flags = String.Empty;
                            if (flagsExpression != null && flagsExpression.Type == typeof(RegexOptions))
                            {
                                var regexOptions = (RegexOptions)flagsExpression.Value;
                                if ((regexOptions & RegexOptions.IgnoreCase) == RegexOptions.IgnoreCase) flags += "i";
                                if ((regexOptions & RegexOptions.Multiline) == RegexOptions.Multiline) flags += "m";
                                if ((regexOptions & RegexOptions.Singleline) == RegexOptions.Singleline) flags += "s";
                                if ((regexOptions & RegexOptions.IgnorePatternWhitespace) == RegexOptions.IgnorePatternWhitespace)
                                    flags += "x";
                            }
                            WriteRegexFilter(sourceExpression, regex, String.Empty.Equals(flags) ? null : flags);
                            return expression;
                        }
                    }
                }
                if (typeof(string) == expression.Method.DeclaringType)
                {
                    if (expression.Method.Name.Equals("Concat"))
                    {
                        WriteFunction("CONCAT", expression.Arguments.ToArray());
                        return expression;
                    }
                }
                if (typeof(Math) == expression.Method.DeclaringType)
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
                        WriteFunction(fnName, expression.Arguments[0]);
                        return expression;
                    }
                }
            }
            return base.VisitMethodCall(expression);
        }

        protected override Expression VisitUnary(UnaryExpression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    _filterExpressionBuilder.Append("(!(");
                    Visit(expression.Operand);
                    _filterExpressionBuilder.Append("))");
                    return expression;
            }
            if (expression.NodeType == ExpressionType.Convert)
            {
                // We will get here if a member property is an Enum as LINQ will require it to be cast to its underlying type
                // Ignore the conversion (it should be handled elsewhere)
                Visit(expression.Operand);
                return expression;
            }
            return base.VisitUnary(expression);
        }

        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression)
        {
            Expression mappedExpression;
            if (QueryBuilder.TryGetQuerySourceMapping(expression.ReferencedQuerySource, out mappedExpression))
            {
#if WINDOWS_PHONE || PORTABLE
                if (mappedExpression is SelectVariableNameExpression)
                {
                    return VisitSelectVariableNameExpression(mappedExpression as SelectVariableNameExpression);
                }
                return VisitExpression(mappedExpression);
#else
                return Visit(mappedExpression);
#endif
            }
            return base.VisitQuerySourceReference(expression);
        }

#if !WINDOWS_PHONE && !PORTABLE
        protected override Expression VisitExtension(Expression expression)
        {
            var selectVariableNameExpression = expression as SelectVariableNameExpression;
            return selectVariableNameExpression != null ? VisitSelectVariableNameExpression(selectVariableNameExpression) : base.VisitExtension(expression);
        }
#endif

        protected Expression VisitSelectVariableNameExpression(SelectVariableNameExpression expression)
        {
            _filterExpressionBuilder.AppendFormat("?{0}", expression.Name);
            return expression;
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            return _parent.VisitBinary(expression, this.InBooleanExpression);
        }

        public void WriteInFilter(Expression itemExpression, Expression fromExpression)
        {
            _filterExpressionBuilder.Append("(");
            var expr = Visit(itemExpression);
            if (expr is SelectVariableNameExpression &&
                ((expr) as SelectVariableNameExpression).BindingType == VariableBindingType.Resource)
            {
                _castAsResourceType = true;
                _identifierPrefix = EntityMappingStore.GetIdentifierPrefix(expr.Type);
                //_identifierPrefix = EntityMappingStore.GetIdentifierPrefix(((SelectVariableNameExpression)expr).
            }
            _filterExpressionBuilder.Append(" IN (");
            Visit(fromExpression);
            _filterExpressionBuilder.Append("))");
            _castAsResourceType = false;
        }

        public void WriteRegexFilter(Expression expression, string s, string flags =null)
        {
            _filterExpressionBuilder.Append("(regex(");
            Visit(expression);
            _filterExpressionBuilder.Append(", ");
            _filterExpressionBuilder.Append(MakeSparqlConstant(s));
            if (flags != null)
            {
                _filterExpressionBuilder.Append(", ");
                _filterExpressionBuilder.Append(MakeSparqlConstant(flags));
            }
            _filterExpressionBuilder.Append("))");
        }

        public void WriteFunction(string extensionNamespace, string fnName, params Expression[] args)
        {
            var prefix = QueryBuilder.AssertPrefix(extensionNamespace);
            WriteFunction(prefix + ":" + fnName, args);
        }

        public void WriteFunction(string fnName, params Expression[] args)
        {
            _filterExpressionBuilder.AppendFormat("({0}(", fnName);
            for(int i =0;i < args.Length;i++)
            {
                if (i > 0) _filterExpressionBuilder.Append(", ");
                Visit(args[i]);
            }
            _filterExpressionBuilder.Append("))");
        }

        public void WriteInvertedFilterPredicate(Expression predicate)
        {
            _filterExpressionBuilder.Append("(!(");
            bool appendMode = _appendExpressionVariable;
            _appendExpressionVariable = true;
            Visit(predicate);
            _appendExpressionVariable = appendMode;
            _filterExpressionBuilder.Append("))");
        }

        public override string ToString()
        {
            return _filterExpressionBuilder.ToString();
        }
    }
}
