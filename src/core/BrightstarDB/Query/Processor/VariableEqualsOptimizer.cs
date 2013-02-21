using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Comparison;
using VDS.RDF.Query.Expressions.Functions.Sparql.Boolean;
using VDS.RDF.Query.Expressions.Primary;
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Update;

namespace BrightstarDB.Query.Processor
{
    class VariableEqualsOptimizer : IAlgebraOptimiser
    {
        public ISparqlAlgebra Optimise(ISparqlAlgebra algebra)
        {
            try
            {
                if (algebra is Bgp)
                {
                    var bgp = (Bgp) algebra;

                    if (bgp.TriplePatterns.OfType<FilterPattern>().Count() == 1)
                    {
                        var filter = bgp.TriplePatterns.OfType<FilterPattern>().Select(fp=>fp.Filter).First();
                        string var;
                        INode term;
                        bool equals;
                        if (IsIdentityExpression(filter.Expression, out var, out term, out equals))
                        {
                            if (equals)
                            {
                                var triplePatterns = new List<ITriplePattern>();
                                foreach(var tp in bgp.TriplePatterns)
                                {
                                    if (tp is FilterPattern) continue;
                                    if (tp is TriplePattern)
                                    {
                                        var triplePattern = (TriplePattern) tp;
                                        if (triplePattern.Variables.Contains(var))
                                        {
                                            PatternItem subjPattern = triplePattern.Subject,
                                                        predPattern = triplePattern.Predicate,
                                                        objPattern = triplePattern.Object;
                                            if (var.Equals(triplePattern.Subject.VariableName))
                                            {
                                                subjPattern = new NodeMatchPattern(term);
                                            }
                                            if (var.Equals(triplePattern.Predicate.VariableName))
                                            {
                                                predPattern = new NodeMatchPattern(term);
                                            }
                                            if (var.Equals(triplePattern.Object.VariableName))
                                            {
                                                objPattern = new NodeMatchPattern(term);
                                            }
                                            triplePatterns.Add(new TriplePattern(subjPattern, predPattern, objPattern));
                                        }
                                        else
                                        {
                                            triplePatterns.Add(triplePattern);
                                        }
                                    } else
                                    {
                                        triplePatterns.Add(tp);
                                    }
                                }
                                return new Bgp(triplePatterns);
                            }
                        }
                    }
                }
                else if (algebra is IAbstractJoin)
                {
                    return ((IAbstractJoin) algebra).Transform(this);
                }
                else if (algebra is IUnaryOperator)
                {
                    return ((IUnaryOperator) algebra).Transform(this);
                }
                else
                {
                    return algebra;
                }
            }
            catch
            {
                return algebra;
            }
            return algebra;
        }

        /// <summary>
        /// Determines whether an expression is an Identity Expression
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="var">Variable</param>
        /// <param name="term">Term</param>
        /// <param name="equals">Whether it is an equals expression (true) or a same term expression (false)</param>
        /// <returns></returns>
        private bool IsIdentityExpression(ISparqlExpression expr, out String var, out INode term, out bool equals)
        {
            var = null;
            term = null;
            equals = false;
            ISparqlExpression lhs, rhs;
            if (expr is EqualsExpression)
            {
                equals = true;
                var eq = (EqualsExpression)expr;
                lhs = eq.Arguments.First();
                rhs = eq.Arguments.Last();
            }
            else if (expr is SameTermFunction)
            {
                var st = (SameTermFunction)expr;
                lhs = st.Arguments.First();
                rhs = st.Arguments.Last();
            }
            else
            {
                return false;
            }

            if (lhs is VariableTerm)
            {
                if (rhs is IValuedNode)
                {
                    var = lhs.Variables.First();
                    term = ((IValuedNode)rhs);
                    if (term.NodeType == NodeType.Uri || term.NodeType == NodeType.Literal)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            if (lhs is IValuedNode)
            {
                if (rhs is VariableTerm)
                {
                    var = rhs.Variables.First();
                    term = ((IValuedNode)lhs);
                    if (term.NodeType == NodeType.Uri || term.NodeType == NodeType.Literal)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            return false;
        }

        public bool IsApplicable(SparqlQuery q)
        {
            return true;
        }

        public bool IsApplicable(SparqlUpdateCommandSet cmds)
        {
            return true;
        }
    }
}
