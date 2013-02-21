using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Query.Algebra;

namespace BrightstarDB.Query.Processor
{
    internal abstract class AccumulatorBase : IAccumulator
    {
        public List<string> Columns { get; private set; }
        public bool[] IsBound { get; private set; }
        public List<int>  SortOrder { get; protected set; }
        public int HighestBoundColumnIndex { get; private set; }
        public abstract IEnumerable<ulong[]> Rows { get; }
        public abstract IAccumulator Sort(IEnumerable<string> otherBoundVariables );
        public List<string> BoundVariables { get; private set; }

        protected bool SortRequired(IEnumerable<string> otherBoundVariables, out List<int> sortColumns)
        {
            sortColumns = new List<int>();
            foreach (var obv in otherBoundVariables)
            {
                var bvIx = Columns.IndexOf(obv);
                if (IsBound[bvIx])
                {
                    sortColumns.Add(bvIx);
                }
            }
            for(int i = 0; i < sortColumns.Count;i++)
            {
                if (SortOrder[i] != sortColumns[i])
                {
                    return true;
                }
            }
            return false;
        }

        protected AccumulatorBase(IEnumerable<string> variables, IEnumerable<string> boundVariables)
        {
            Columns = variables.ToList();
            IsBound = new bool[Columns.Count];
            SortOrder = new List<int>();
            int highestBound = -1;
            BoundVariables = new List<string>(boundVariables);
            foreach (var boundVariable in BoundVariables)
            {
                var ix = Columns.IndexOf(boundVariable);
                IsBound[ix] = true;
                if (ix > highestBound) highestBound = ix;
                SortOrder.Add(ix);
            }
            HighestBoundColumnIndex = highestBound;
            /*
            SortOrder = new List<int>();
            for (int i = 0; i < Columns.Count;i++ )
            {
                if (IsBound[i])
                {
                    SortOrder.Add(i);
                }
            }
             */
        }

        protected AccumulatorBase(IAccumulator src  )
        {
            Columns = new List<string>(src.Columns);
            IsBound = new bool[src.IsBound.Length]; 
            src.IsBound.CopyTo(IsBound, 0);
            HighestBoundColumnIndex = src.HighestBoundColumnIndex;
            BoundVariables = new List<string>(src.BoundVariables);
            SortOrder = new List<int>(src.SortOrder);
        }

        protected  AccumulatorBase(IAccumulator src, IAccumulator joined)
        {
            Columns = new List<string>(src.Columns);
            IsBound = src.IsBound.Select((x, i) => x || joined.IsBound[i]).ToArray();
            HighestBoundColumnIndex = Math.Max(src.HighestBoundColumnIndex, joined.HighestBoundColumnIndex);
            BoundVariables = IsBound.Where(x => x).Select((x, i) => Columns[i]).ToList();
            SortOrder = new List<int>(src.SortOrder);
        }

        public virtual IAccumulator Join(IAccumulator inner)
        {
            if (inner.HighestBoundColumnIndex < HighestBoundColumnIndex)
            {
                // Swap the join order
                return inner.Join(this);
            }

            // Inner needs to be materialized to allow rewinds
            if (inner is VirtualizingAccumulator)
            {
                inner = (inner as VirtualizingAccumulator).Materialize();
            }
            
            bool haveOverlap = false;
            for(int i = 0; i < IsBound.Length && !haveOverlap; i++)
            {
                haveOverlap = IsBound[i] && inner.IsBound[i];
            }

            if (!haveOverlap)
            {
                return Product(inner);
            }
            var sortedOuter = Sort(Columns.Where((x, i) => IsBound[i] && inner.IsBound[i]));
            var sortedInner = inner.Sort(Columns.Where((x, i) => IsBound[i] && inner.IsBound[i]));
            return JoinSorted(sortedOuter, sortedInner);

        }

        private static IAccumulator JoinSorted(IAccumulator outer, IAccumulator inner)
        {
            var output = new Accumulator(outer, inner);
            IEnumerator<ulong[]> outerEnumerator = outer.Rows.GetEnumerator();
            if (inner is VirtualizingAccumulator) inner = (inner as VirtualizingAccumulator).Materialize();
            var innerAcc = inner as Accumulator;
            RewindableListEnumerator<ulong[]> innerEnumerator = innerAcc.RewindableEnumerator;
            //IEnumerator<ulong[]> innerEnumerator = inner.Rows.GetEnumerator();

            bool keepGoing = outerEnumerator.MoveNext() && innerEnumerator.MoveNext();
            while (keepGoing)
            {
                while (keepGoing && RowMatch(outerEnumerator.Current, innerEnumerator.Current))
                {
                    output.AddRow(RowJoin(outerEnumerator.Current, innerEnumerator.Current));
                    innerEnumerator.SetMark();
                    bool innerKeepGoing = innerEnumerator.MoveNext();
                    while (innerKeepGoing && RowMatch(outerEnumerator.Current, innerEnumerator.Current))
                    {
                        output.AddRow(RowJoin(outerEnumerator.Current, innerEnumerator.Current));
                        innerKeepGoing = innerEnumerator.MoveNext();
                    }
                    innerEnumerator.RewindToMark();
                    keepGoing = outerEnumerator.MoveNext();
                }
                if (keepGoing)
                {
                    var cmp = RowCompare(outerEnumerator.Current, innerEnumerator.Current);
                    while (cmp != 0 && keepGoing)
                    {
                        keepGoing = cmp > 0 ? innerEnumerator.MoveNext() : outerEnumerator.MoveNext();
                        if (keepGoing) cmp = RowCompare(outerEnumerator.Current, innerEnumerator.Current);
                    }
                }
                if (!keepGoing)
                {
                    keepGoing = outerEnumerator.MoveNext();
                    if (keepGoing)
                    {
                        innerEnumerator.Reset();
                        keepGoing &= innerEnumerator.MoveNext();
                    }
                }
                /*
                while (keepGoing && RowCompare(outerEnumerator.Current, innerEnumerator.Current) < 0)
                    keepGoing = outerEnumerator.MoveNext();
                while (keepGoing && RowCompare(outerEnumerator.Current, innerEnumerator.Current) > 0)
                    keepGoing = innerEnumerator.MoveNext();
                     */
            }
            return output;
        }

        private IAccumulator Product(IAccumulator other)
        {
            var product = new Accumulator(Columns, BoundVariables.Union(other.BoundVariables));
            /*
            IEnumerable<ulong[]> outerEnumerator, innerEnumerator;
            if (this.HighestBoundColumnIndex < other.HighestBoundColumnIndex)
            {
                outerEnumerator = this.Rows;
                innerEnumerator = other.Rows;
            }
            else
            {
                // This accumulator needs to be inner, so if it is a virtual one we need to materialize it first
                if (this is VirtualizingAccumulator)
                {
                    return (this as VirtualizingAccumulator).Materialize().Product(other);
                }
                outerEnumerator = other.Rows;
                innerEnumerator = this.Rows;
            }
            foreach(var outer in outerEnumerator)
            {
                foreach(var inner in innerEnumerator)
                {
                    product.AddRow(RowJoin(outer, inner));
                }
            }
             */

            IEnumerable<ulong[]> outerEnumerator = this.Rows;
            IEnumerable<ulong[]> innerEnumerator = other.Rows;
            foreach(var outer in outerEnumerator)
            {
                foreach(var inner in innerEnumerator)
                {
                    product.AddRow(RowJoin(outer, inner));
                }
            }

            // Update the sort order for the product
            product.SortOrder = this.SortOrder;
            foreach(var sortCol in other.SortOrder)
            {
                if(!product.SortOrder.Contains(sortCol)) product.SortOrder.Add(sortCol);
            }

            return product;
        }

        public static int RowCompare(ulong[] x, ulong[]y)
        {
            int cmp = 0;
            for(int i = 0; i < x.Length && cmp == 0; i++)
            {
                if (x[i] == 0 || y[i] == 0) continue;
                cmp = x[i].CompareTo(y[i]);
            }
            return cmp;
        }

        public virtual BaseMultiset GetMultiset(IStore store)
        {
            var ms = new Multiset(Columns);
            foreach (var row in Rows)
            {
                var set = new Set();
                for (int i = 0; i < Columns.Count; i++)
                {
                    if (row[i] > 0)
                    {
                        set.Add(Columns[i], MakeNode(store, row[i]));
                    }
                }
                ms.Add(set);
            }
            return ms;
        }

        
        protected static bool RowMatch(ulong[] row, ulong[] otherRow)
        {
            return !row.Where((t, i) => !(t == 0 || otherRow[i] == 0 || t == otherRow[i])).Any();
        }

        protected static ulong[] RowJoin(ulong[] row, ulong[]otherRow)
        {
            return row.Select((t, i) => t == 0 ? otherRow[i] : t).ToArray();
        }

        private INode MakeNode(IStore store, ulong resourceId)
        {
            var resource = store.Resolve(resourceId);
            if (resource.IsLiteral)
            {
                var dt = store.Resolve(resource.DataTypeResourceId);
                var datatype = dt == null ? null : store.ResolvePrefixedUri(dt.LexicalValue);
                return BrightstarLiteralNode.Create(resource.LexicalValue, datatype, resource.LanguageCode);
            }
            return new BrightstarUriNode(new Uri(store.ResolvePrefixedUri(resource.LexicalValue)));
        }
    }
}