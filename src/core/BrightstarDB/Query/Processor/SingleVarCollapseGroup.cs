using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Query.Patterns;

namespace BrightstarDB.Query.Processor
{
    internal class SingleVarCollapseGroup : CollapseGroup
    {
        private List<IEnumerator<ulong>> _matchEnumerations; 
        private readonly List<TriplePattern> _triplePatterns;
        private IStore _store;
        private readonly IEnumerable<string> _graphUris ;

        public SingleVarCollapseGroup(IStore store, TriplePattern tp, IEnumerable<string> globalSortVars, IEnumerable<string> graphUris) : base(tp, globalSortVars)
        {
            _store = store;
            if (tp.GetVariableCount() != 1) throw new ArgumentException("Triple pattern must bind a single variable");
            _triplePatterns = new List<TriplePattern>{tp};
            _matchEnumerations = new List<IEnumerator<ulong>>();
            _graphUris = graphUris;
        }

        public override void AddTriplePattern(TriplePattern tp)
        {
            _triplePatterns.Add(tp);
        }

        public override void Evaluate()
        {
            foreach(var tp in _triplePatterns)
            {
                _matchEnumerations.Add(GetMatchEnumeration(tp).GetEnumerator());
            }
        }

        public override IAccumulator BuildAccumulator(IEnumerable<string> variables)
        {
            var acc = new Accumulator(variables, _triplePatterns[0].Variables.Take(1));
            var varIx = acc.Columns.IndexOf(_triplePatterns[0].Variables[0]);

            bool keepGoing = true;
            ulong[] topItems = new ulong[_matchEnumerations.Count];
            ulong toMatch = 0;
            for (int i = 0; i < _matchEnumerations.Count; i++)
            {
                keepGoing &= _matchEnumerations[i].MoveNext();
                if (!keepGoing) break;
                topItems[i] = _matchEnumerations[i].Current;
                if (topItems[i] > toMatch) toMatch = topItems[i];
            }
            while (keepGoing)
            {
                for(int i = 0; i < _matchEnumerations.Count; i++)
                {
                    while (_matchEnumerations[i].Current < toMatch && keepGoing)
                    {
                        keepGoing &= _matchEnumerations[i].MoveNext();
                    }
                    if (keepGoing && _matchEnumerations[i].Current > toMatch)
                    {
                        toMatch = _matchEnumerations[i].Current;
                    }
                }
                if (keepGoing && _matchEnumerations.All(x=>x.Current.Equals(toMatch)))
                {
                    // Bingo!
                    var newRow = new ulong[acc.Columns.Count];
                    newRow[varIx] = toMatch;
                    acc.AddRow(newRow);
                    keepGoing &= _matchEnumerations[0].MoveNext();
                    toMatch = _matchEnumerations[0].Current;
                }
            }
            return acc;
        }

        private IEnumerable<ulong> GetMatchEnumeration(TriplePattern tp)
        {
            if (tp.Object.VariableName != null)
            {
                var subjectMatch = tp.Subject as NodeMatchPattern;
                var predMatch = tp.Predicate as NodeMatchPattern;
                var subj = (subjectMatch.Node as IUriNode).Uri;
                var pred = (predMatch.Node as IUriNode).Uri;
                return _store.GetMatchEnumeration(subj.ToString(), pred.ToString(), null, false,null, null, _graphUris);
            }
            if (tp.Subject.VariableName != null)
            {
                var predMatch = tp.Predicate as NodeMatchPattern;
                var objMatch = tp.Object as NodeMatchPattern;
                if (objMatch.Node is ILiteralNode)
                {
                    var litNode = (objMatch.Node as ILiteralNode);
                    var pred = (predMatch.Node as IUriNode).Uri;
                    return _store.GetMatchEnumeration(null, pred.ToString(), litNode.Value, true,
                                                      litNode.DataType == null ? Constants.DefaultDatatypeUri : litNode.DataType.ToString(),
                                                      litNode.Language,
                                                      _graphUris);
                }
                else
                {
                    var pred = (predMatch.Node as IUriNode).Uri;
                    var obj = (objMatch.Node as IUriNode).Uri;
                    return _store.GetMatchEnumeration(null, pred.ToString(), obj.ToString(),
                                                      false, null, null,
                                                      _graphUris);
                }
            }
            if (tp.Predicate.VariableName != null)
            {
                var subjMatch = tp.Subject as NodeMatchPattern;
                var objMatch = tp.Object as NodeMatchPattern;
                if (objMatch.Node is ILiteralNode)
                {
                    var litNode = (objMatch.Node as ILiteralNode);
                    var subj = (subjMatch.Node as IUriNode);
                    return _store.GetMatchEnumeration(subj.ToString(), null, litNode.Value, true,
                                                      litNode.DataType == null ? Constants.DefaultDatatypeUri : litNode.DataType.ToString(),
                                                      litNode.Language,
                                                      _graphUris);
                }
                else
                {
                    var subj = (subjMatch.Node as IUriNode).Uri;
                    var obj = (objMatch.Node as IUriNode).Uri;
                    return _store.GetMatchEnumeration(subj.ToString(), null, obj.ToString(),
                                                      false, null, null,
                                                      _graphUris);

                }
            }
            throw new BrightstarInternalException("Error determining match enumeration.");
        }
    }
}