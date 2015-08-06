using System;
using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;
using Nancy.Responses.Negotiation;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using StringWriter = System.IO.StringWriter;

namespace BrightstarDB.Server.Modules
{
    public class GraphsModule : NancyModule
    {
        private const string DefaultGraphContentQuery = "CONSTRUCT { ?s ?p ?o } WHERE { ?s ?p ?o }";
        private const string NamedGraphContentQuery = "CONSTRUCT {{ ?s ?p ?o }} WHERE {{ GRAPH <{0}> {{ ?s ?p ?o }} }}";
        private const string UpdateNamedGraph = "DROP SILENT GRAPH <{0}>; INSERT DATA {{ GRAPH <{0}> {{ {1} }} }}";
        private const string UpdateDefaultGraph = "DROP SILENT DEFAULT; INSERT DATA {{ {0} }}";
        private const string DropDefaultGraph = "DROP DEFAULT";
        private const string DropNamedGraph = "DROP GRAPH <{0}>";
        private const string MergeNamedGraph = "INSERT DATA {{ GRAPH <{0}> {{ {1} }} }}";
        private const string MergeDefaultGraph = "INSERT DATA {{ {0} }}";

        public GraphsModule(IBrightstarService brightstarService,
            AbstractStorePermissionsProvider storePermissionsProvider)
        {
            this.RequiresBrightstarStorePermission(storePermissionsProvider, get:StorePermissions.Read, put:StorePermissions.SparqlUpdate, post:StorePermissions.SparqlUpdate, delete:StorePermissions.SparqlUpdate);


            Get["/{storeName}/graphs"] = parameters =>
            {
                if (!brightstarService.DoesStoreExist(parameters["storeName"])) return HttpStatusCode.NotFound;
                Uri graphUri;
                if (Request.Query["default"] == null && Request.Query["graph"] == null)
                {
                    // Get with no default or graph parameter returns a list of graphs
                    // This is not part of the SPARQL 1.1 Graph Store protocol, but a useful extension
                    // The returned value is a SPARQL results set with a single variable named "graphUri"
                    // Acceptable media types are the supported SPARQL results set formats
                    return
                        Negotiate.WithModel(
                            new GraphListModel(brightstarService.ListNamedGraphs(parameters["storeName"])));
                }
                if (!TryGetGraphUri(out graphUri))
                {
                    // Will get here if the value of the graph parameter is parsed as a relative URI
                    return HttpStatusCode.BadRequest;
                }
                try
                {
                    // Return the serialized content of the graph
                    return SerializeGraph(brightstarService, parameters["storeName"], graphUri.ToString());
                }
                catch (BrightstarStoreNotModifiedException)
                {
                    return HttpStatusCode.NotModified;
                }
            };

            Put["/{storeName}/graphs"] = parameters =>
            {
                var storeParam = parameters["storeName"];
                var store = storeParam.Value as string;
                Uri graphUri;
                if (!brightstarService.DoesStoreExist(store)) return HttpStatusCode.NotFound;
                if (!TryGetGraphUri(out graphUri))
                {
                    return HttpStatusCode.BadRequest;
                }
                var graphUriStr = graphUri.ToString();
                bool isNewGraph = !graphUriStr.Equals(Constants.DefaultGraphUri) &&
                                  !(brightstarService.ListNamedGraphs(store).Any(x => x.Equals(graphUriStr)));

                try
                {
                    var rdfFormat = GetRequestBodyFormat();
                    if (rdfFormat == null) return HttpStatusCode.NotAcceptable;
                    var rdfPayload = ParseBody(rdfFormat);
                    var sparqlUpdate = graphUri.ToString().Equals(Constants.DefaultGraphUri) ?
                        String.Format(UpdateDefaultGraph, rdfPayload) :
                        String.Format(UpdateNamedGraph, graphUri, rdfPayload);

                    var job = brightstarService.ExecuteUpdate(store, sparqlUpdate, true, "Update Graph " + graphUri);
                    return job.JobCompletedOk
                        ? (isNewGraph ? HttpStatusCode.Created : HttpStatusCode.NoContent)
                        : HttpStatusCode.InternalServerError;
                }
                catch (RdfException)
                {
                    return HttpStatusCode.BadRequest;
                }
            };

            Post["/{storeName}/graphs"] = parameters =>
            {
                var storeParam = parameters["storeName"];
                var store = storeParam.Value as string;
                Uri graphUri;
                if (!brightstarService.DoesStoreExist(store)) return HttpStatusCode.NotFound;
                if (!TryGetGraphUri(out graphUri))
                {
                    return HttpStatusCode.BadRequest;
                }
                var graphUriStr = graphUri.ToString();
                var isNewGraph = !graphUriStr.Equals(Constants.DefaultGraphUri) &&
                                 !(brightstarService.ListNamedGraphs(store).Any(x => x.Equals(graphUriStr)));
                try
                {
                    var rdfFormat = GetRequestBodyFormat();
                    if (rdfFormat == null) return HttpStatusCode.NotAcceptable;
                    var rdfPayload = ParseBody(rdfFormat);
                    var sparqlUpdate = graphUri.ToString().Equals(Constants.DefaultGraphUri) ?
                        String.Format(MergeDefaultGraph, rdfPayload) :
                        String.Format(MergeNamedGraph, graphUri, rdfPayload);

                    var job = brightstarService.ExecuteUpdate(store, sparqlUpdate, true, "Update Graph " + graphUri);
                    return job.JobCompletedOk
                        ? (isNewGraph ? HttpStatusCode.Created : HttpStatusCode.NoContent)
                        : HttpStatusCode.InternalServerError;
                }
                catch (RdfException)
                {
                    return HttpStatusCode.BadRequest;
                }
            };

            Delete["{storeName}/graphs"] = parameters =>
            {
                Uri graphUri;
                string sparqlUpdate, jobName;
                var store = parameters["storeName"].Value as string;
                if (!brightstarService.DoesStoreExist(store)) return HttpStatusCode.NotFound;
                if (!TryGetGraphUri(out graphUri)) return HttpStatusCode.BadRequest;
                if (graphUri.ToString().Equals(Constants.DefaultGraphUri))
                {
                    // Clear the default graph
                    sparqlUpdate = DropDefaultGraph;
                    jobName = "Drop Default Graph";
                }
                else
                {
                    var graphId = graphUri.ToString();
                    if (!brightstarService.ListNamedGraphs(store).Contains(graphId))
                    {
                        return HttpStatusCode.NotFound;
                    }
                    // Clear the named graph
                    sparqlUpdate = String.Format(DropNamedGraph, graphId);
                    jobName = "Drop Graph " + graphId;
                }

                var job = brightstarService.ExecuteUpdate(store, sparqlUpdate, true, jobName);
                return job.JobCompletedOk
                    ? HttpStatusCode.NoContent
                    : HttpStatusCode.InternalServerError;
            };

        }

        private bool TryGetGraphUri(out Uri graphUri)
        {
            if (Request.Query["default"] != null)
            {
                graphUri = new Uri(Constants.DefaultGraphUri);
                return true;
            }
            if (Request.Query["graph"] != null)
            {
                var graph = Request.Query["graph"].Value as string;
                if (Uri.TryCreate(graph, UriKind.Absolute, out graphUri)) return true;
            }
            graphUri = null;
            return false;
        }
        private Negotiator SerializeGraph(IBrightstarService brightstarService, string storeName, string graphUri)
        {
            var sparqlQuery = graphUri.Equals(Constants.DefaultGraphUri)
                ? DefaultGraphContentQuery
                : String.Format(NamedGraphContentQuery, graphUri);
            return
                Negotiate.WithModel(
                    new SparqlQueryProcessingModel(storeName, brightstarService,
                        new SparqlRequestObject
                        {
                            Query = sparqlQuery,
                            DefaultGraphUri = new[] {Constants.DefaultGraphUri}
                        }
                        ));
        }

        private string ParseBody( RdfFormat contentType)
        {
            var parser = MimeTypesHelper.GetParser(contentType.MediaTypes.First());

            var writer = new WriteToStringHandler(typeof (VDS.RDF.Writing.Formatting.NTriplesFormatter));
            using (var reader = new StreamReader(Request.Body))
            {
                parser.Load(writer, reader);
            }
            return writer.ToString();
        }

        private RdfFormat GetRequestBodyFormat()
        {
            return Request.Headers.ContentType == null ? RdfFormat.RdfXml : RdfFormat.GetResultsFormat(Request.Headers.ContentType);
        }
    }

    class WriteToStringHandler : BaseRdfHandler
    {
        private readonly Type _formatterType;
        private WriteThroughHandler _handler;
        private readonly StringBuilder _buffer;

        public WriteToStringHandler(Type formatterType)
        {
            _formatterType = formatterType;
            _buffer = new StringBuilder();
        }

        protected override void StartRdfInternal()
        {
            _handler = new WriteThroughHandler(_formatterType, new StringWriter(_buffer));
            _handler.StartRdf();
        }

        protected override void EndRdfInternal(bool ok)
        {
            _handler.EndRdf(ok);
        }

        protected override bool HandleBaseUriInternal(Uri baseUri)
        {
            return _handler.HandleBaseUri(baseUri);
        }

        protected override bool HandleNamespaceInternal(string prefix, Uri namespaceUri)
        {
            return _handler.HandleNamespace(prefix, namespaceUri);
        }

        protected override bool HandleTripleInternal(Triple t)
        {
            return _handler.HandleTriple(t);
        }

        public override bool AcceptsAll
        {
            get
            {
                return true;
            }
        }

        public override string ToString()
        {
            return _buffer.ToString();
        }
    }
}
