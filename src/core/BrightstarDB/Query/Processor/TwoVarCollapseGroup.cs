using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Query.Patterns;

namespace BrightstarDB.Query.Processor
{
    internal class TwoVarCollapseGroup : CollapseGroup
    {
        private readonly List<IEnumerator<ulong[]>>  _matchEnumerations;
        private readonly List<TriplePattern> _triplePatterns;
        private readonly IStore _store;
        private readonly int _matchLength;
        private readonly IEnumerable<string> _graphUris;
        private string _fixedSubjectBinding;
        private string _fixedObjectBinding;
        private bool _haveFixedBinding;

        public TwoVarCollapseGroup(IStore store, TriplePattern tp, IEnumerable<string> sortVars, IEnumerable<string> graphUris ) : base(tp, sortVars)
        {
            _store = store;
            _triplePatterns = new List<TriplePattern> {tp};
            _matchLength = 2;
            _matchEnumerations = new List<IEnumerator<ulong[]>>();
            _graphUris = graphUris;
        }

        public override void AddTriplePattern(TriplePattern tp)
        {
            _triplePatterns.Add(tp);
        }

        public override void Evaluate()
        {
            foreach (var tp in _triplePatterns)
            {
                _matchEnumerations.Add(GetMatchEnumeration(tp).GetEnumerator());
            }
        }

        public override IAccumulator BuildAccumulator(IEnumerable<string> variables)
        {
            var acc = new Accumulator(variables, SortVariables);
            var variableIndexes = new int[_matchLength];
            for(int i = 0 ; i < _matchLength; i++)
            {
                variableIndexes[i] = acc.Columns.IndexOf(SortVariables[i]);
            }

            if (_matchEnumerations.Count == 1)
            {
                return new VirtualizingAccumulator(acc.Columns, _matchEnumerations[0], variableIndexes, SortVariables);
            }

            bool keepGoing = true;
            ulong[] toMatch = new ulong[_matchLength];
            foreach (var enumerator in _matchEnumerations)
            {
                keepGoing &= enumerator.MoveNext();
                if (CompareArrays(enumerator.Current, toMatch) > 0) toMatch = enumerator.Current;
            }
            while (keepGoing)
            {
                for(int i = 0; i < _matchEnumerations.Count; i++)
                {
                    while(keepGoing && CompareArrays(toMatch, _matchEnumerations[i].Current) > 0)
                    {
                        keepGoing &= _matchEnumerations[i].MoveNext();
                    }
                    if (keepGoing && CompareArrays(_matchEnumerations[i].Current, toMatch) > 0)
                    {
                        toMatch = _matchEnumerations[i].Current;
                    }
                }
                if (keepGoing && _matchEnumerations.All(x=>CompareArrays(toMatch, x.Current) == 0))
                {
                    // Bingo!
                    var newRow = new ulong[acc.Columns.Count];
                    for(int i = 0; i < _matchLength; i++)
                    {
                        newRow[variableIndexes[i]] = toMatch[i];
                    }
                    acc.AddRow(newRow);
                    keepGoing &= _matchEnumerations[0].MoveNext();
                    toMatch = _matchEnumerations[0].Current;
                }
            }
            return acc;
        }

        private int CompareArrays(ulong[] x, ulong[]y)
        {
            var cmp = 0;
            for(int i = 0; i < x.Length && cmp == 0; i++)
            {
                cmp = x[i].CompareTo(y[i]);
            }
            return cmp;
        }

        private IEnumerable<ulong[]> GetMatchEnumeration(TriplePattern tp)
        {
            if (tp.Subject.VariableName == null)
            {
                if (tp.Predicate.VariableName.Equals(SortVariables[0]))
                {
                    return GetPredicateObjectMatchEnumeration(tp);
                } else
                {
                    return GetObjectPredicateMatchEnumeration(tp);
                }
            }
            if (tp.Predicate.VariableName == null)
            {
                if (tp.Subject.VariableName.Equals(SortVariables[0]))
                {
                    return GetSubjectObjectMatchEnumeration(tp);
                }
                else
                {
                    return GetObjectSubjectMatchEnumeration(tp);
                }
            }
            if (tp.Object.VariableName == null)
            {
                if (tp.Subject.VariableName.Equals(SortVariables[0]))
                {
                    return GetSubjectPredicateMatchEnumeration(tp);
                }
                else
                {
                    return GetPredicateSubjectMatchEnumeration(tp);
                }
            }
            return GetAllTriplesEnumeration(tp);
            //throw new BrightstarInternalException("Invalid two-variable triple pattern");
        }

        private IEnumerable<ulong[]> GetPredicateSubjectMatchEnumeration(TriplePattern tp)
        {
            var objMatch = tp.Object as NodeMatchPattern;
            if (objMatch.Node is ILiteralNode)
            {
                var lit = objMatch.Node as ILiteralNode;
                return _store.GetPredicateSubjectMatchEnumeration(
                    lit.Value, true, lit.DataType == null ? Constants.DefaultDatatypeUri : lit.DataType.ToString(),
                    lit.Language, _graphUris);
            }
            else
            {
                var uri = objMatch.Node as IUriNode;
                return _store.GetPredicateSubjectMatchEnumeration(
                    uri.Uri.ToString(), false, null, null,
                    _graphUris);
            }
        }

        private IEnumerable<ulong[]> GetSubjectPredicateMatchEnumeration(TriplePattern tp)
        {
            return GetPredicateSubjectMatchEnumeration(tp).OrderBy(x => x[1]).ThenBy(x => x[0]).Select(x => new ulong[] { x[1], x[0] });
        }

        private IEnumerable<ulong[]> GetObjectPredicateMatchEnumeration(TriplePattern tp)
        {
            /*
            var intermediate = GetPredicateObjectMatchEnumeration(tp).ToList();
            var sorted =
                intermediate.OrderBy(x => x[1]).ThenBy(x => x[0]).Select(x => new ulong[] {x[1], x[0]}).ToList();
            return sorted;
             */
            return GetPredicateObjectMatchEnumeration(tp).OrderBy(x => x[1]).ThenBy(x => x[0]).Select(x=>new ulong[]{x[1], x[0]});
        }

        private IEnumerable<ulong[]> GetPredicateObjectMatchEnumeration(TriplePattern tp)
        {
            var subjMatch = tp.Subject as NodeMatchPattern;
            var uri = subjMatch.Node as IUriNode;
            return _store.GetPredicateObjectMatchEnumeration(uri.Uri.ToString(), _graphUris);
        }

        private IEnumerable<ulong[]> GetSubjectObjectMatchEnumeration(TriplePattern tp)
        {
            var predMatch = tp.Predicate as NodeMatchPattern;
            var pred = (predMatch.Node as UriNode).Uri;
            if (_haveFixedBinding)
            {
                if (!string.IsNullOrEmpty(_fixedSubjectBinding))
                {
                    return _store.EnumerateObjectsForPredicate(pred.ToString(), _graphUris, false);
                }
                if (!string.IsNullOrEmpty(_fixedObjectBinding))
                {
                    return _store.EnumerateSubjectsForPredicate(pred.ToString(), _graphUris, true);
                }
            }
            return _store.GetSubjectObjectMatchEnumeration(pred.ToString(), _graphUris);
        } 

        private IEnumerable<ulong[]> GetObjectSubjectMatchEnumeration(TriplePattern tp)
        {
            var predMatch = tp.Predicate as NodeMatchPattern;
            var pred = (predMatch.Node as UriNode).Uri;
            if (_haveFixedBinding)
            {
                if (!string.IsNullOrEmpty(_fixedSubjectBinding))
                {
                    return _store.EnumerateObjectsForPredicate(pred.ToString(), _graphUris, true);
                }
                if (!string.IsNullOrEmpty(_fixedObjectBinding))
                {
                    return _store.EnumerateSubjectsForPredicate(pred.ToString(), _graphUris, false);
                }
            }
            return _store.GetObjectSubjectMatchEnumeration(pred.ToString(), _graphUris);
        } 

        private IEnumerable<ulong[]> GetAllTriplesEnumeration(TriplePattern tp)
        {
            int[] variableIndexes = new int[3];
            variableIndexes[0] = SortVariables.IndexOf(tp.Predicate.VariableName);
            variableIndexes[1] = SortVariables.IndexOf(tp.Subject.VariableName);
            variableIndexes[2] = SortVariables.IndexOf(tp.Object.VariableName);

            foreach(var entry in _store.MatchAllTriples(_graphUris))
            {
                bool addRow = true;
                ulong[] newRow = new ulong[2];
                for(int i = 0; i < 3; i++)
                {
                    ulong currentValue = newRow[variableIndexes[i]];
                    if (currentValue > 0 && entry[i] != currentValue)
                    {
                        addRow = false;
                        break;
                    }
                    newRow[variableIndexes[i]] = entry[i];
                }
                if (addRow) yield return newRow;
            }
        }

        public void FixSubjectBinding(string binding)
        {
            _fixedSubjectBinding = binding;
            _haveFixedBinding = true;
        }

        public void FixObjectBinding(string binding)
        {
            _fixedObjectBinding = binding;
            _haveFixedBinding = true;
        }
    }
}