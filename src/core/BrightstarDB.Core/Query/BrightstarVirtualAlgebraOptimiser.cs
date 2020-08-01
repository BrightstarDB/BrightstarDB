using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Storage.Virtualisation;

namespace BrightstarDB.Query
{
    internal class BrightstarVirtualAlgebraOptimiser : VirtualAlgebraOptimiser<ulong, int>
    {
        public BrightstarVirtualAlgebraOptimiser(IVirtualRdfProvider<ulong, int> provider) : base(provider)
        {
        }

        protected override INode CreateVirtualNode(ulong id, INode value)
        {
            return new BrightstarVirtualNode(id, _provider, value);
        }
    }
}
