using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;

namespace BrightstarDB.EntityFramework.Query
{
    internal class SparqlQueryBuilder
    {
        private int _varCount;
        private readonly List<String> _selectVars = new List<string>(1);
        private readonly List<Tuple<String, String>> _aggregates = new List<Tuple<string, string>>(1);
        private readonly EntityContext _context;
        private readonly StringBuilder _graphPatternBuilder;
        private readonly QuerySourceMapping _querySourceMapping;
        private readonly Dictionary<string, string> _variableValueMapping;
        private readonly List<SparqlOrdering> _ordering;
        private readonly List<Tuple<string, string>> _anonymousMembersMap;
        private readonly List<Tuple<MemberInfo, string>>_membersMap;
        private readonly List<string> _groupByExpressions;
        private readonly List<string> _constructorArgs;
        private readonly Dictionary<QueryModel, SparqlGroupingExpression> _groupingExpressions; 
        private readonly Dictionary<string, string> _prefixes = new Dictionary<string, string>();

        /// <summary>
        /// List that captures the named variables extracted from the LINQ query to guard against clashes.
        /// </summary>
        private readonly List<string> _namedVariables = new List<string>();

        public List<TripleInfo> AllTriples { get; private set; }

        public int Limit { get; set; }
        public int Offset { get; set; }
        public bool IsDistinct { get; set; }
        public bool IsOrdered { get { return _ordering.Count > 0; } }

        public SparqlQueryBuilder(EntityContext context)
        {
            _context = context;
            _graphPatternBuilder =new StringBuilder();
            _querySourceMapping = new QuerySourceMapping();
            _variableValueMapping = new Dictionary<string, string>();
            _ordering = new List<SparqlOrdering>();
            _anonymousMembersMap = new List<Tuple<string, string>>();
            _membersMap = new List<Tuple<MemberInfo, string>>();
            _groupByExpressions = new List<string>();
            _constructorArgs= new List<string>();
            _groupingExpressions = new Dictionary<QueryModel, SparqlGroupingExpression>();
            AllTriples = new List<TripleInfo>();
        }

        public EntityContext Context { get { return _context; } }

        public IEnumerable<String> SelectVariables { get { return _selectVars.AsReadOnly(); } }

        /// <summary>
        /// Gets a list of the anonymous types members and their mapping to SPARQL results columns
        /// </summary>
        public List<Tuple<string, string>> AnonymousMembersMap { get { return _anonymousMembersMap; } }

        /// <summary>
        /// Gets a dictionary that maps the members assigned by the <see cref="MemberInitExpression"/>
        /// to the matching variables in the SPARQL results set.
        /// </summary>
        public List<Tuple<MemberInfo, string>> MembersMap { get { return _membersMap; } }

        public Expression MemberInitExpression { get; internal set; }

        public ConstructorInfo Constructor { get; internal set; }

        public List<string> ConstructorArgs { get { return _constructorArgs; } }

        public string NextVariable()
        {
            return "v" + _varCount++;
        }

        public void AddSelectVariable(string varName)
        {
            if (!_selectVars.Contains(varName))
            {
                _selectVars.Add(varName);
                if (_variableValueMapping.ContainsKey(varName))
                {
                    AddFilterExpression(String.Format("(?{0} = {1})", varName, _variableValueMapping[varName]));
                }
            }
        }

        public string AddFromPart(IQuerySource querySource)
        {
            
            var typeUri = _context.MapTypeToUri(querySource.ItemType);
            var itemVarName = IntroduceNamedVariable(querySource.ItemName);
            AddTripleConstraint(
                GraphNode.Variable, 
                itemVarName,
                GraphNode.Raw, "a",
                GraphNode.Iri, typeUri);
            AddQuerySourceMapping(querySource, new SelectVariableNameExpression(itemVarName, VariableBindingType.Resource, querySource.ItemType));
            if (querySource is MainFromClause)
            {
                var mfc = querySource as MainFromClause;
                if (mfc.FromExpression is ConstantExpression)
                {
                    var fromExpr = mfc.FromExpression as ConstantExpression;
                    if (fromExpr.Value is IBrightstarEntityCollection)
                    {
                        var fromCollection = fromExpr.Value as IBrightstarEntityCollection;
                        if (fromCollection.IsInverseProperty)
                        {

                            AddTripleConstraint(GraphNode.Variable, itemVarName,
                                                GraphNode.Iri, fromCollection.PropertyIdentity,
                                                GraphNode.Iri, fromCollection.ParentIdentity);
                        }
                        else
                        {
                            AddTripleConstraint(GraphNode.Iri, fromCollection.ParentIdentity,
                                                GraphNode.Iri, fromCollection.PropertyIdentity,
                                                GraphNode.Variable, itemVarName);
                        }
                    }
                }
            }
            return itemVarName;
        }

        public string IntroduceNamedVariable(string varName)
        {
            var safeVarName = SafeSparqlVarName(varName);
            if (this._namedVariables.Contains(safeVarName))
            {
                var suffix = 1;
                while (this._namedVariables.Contains(safeVarName + suffix)) suffix++;
                safeVarName = safeVarName + suffix;
            }
            this._namedVariables.Add(safeVarName);
            return safeVarName;
        }

        /// <summary>
        /// Converts the input string into a string which is safe for use as a SPARQL variable name.
        /// </summary>
        /// <param name="varName">The input string to be converted</param>
        /// <returns>The input string with characters that are not valid by the SPARQL VARNAME production 
        /// replaced with the string 'x' followed by the 4-digit hex value of the character.</returns>
        public static string SafeSparqlVarName(string varName)
        {
            var sb = new StringBuilder();
            foreach(var c in varName)
            {
                if ( (c >= 0x41 && c <= 0x5A) ||
                    (c>=0x61 && c<=0x7A) ||
                    (c>=0x00C0&& c<=0x00D6) || 
                    (c>=0x00D8 && c<=0x00F6) || 
                    (c>=0x00F8 && c<=0x02FF) || 
                    (c>=0x0370 && c<=0x037D) || 
                    (c>=0x037F && c<=0x1FFF) || 
                    (c>=0x200C && c<=0x200D) || 
                    (c>=0x2070 && c<=0x218F) || 
                    (c>=0x2C00 && c<=0x2FEF) || 
                    (c>=0x3001 && c<=0xD7FF) || 
                    (c>=0xF900 && c<=0xFDCF) || 
                    (c>=0xFDF0 && c<=0xFFFD) )
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(String.Format("x{0:X4}", (uint)c));
                }
            }
            return sb.ToString();
        }

        public string GetSparqlDescribeString()
        {
            return GetPrefixes() + "DESCRIBE " + GetSparqlQuery();
        }

        public String GetSparqlString()
        {
            return GetPrefixes() + "SELECT " + GetSparqlQuery();
        }

        private string GetPrefixes()
        {
            if (_prefixes.Count > 0)
            {
                var prefixesBuilder = new StringBuilder();
                foreach(var kvp in _prefixes)
                {
                    prefixesBuilder.AppendFormat("PREFIX {0}: <{1}>\n", kvp.Value, kvp.Key);
                }
                return prefixesBuilder.ToString();
            }
            return string.Empty;
        }

        private string GetSparqlQuery()
        {
            var queryStringBuilder = new StringBuilder();
            if (IsDistinct) queryStringBuilder.Append("DISTINCT ");
            foreach(var sv in _selectVars)
            {
                queryStringBuilder.AppendFormat("?{0} ", sv);
            }
            foreach(var ag in _aggregates)
            {
                queryStringBuilder.AppendFormat("({0} AS ?{1}) ", ag.Item2, ag.Item1);
            }
            queryStringBuilder
                .Append("WHERE {")
                .Append(_graphPatternBuilder.ToString())
                .Append("}");
            if (_groupByExpressions.Count > 0)
            {
                queryStringBuilder.Append(" GROUP BY ");
                queryStringBuilder.Append(String.Join(" ", _groupByExpressions));
            }
            if (_ordering.Count > 0)
            {
                queryStringBuilder.Append(" ORDER BY ");
                queryStringBuilder.Append(String.Join(" ", _ordering));
            }
            if (Limit > 0)
            {
                queryStringBuilder.Append(" LIMIT ");
                queryStringBuilder.Append(Limit);
            }
            if (Offset > 0)
            {
                queryStringBuilder.Append(" OFFSET ");
                queryStringBuilder.Append(Offset);
            }
            var sparqlString = queryStringBuilder.ToString();
            return ReplaceFixedVariables(sparqlString);
        }

        public void AddTripleConstraint(GraphNode subjectType, string subject, GraphNode verbType, string verb, GraphNode objectType, string obj)
        {
            _graphPatternBuilder.Append(Stringify(subjectType, subject));
            _graphPatternBuilder.Append(' ');
            _graphPatternBuilder.Append(Stringify(verbType, verb));
            _graphPatternBuilder.Append(' ');
            _graphPatternBuilder.Append(Stringify(objectType, obj));
            _graphPatternBuilder.Append(" .");
            AllTriples.Add(new TripleInfo(subjectType, subject, verbType, verb, objectType, obj));
        }

        public void AddGroupByExpression(QueryModel groupedQueryModel, SparqlGroupingExpression groupingExpression)
        {
            if (!_groupingExpressions.ContainsKey(groupedQueryModel))
            {
                foreach (var varNameBinding in groupingExpression.GroupVars)
                {
                    _groupByExpressions.Add("?" + varNameBinding.Name);
                }
                _groupingExpressions.Add(groupedQueryModel, groupingExpression);
            }
        }

        private static String Stringify(GraphNode nodeType, string nodeValue)
        {
            switch (nodeType)
            {
                case GraphNode.Iri:
                    return String.Format("<{0}>", nodeValue);
                case GraphNode.Literal:
                    if (nodeValue.Contains("'"))
                    {
                        if (nodeValue.Contains("\""))
                        {
                            return String.Format("'''{0}'''", nodeValue);
                        }
                        return String.Format("\"{0}\"", nodeValue);
                    }
                    return String.Format("'{0}'", nodeValue);
                case GraphNode.Variable:
                    return String.Format("?{0}", nodeValue);
                case GraphNode.Raw:
                    return nodeValue;
            }
            return String.Empty;
        }

        public void StartOptional()
        {
            _graphPatternBuilder.Append(" OPTIONAL {");
        }

        public void EndOptional()
        {
            _graphPatternBuilder.Append("} ");
        }

        public void AddFilterExpression(string filterExpression)
        {
            if (!string.IsNullOrEmpty(filterExpression))
            {
                _graphPatternBuilder.Append("FILTER ");
                _graphPatternBuilder.Append(filterExpression);
                _graphPatternBuilder.Append(".");
            }
        }

        public void AddQuerySourceMapping(IQuerySource querySource, Expression mappedExpression)
        {
            _querySourceMapping.AddMapping(querySource, mappedExpression);
        }

        public bool TryGetQuerySourceMapping(IQuerySource querySource, out Expression mappedExpression)
        {
            if (_querySourceMapping.ContainsMapping(querySource))
            {
                mappedExpression = _querySourceMapping.GetExpression(querySource);
                return true;
            }
            mappedExpression = null;
            return false;
        }

        public bool TryGetGroupingExpression(QueryModel queryModel, out SparqlGroupingExpression sparqlGroupingExpression)
        {
            return _groupingExpressions.TryGetValue(queryModel, out sparqlGroupingExpression);
        }

        public void ConvertVariableToConstantUri(string varName, string uri)
        {
            if (!_variableValueMapping.ContainsKey(varName))
            {
                _variableValueMapping[varName] = "<" + uri + ">";
            }
        }

        private string ReplaceFixedVariables(string query)
        {
            foreach(string varName in _variableValueMapping.Keys)
            {
                if (_selectVars.Contains(varName))
                {
                    // selected variables cannot be replaced
                    continue;
                }
                string matchPattern = @"([\s+|\.|\{|\(])\?(" + Regex.Escape(varName) + @")([\s+|,])";
                query = Regex.Replace(query, matchPattern, m =>
                                                               {
                                                                   return m.Groups[1] + _variableValueMapping[varName] +
                                                                   m.Groups[3];
                                                               });
            }
            return query;
        }

        public string GetVariableForObject(GraphNode subjectType, string subject, GraphNode verbType, string verb)
        {
            return AllTriples.Where(x =>
                                    x.SubjectType == subjectType &&
                                    x.Subject.Equals(subject) &&
                                    x.VerbType == verbType &&
                                    x.Verb.Equals(verb) &&
                                    x.ObjectType == GraphNode.Variable).Select(x => x.Object).FirstOrDefault();
        }

        public string GetVariableForSubject(GraphNode verbType, string verb, GraphNode objectType, string obj)
        {
            return AllTriples.Where(x =>
                                    x.VerbType == verbType &&
                                    x.Verb.Equals(verb) &&
                                    x.ObjectType == objectType &&
                                    x.Object.Equals(obj) &&
                                    x.SubjectType == GraphNode.Variable
                ).Select(x => x.Subject).FirstOrDefault();
        }

        public void RenameVariable(string oldName, string newName)
        {
            if (!_variableValueMapping.ContainsKey(oldName))
            {
                _variableValueMapping[oldName] = "?" + newName;
            }
        }

        public void AddOrdering(SparqlOrdering ordering)
        {
            _ordering.Add(ordering);
        }

        public void AddAnonymousMemberMapping(string propertyName, string variableName)
        {
            _anonymousMembersMap.Add(new Tuple<string, string>(propertyName, variableName));
        }

        public void ApplyAggregate(string aggregateFunction, string aggregateVar)
        {
            _selectVars.Remove(aggregateVar);
            _aggregates.Add(new Tuple<string, string>(NextVariable(), String.Format("{0}(?{1})", aggregateFunction, aggregateVar)));
        }

        public void StartExists()
        {
            _graphPatternBuilder.Append("FILTER EXISTS { ");
        }

        public void EndExists()
        {
            _graphPatternBuilder.Append("} ");
        }

        public void StartNotExists()
        {
            _graphPatternBuilder.Append("FILTER NOT EXISTS { ");
        }

        public void EndNotExists()
        {
            _graphPatternBuilder.Append("} ");
        }

        public string AssertPrefix(string extensionNamespace)
        {
            string prefix;
            if (_prefixes.TryGetValue(extensionNamespace, out prefix)) return prefix;
            prefix = "ns" + _prefixes.Count;
            _prefixes[extensionNamespace] = prefix;
            return prefix;
        }

    }
}
