using System;
using System.Collections.Generic;
using VDS.RDF.Query.Patterns;

namespace BrightstarDB.Query.Processor
{
    internal class PassthroughCollapseGroup : CollapseGroup
    {
        private readonly ITriplePattern _triplePattern;

        public ITriplePattern TriplePattern { get { return _triplePattern; } }

        public PassthroughCollapseGroup(ITriplePattern tp, IEnumerable<string> globalSortVars):base(tp, globalSortVars)
        {
            _triplePattern = tp;
        }

        public override void AddTriplePattern(TriplePattern tp)
        {
            throw new NotImplementedException();
        }

        public override void Evaluate()
        {
            return;
        }

        public override IAccumulator BuildAccumulator(IEnumerable<string> variables)
        {
            throw new NotImplementedException();
        }

    }
}