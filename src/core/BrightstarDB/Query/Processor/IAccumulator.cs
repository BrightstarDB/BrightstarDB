using System.Collections.Generic;
using BrightstarDB.Storage;
using VDS.RDF.Query.Algebra;

namespace BrightstarDB.Query.Processor
{
    internal interface IAccumulator
    {
        List<string> Columns { get; }
        List<int> SortOrder { get; } 
        bool[] IsBound { get; }
        int HighestBoundColumnIndex { get; }
        IEnumerable<ulong[]> Rows { get; } 
        BaseMultiset GetMultiset(IStore store);
        IAccumulator Join(IAccumulator inner);
        IAccumulator Sort(IEnumerable<string> otherBoundVariables );
        List<string> BoundVariables { get; }
    }
}