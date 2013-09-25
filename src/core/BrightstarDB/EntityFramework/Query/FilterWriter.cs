using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BrightstarDB.EntityFramework.Query
{
    internal class FilterWriter : ExpressionTreeVisitorBase
    {
        private readonly ExpressionTreeVisitorBase _parent;
        private readonly StringBuilder _filterExpressionBuilder;
        private bool _castAsResourceType;

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

        public FilterWriter(ExpressionTreeVisitorBase parentVisitor, SparqlQueryBuilder queryBuilder, StringBuilder filterExpressionBuilder) : base(queryBuilder)
        {
            _parent = parentVisitor;
            _filterExpressionBuilder = filterExpressionBuilder;
        }

        /// <summary>
        /// Returns the current filter expression
        /// </summary>
        public string FilterExpression { get { return _filterExpressionBuilder.ToString(); } }

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

        protected override Expression VisitMemberExpression(MemberExpression expression)
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
                    var hint = QueryBuilder.Context.GetPropertyHint(propertyInfo);

                    if (hint != null)
                    {
                        switch (hint.MappingType)
                        {
                            case PropertyMappingType.Id:
                                //throw new NotSupportedException("Properties that map to a topic ID cannot be used as part of a filter expression");
                                if (_appendExpressionVariable)
                                {
                                    _filterExpressionBuilder.AppendFormat("?{0}", sourceVarName);
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
                                            _filterExpressionBuilder.AppendFormat("?{0}", existingVarName);
                                        }
                                        return new SelectVariableNameExpression(existingVarName,
                                        hint.MappingType == PropertyMappingType.Arc ? VariableBindingType.Resource : VariableBindingType.Literal,
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
                                            _filterExpressionBuilder.AppendFormat("?{0}", varName);
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
                                            _filterExpressionBuilder.AppendFormat("?{0}", existingVarName);
                                        }
                                        return new SelectVariableNameExpression(existingVarName,
                                                                                VariableBindingType.Resource, 
                                                                                propertyInfo.PropertyType);
                                    }
                                    else
                                    {
                                        var varName = QueryBuilder.NextVariable();
                                        QueryBuilder.AddTripleConstraint(GraphNode.Variable, varName,
                                            GraphNode.Iri, hint.SchemaTypeUri,
                                            GraphNode.Variable, sourceVarName);
                                        if (_appendExpressionVariable)
                                        {
                                            _filterExpressionBuilder.AppendFormat("?{0}", varName);
                                        }
                                        return new SelectVariableNameExpression(varName, VariableBindingType.Resource, propertyInfo.PropertyType);
                                    }
                                }
                            case PropertyMappingType.Address:
                                if (_appendExpressionVariable)
                                {
                                    _filterExpressionBuilder.AppendFormat("?{0}", sourceVarName);
                                }
                                return expression;
                        }
                    }
                    else
                    {
                        if (typeof (String).Equals(propertyInfo.DeclaringType))
                        {
                            if ("Length".Equals(propertyInfo.Name))
                            {
                                WriteFunction("STRLEN", expression.Expression);
                                return expression;
                            }
                        }
                        if (typeof(DateTime).Equals(propertyInfo.DeclaringType))
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
            return base.VisitMemberExpression(expression);
        }

        private static readonly List<Type> NumericTypes = new List<Type>
                                                              {
                                                                  typeof (byte),
                                                                  typeof (short),
                                                                  typeof (ushort),
                                                                  typeof (int),
                                                                  typeof (uint),
                                                                  typeof (long),
                                                                  typeof (ulong),
                                                                  typeof (decimal),
                                                                  typeof (float),
                                                                  typeof (double)
                                                              };

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {


            // Determine the effective expression type (removing Nullable<T> wrapper)
            var expressionType = expression.Type;
            if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition().Equals(typeof(System.Nullable<>)))
            {
                expressionType = expression.Type.GetGenericArguments()[0];
            }
            if (typeof(String).IsAssignableFrom(expressionType))
            {
                _filterExpressionBuilder.Append(MakeSparqlStringConstant(_regexEscaping ? Regex.Escape(expression.Value as String) : expression.Value as string));
                var dt = GetDatatype(typeof (string));
                if (!String.IsNullOrEmpty(dt))
                {
                    _filterExpressionBuilder.AppendFormat("^^<{0}>", dt);
                }
                return expression;
            }
            if (NumericTypes.Contains(expressionType))
            {
                _filterExpressionBuilder.Append(MakeSparqlStringConstant(MakeSparqlNumericConstant(expression.Value)));
                var dt = GetDatatype(expressionType);
                if (!String.IsNullOrEmpty(dt))
                {
                    _filterExpressionBuilder.AppendFormat("^^<{0}>", dt);
                }
                return expression;
            }
            if (expressionType.Equals(typeof(bool)))
            {
                _filterExpressionBuilder.Append(((bool) expression.Value) ? "true" : "false");
                /*var dt = GetDatatype(typeof (bool));
                if (!String.IsNullOrEmpty(dt))
                {
                    _filterExpressionBuilder.AppendFormat("^^<{0}>", dt);
                }
                 */
                return expression;
            }
            if (expressionType.Equals(typeof(DateTime)))
            {

                _filterExpressionBuilder.Append(MakeSparqlStringConstant(((DateTime) expression.Value).ToString("O")));
                var dt = GetDatatype(typeof(DateTime));
                if (!String.IsNullOrEmpty(dt))
                {
                    _filterExpressionBuilder.AppendFormat("^^<{0}>", dt);
                }
                return expression;
            }
            if (typeof(IEnumerable).IsAssignableFrom(expression.Type))
            {
                var enumerable = expression.Value as IEnumerable;
                bool isFirst = true;
                foreach (var o in enumerable)
                {
                    if (o != null)
                    {
                        if (!isFirst)
                        {
                            _filterExpressionBuilder.Append(",");
                        }
                        else
                        {
                            isFirst = false;
                        }
                        _filterExpressionBuilder.Append(MakeSparqlConstant(o));
                        var dt = GetDatatype(typeof(string));
                        if (!String.IsNullOrEmpty(dt))
                        {
                            _filterExpressionBuilder.AppendFormat("^^<{0}>", dt);
                        }
                    }
                }
                return expression;
            }
            if (typeof(IEntityObject).IsAssignableFrom(expression.Type) ||
                QueryBuilder.Context.Mappings.IsKnownInterface(expression.Type))
            {
                var obj = expression.Value as IEntityObject;
                var address = QueryBuilder.Context.GetResourceAddress(obj);
                _filterExpressionBuilder.AppendFormat("<{0}>", address);
                return expression;
            }
            return base.VisitConstantExpression(expression);
        }

        internal string MakeSparqlConstant(object o)
        {
            if (_castAsResourceType)
            {
                Uri u;
                if (o is Uri) u = o as Uri;
                else if (QueryBuilder.Context.IsOfMappedType(o))
                {
                    string resourceAddress = QueryBuilder.Context.GetResourceAddress(o);
                    if (String.IsNullOrEmpty(resourceAddress))
                    {
                        throw new EntityFrameworkException(
                            "Unable to retrieve resource address from object passed in for related resource filtering.");
                    }
                    u = new Uri(resourceAddress);
                }
                else if (!Uri.TryCreate(o.ToString(), UriKind.Absolute, out u))
                {
                    throw new EntityFrameworkException(
                        String.Format(
                            "Unable to convert constant {0} to a URI string as required for related resource filtering.",
                            o));
                }
                return "<" + u + ">";
            }
            var t = o.GetType();
            if (typeof(String).IsAssignableFrom(t))
            {
                return MakeSparqlStringConstant(o as String);
            }
            if (NumericTypes.Contains(t))
            {
                return MakeSparqlNumericConstant(o);
            }
            if (t.Equals(typeof(bool)))
            {
                return ((bool) o) ? "true" : "false";
            }
            return MakeSparqlStringConstant(o.ToString());
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name.Equals("Equals"))
            {
                if(QueryBuilder.Context.Mappings.IsKnownInterface(expression.Object.Type))
                {
                    // Query is testing equality on a property that maps to an entity type
                    if (expression.Arguments[0].NodeType == ExpressionType.Constant)
                    {
                        _filterExpressionBuilder.Append("sameterm(");
                        VisitExpression(expression.Object);
                        _filterExpressionBuilder.Append(",");
                        VisitExpression(expression.Arguments[0]);
                        _filterExpressionBuilder.Append(")");
                        return expression;
                    }
                }
                if (expression.Object.NodeType == ExpressionType.MemberAccess)
                {
                    var ma = expression.Object as MemberExpression;
                    if (ma.Member is PropertyInfo)
                    {
                        var targetProperty = ma.Member as PropertyInfo;
                        var propertyHint = QueryBuilder.Context.GetPropertyHint(targetProperty);
                        if (propertyHint != null && propertyHint.MappingType == PropertyMappingType.Id)
                        {
                            if (expression.Arguments[0].NodeType == ExpressionType.Constant)
                            {
                                var constant = expression.Arguments[0] as ConstantExpression;
                                _filterExpressionBuilder.Append("sameterm(");
                                VisitExpression(expression.Object);
                                _filterExpressionBuilder.Append(",");
                                _filterExpressionBuilder.AppendFormat(
                                    "<{0}>",
                                    QueryBuilder.Context.MapIdToUri(targetProperty, constant.Value.ToString()));
                                _filterExpressionBuilder.Append(")");
                                return expression;
                            }
                            throw new NotSupportedException("Entity ID properties can only be compared to constant values");
                        }
                        // Otherwise fall through to default handling
                    }
                }
                _filterExpressionBuilder.Append('(');
                VisitExpression(expression.Object);
                _filterExpressionBuilder.Append("=");
                VisitExpression(expression.Arguments[0]);
                _filterExpressionBuilder.Append(')');
                return expression;
            }
            if (expression.Object != null && expression.Object.Type == typeof(String))
            {
                if (expression.Method.Name.Equals("StartsWith"))
                {
                    if (expression.Arguments[0].NodeType == ExpressionType.Constant)
                    {
                        var constantExpression = expression.Arguments[0] as ConstantExpression;
                        if (expression.Arguments.Count == 1)
                        {
                            WriteFunction("STRSTARTS", expression.Object, expression.Arguments[0]);
                        }
                        else
                        {
                            var flags = SparqlGeneratorWhereExpressionTreeVisitor.GetStringComparisonFlags(expression.Arguments[1]);
                            WriteRegexFilter(expression.Object,
                                                           "^" + Regex.Escape(constantExpression.Value.ToString()),
                                                           flags);
                            return expression;
                        }
                        return expression;
                    }
                }
                if (expression.Method.Name.Equals("EndsWith"))
                {
                    if (expression.Arguments[0].NodeType == ExpressionType.Constant)
                    {
                        var constantExpression = expression.Arguments[0] as ConstantExpression;
                        if (expression.Arguments.Count == 1)
                        {
                            WriteFunction("STRENDS", expression.Object, expression.Arguments[0]);
                        }
                        if (expression.Arguments.Count > 1 && expression.Arguments[1] is ConstantExpression)
                        {
                            string flags = SparqlGeneratorWhereExpressionTreeVisitor.GetStringComparisonFlags(expression.Arguments[1]);
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
                    if (expression.Arguments[0] is ConstantExpression &&
                        expression.Arguments[0].Type == typeof(int))
                    {
                        start = Expression.Constant(((int)(expression.Arguments[0] as ConstantExpression).Value) + 1);
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
                if (expression.Method.DeclaringType.Equals(typeof(Regex)))
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
                if (typeof(String).Equals(expression.Method.DeclaringType))
                {
                    if (expression.Method.Name.Equals("Concat"))
                    {
                        WriteFunction("CONCAT", expression.Arguments.ToArray());
                        return expression;
                    }
                }
                if (typeof(Math).Equals(expression.Method.DeclaringType))
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
            return base.VisitMethodCallExpression(expression);
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    _filterExpressionBuilder.Append("(!(");
                    VisitExpression(expression.Operand);
                    _filterExpressionBuilder.Append("))");
                    return expression;
            }
            if (expression.NodeType == ExpressionType.Convert)
            {
                // We will get here if a member property is an Enum as LINQ will require it to be cast to its underlying type
                // Ignore the conversion (it should be handled elsewhere)
                VisitExpression(expression.Operand);
                return expression;
            }
            return base.VisitUnaryExpression(expression);
        }

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            return _parent.VisitExpression(expression);
        }

        public void WriteInFilter(Expression itemExpression, Expression fromExpression)
        {
            _filterExpressionBuilder.Append("(");
            var expr = VisitExpression(itemExpression);
            if (expr is SelectVariableNameExpression &&
                ((expr) as SelectVariableNameExpression).BindingType == VariableBindingType.Resource)
            {
                _castAsResourceType = true;
            }
            _filterExpressionBuilder.Append(" IN (");
            VisitExpression(fromExpression);
            _filterExpressionBuilder.Append("))");
            _castAsResourceType = false;
        }

        public void WriteRegexFilter(Expression expression, string s, string flags =null)
        {
            _filterExpressionBuilder.Append("(regex(");
            VisitExpression(expression);
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
                VisitExpression(args[i]);
            }
            _filterExpressionBuilder.Append("))");
        }

        public void WriteInvertedFilterPredicate(Expression predicate)
        {
            _filterExpressionBuilder.Append("(!(");
            bool appendMode = _appendExpressionVariable;
            _appendExpressionVariable = true;
            VisitExpression(predicate);
            _appendExpressionVariable = appendMode;
            _filterExpressionBuilder.Append("))");
        }

        public override string ToString()
        {
            return _filterExpressionBuilder.ToString();
        }
    }
}
