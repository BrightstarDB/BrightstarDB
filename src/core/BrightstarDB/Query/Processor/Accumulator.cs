using System.Collections.Generic;

namespace BrightstarDB.Query.Processor
{
    internal class Accumulator : AccumulatorBase
    {
        private readonly List<ulong[]> _rows;

        public override IEnumerable<ulong[]> Rows { get { return _rows; } }


        public Accumulator(IAccumulator src, IAccumulator joined) : base(src, joined)
        {
            _rows = new List<ulong[]>();
        }

        public Accumulator(IAccumulator src) : base(src)
        {
            _rows = new List<ulong[]>();
        }

        public Accumulator(IEnumerable<string> variables, IEnumerable<string> boundVariables  ):base(variables, boundVariables)
        {
            _rows = new List<ulong[]>();
        }

        public RewindableListEnumerator<ulong[]> RewindableEnumerator {get {return new RewindableListEnumerator<ulong[]>(_rows);}}

        public void AddRow(ulong[] row)
        {
            _rows.Add(row);
        }

        public override IAccumulator Sort(IEnumerable<string> otherBoundVariables )
        {
            List<int> sortColumns;
            if (SortRequired(otherBoundVariables, out sortColumns))
            {
                var comparer = new RowComparer(sortColumns);
                _rows.Sort(comparer);
            }
            SortOrder = new List<int>(sortColumns);
            return this;
        }

        public class RowComparer : IComparer<ulong[]>
        {
            private readonly List<int> _sortIndexes;
            public RowComparer(List<int> sortIndexes )
            {
                _sortIndexes = sortIndexes;
            }

            public int Compare(ulong[] x, ulong[] y)
            {
                int cmp = 0;
                for(int i = 0; i < _sortIndexes.Count && cmp == 0; i++)
                {
                    cmp = x[_sortIndexes[i]].CompareTo(y[_sortIndexes[i]]);
                }
                return cmp;
            }
        }
    }
}