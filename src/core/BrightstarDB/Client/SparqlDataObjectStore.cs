using System.Collections.Generic;
using System.Text;
using VDS.RDF.Query;
using VDS.RDF.Update;

namespace BrightstarDB.Client
{
    internal class SparqlDataObjectStore : RemoteDataObjectStore
    {
        private readonly IUpdateableStore _client;

        public SparqlDataObjectStore(
            ISparqlQueryProcessor queryProcessor, ISparqlUpdateProcessor updateProcessor,
            Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled, 
            string updateGraphUri = null, IEnumerable<string> datasetGraphUris = null, string versionGraphUri = null) 
            : base(namespaceMappings, optimisticLockingEnabled, updateGraphUri, datasetGraphUris, versionGraphUri)
        {
            _client = new SparqlUpdatableStore(queryProcessor, updateProcessor);
        }

        protected override IUpdateableStore Client
        {
            get { return _client; }
        }

        protected override string GetQueryTemplate()
        {
            if (DataSetGraphUris == null && UpdateGraphUri == Constants.DefaultGraphUri &&
                VersionGraphUri == Constants.DefaultGraphUri)
            {
                return "SELECT ?p ?o WHERE {{ <{0}> ?p ?o }}";
            }
            var sb = new StringBuilder();
            sb.Append("SELECT ?p ?o ?g");
            if (DataSetGraphUris != null)
            {
                foreach (var dsGraph in DataSetGraphUris)
                {
                    sb.AppendFormat(" FROM NAMED <{0}>", dsGraph);
                }
            }
            sb.AppendFormat(" FROM NAMED <{0}>", UpdateGraphUri);
            sb.AppendFormat(" FROM NAMED <{0}>", VersionGraphUri);
            sb.Append(" WHERE {{ GRAPH ?g {{ <{0}> ?p ?o }} }}");
            return sb.ToString();
        }
    }
}
