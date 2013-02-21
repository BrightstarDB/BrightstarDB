using System.Collections.Generic;
using BrightstarDB.Storage;
using VDS.RDF.Query.Algebra;

namespace BrightstarDB.Query.Processor
{
    internal class VirtualizingAccumulator : AccumulatorBase
    {
        private readonly IEnumerator<ulong[]> _enumerator;
        private readonly int[] _enumeratorVariableIndexes;
        private readonly int _rowLength;

        public VirtualizingAccumulator(IEnumerable<string> variables, IEnumerator<ulong[]> enumerator, int[] enumeratorVariableIndexes, IEnumerable<string> enumeratorVariables   ) : base(variables, enumeratorVariables)
        {
            _enumerator = enumerator;
            _enumeratorVariableIndexes = enumeratorVariableIndexes;
            _rowLength = Columns.Count;
        }

        public override IEnumerable<ulong[]> Rows
        {
            get
            {
                while(_enumerator.MoveNext())
                {
                    var row = new ulong[_rowLength];
                    for(int i = 0; i < _enumeratorVariableIndexes.Length; i++)
                    {
                        row[_enumeratorVariableIndexes[i]] = _enumerator.Current[i];
                    }
                    yield return row;
                }
            }
        }

        /*
        public override BaseMultiset GetMultiset(Store store)
        {
            return Materialize().GetMultiset(store);
        }
        */

        public Accumulator Materialize()
        {
            var ret = new Accumulator(this);
            foreach(var row in Rows)
            {
                ret.AddRow(row);
            }
            return ret;
        }

        public override IAccumulator Sort(IEnumerable<string> otherBoundVariables )
        {
            List<int> sortColumns;
            if (SortRequired(otherBoundVariables, out sortColumns))
            {
                return Materialize().Sort(otherBoundVariables);
            }
            return this;
        }

        /*
        public override IAccumulator Join(IAccumulator inner)
        {
#if DEBUG
            return this.Materialize().Join(inner);
#endif
            return base.Join(inner);
        }
         */
    }
}