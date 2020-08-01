using System.Linq;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Update;

namespace BrightstarDB.Query.Processor
{
    internal class JoinOptimiser : IAlgebraOptimiser
    {
        public ISparqlAlgebra Optimise(ISparqlAlgebra algebra)
        {
            try
            {
                var j = algebra as IJoin;
                if (j != null)
                {
                    var bgp = j.Lhs as IBgp;
                    if (bgp != null)
                    {
                        var leftBgp = bgp;
                        if (leftBgp.TriplePatterns.All(x => x.IsAcceptAll))
                        {
                            if (bgp.FixedVariables.Any(x => j.Rhs.FixedVariables.Contains(x)))
                            {
                                if (bgp.FloatingVariables.All(floater => j.Rhs.FixedVariables.Contains(floater)))
                                {
                                    return new LeftJoin(j.Rhs, j.Lhs);
                                }
                            }
                        }
                    }
                }
                return algebra;
            }
            catch
            {
                return algebra;
            }
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
