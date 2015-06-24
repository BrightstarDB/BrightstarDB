using BrightstarDB.Query.Processor;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.Optimisation;

namespace BrightstarDB.Query
{
    internal class BrightstarQueryProcessor : LeviathanQueryProcessor
    {
        // KA: Not currently used
        //private readonly IStore _store;


        static BrightstarQueryProcessor()
        {
            SparqlOptimiser.AddOptimiser(new VariableEqualsOptimizer());
            SparqlOptimiser.AddOptimiser(new JoinOptimiser());
        }

        public BrightstarQueryProcessor(IInMemoryQueryableStore store) : base(store)
        {
        }

        public BrightstarQueryProcessor(IStore store, ISparqlDataset data) : base(data)
        {
            //_store = store;
        }

        // public override 


    }

}