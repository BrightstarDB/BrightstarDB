using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Query.Processor;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Query.Patterns;

namespace BrightstarDB.Query
{
    internal class BrightstarQueryProcessor : LeviathanQueryProcessor
    {
        private readonly IStore _store;


        static BrightstarQueryProcessor()
        {
            SparqlOptimiser.AddOptimiser(new VariableEqualsOptimizer());
        }

        public BrightstarQueryProcessor(IInMemoryQueryableStore store) : base(store)
        {
        }

        public BrightstarQueryProcessor(IStore store, ISparqlDataset data) : base(data)
        {
            _store = store;
        }

        // public override 


    }

}