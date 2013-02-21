using System;
using System.Collections.Generic;
using BrightstarDB.Storage;
using VDS.RDF.Query.Patterns;

namespace BrightstarDB.Query.Processor
{
    internal class ThreeVarCollapseGroup : CollapseGroup
    {
        private IStore _store;
        private int _matchLength;
        private readonly IEnumerable<string> _graphUris ;
        public ThreeVarCollapseGroup(IStore store, ITriplePattern tp, IEnumerable<string> globalSortVars, IEnumerable<string> activeGraphUris) : base(tp, globalSortVars)
        {
            _store = store;
            _matchLength = 3;
            SortVariables = new List<string>();
            var triplePattern = tp as TriplePattern;
            // This is the order we get from the store - pred, subj, obj
            SortVariables.Add(triplePattern.Predicate.VariableName);
            SortVariables.Add(triplePattern.Subject.VariableName);
            SortVariables.Add(triplePattern.Object.VariableName);

            _graphUris = activeGraphUris;
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
            var acc = new Accumulator(variables, SortVariables);
            var variableIndexes = new int[_matchLength];
            for (int i = 0; i < _matchLength; i++)
            {
                variableIndexes[i] = acc.Columns.IndexOf(SortVariables[i]);
            }

            foreach(var triple in _store.MatchAllTriples(_graphUris))
            {
                var newRow = new ulong[acc.Columns.Count];
                for (int i = 0; i < _matchLength; i++)
                {
                    newRow[variableIndexes[i]] = triple[i];
                }
                acc.AddRow(newRow);
            }
            return acc;
        }
    }
}