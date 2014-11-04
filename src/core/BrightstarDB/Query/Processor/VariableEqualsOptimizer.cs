using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing.Contexts;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Comparison;
using VDS.RDF.Query.Expressions.Conditional;
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
                ISparqlAlgebra optimised;
                var bgp = algebra as Bgp;
                if (bgp != null)
                {
                    return OptimiseBgp(bgp, out optimised) ? optimised : bgp;
                }
                var filter = algebra as IFilter;
                if (filter != null)
                {
                    return OptimiseFilter(filter, out optimised) ? optimised : filter;
                }
                var abstractJoin = algebra as IAbstractJoin;
                if (abstractJoin != null)
                {
                    return abstractJoin.Transform(this);
                }
                var unaryOperator = algebra as IUnaryOperator;
                if (unaryOperator != null)
                {
                    return unaryOperator.Transform(this);
                }
                
                return algebra;
            }
            catch
            {
                return algebra;
            }
        }

        /// <summary>
        /// Runs the optimisation against a Filter algebra
        /// </summary>
        /// <param name="filter">The Filter algebra to optimise</param>
        /// <param name="optimisedAlgebra">Receives the optimised algebra if optimisation was performed or the input algebra otherwise</param>
        /// <returns>True if an optimisation was performed, false otherwise</returns>
        /// <remarks>This implementation currently handles the simple case of a Filter applied to a BGP where the filter
        /// expression is either a single EqualsExpression or SameTermExpression or an AndExpression containing one or more
        /// EqualsExpression or SameTermExpression arguments. The implementation ensures that the replaced variable is still
        /// available to the outer algebra by inserting a BindPattern into the BGP. If the filter expression is a single 
        /// EqualsExpression or SameTermExpression, the optimiser will also strip this out of the algebra, but with an
        /// AndExpression it will leave the full filter expression untouched.
        /// TODO: It should be possible to remove EqualsExpression and SameTermExpression instances from the AndExpression arguments and then either strip it out (if it has no remaining arguments), or optimise it to a single expression (if it has one remaining argument)
        /// </remarks>
        private bool OptimiseFilter(IFilter filter, out ISparqlAlgebra optimisedAlgebra)
        {
            if (!(filter.InnerAlgebra is Bgp))
            {
                // Need a BGP to be able to insert BindPatterns for replaced variables
                optimisedAlgebra = filter;
                return false;
            }

            var filterExpression = filter.SparqlFilter.Expression;
            var replacementTerms = new Dictionary<string, INode>();
            string var;
            INode term;
            bool equals;
            
            // Currently only handle the simple filter cases of a single identity expression
            // or an AND of expressions
            if (IsIdentityExpression(filterExpression, out var, out term, out equals))
            {
                replacementTerms.Add(var, term);
            }
            else if (filterExpression is AndExpression)
            {
                foreach (var arg in filterExpression.Arguments)
                {
                    if (IsIdentityExpression(arg, out var, out term, out equals))
                    {
                        replacementTerms.Add(var, term);
                    }
                    else
                    {
                        foreach (var variable in arg.Variables) {
                            // Cannot guarantee that the argument doesn't imply some other possible binding for the variables
                            replacementTerms.Remove(variable);
                        }
                    }
                }
            }

            if (replacementTerms.Any())
            {
                var optimisedInner = filter.InnerAlgebra as Bgp;
                foreach (var replacementEntry in replacementTerms)
                {
                    try
                    {
                        // Replace the variable with a constant term wherever it appears and then add a Bind pattern
                        // to ensure that the variable is bound for use in the outer algebra
                        var t = new VariableSubstitutionTransformer(replacementEntry.Key, replacementEntry.Value);
                        optimisedInner = t.Optimise(optimisedInner) as Bgp;
                        optimisedInner =
                            new Bgp(
                                optimisedInner.TriplePatterns.Concat(new[]
                                {new BindPattern(replacementEntry.Key, new ConstantTerm(replacementEntry.Value))}));
                    }
                    catch (RdfQueryException)
                    {
                        // Could not perform this replacement.
                    }
                }
                if (filterExpression is AndExpression)
                {
                    // Keep the filter as it may contain other necessary expressions
                    // TODO: Could try to remove the identity expressions here ?
                    optimisedAlgebra = new Filter(optimisedInner, filter.SparqlFilter);
                }
                else
                {
                    // Can optimise away the filter entirely
                    optimisedAlgebra = optimisedInner;
                }
                return true;
            }
            optimisedAlgebra = filter;
            return false;
        }

        private bool OptimiseBgp(Bgp bgp, out ISparqlAlgebra optimisedAlgebra)
        {
            if (bgp.TriplePatterns.OfType<FilterPattern>().Count() == 1)
            {
                var filter = bgp.TriplePatterns.OfType<FilterPattern>().Select(fp => fp.Filter).First();
                string var;
                INode term;
                bool equals;
                if (IsIdentityExpression(filter.Expression, out var, out term, out @equals))
                {
                    if (@equals)
                    {
                        var triplePatterns = new List<ITriplePattern>();
                        foreach (var tp in bgp.TriplePatterns)
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
                            }
                            else
                            {
                                triplePatterns.Add(tp);
                            }
                        }
                        {
                            optimisedAlgebra = new Bgp(triplePatterns);
                            return true;
                        }
                    }
                }
            }
            optimisedAlgebra = bgp;
            return false;
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
                lhs = expr.Arguments.First();
                rhs = expr.Arguments.Last();
            }
            else if (expr is SameTermFunction)
            {
                equals = false;
                lhs = expr.Arguments.First();
                rhs = expr.Arguments.Last();
            }
            else
            {
                return false;
            }

            // Check that the equality / sameTerm operator compares a variable and a constant
            // Extract the variable name and the constant value
            if (lhs is VariableTerm)
            {
                if (rhs is ConstantTerm)
                {
                    var = lhs.Variables.First();
                    term = rhs.Evaluate(null, 0);
                    if (term.NodeType == NodeType.Uri || term.NodeType == NodeType.Literal)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            if (lhs is ConstantTerm)
            {
                if (rhs is VariableTerm)
                {
                    var = rhs.Variables.First();
                    term = lhs.Evaluate(null, 0);
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
