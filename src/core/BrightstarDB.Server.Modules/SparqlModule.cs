using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public class SparqlModule : NancyModule
    {
        private readonly IBrightstarService _brightstar;

        public SparqlModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider permissionsProvider)
        {
            this.RequiresBrightstarStorePermission(permissionsProvider, get:StorePermissions.Read, post:StorePermissions.Read);
            _brightstar = brightstarService;

            Get["/{storeName}/sparql"] = parameters =>
                {
                    var requestObject = BindSparqlRequestObject();
                    return ProcessQuery(parameters["storeName"], requestObject);
                };
            Post["/{storeName}/sparql"] = parameters =>
                {
                    var requestObject = BindSparqlRequestObject();
                    return ProcessQuery(parameters["storeName"], requestObject);
                };
            Get["/{storeName}/commits/{commitId}/sparql"] = ProcessCommitPointQuery;
            Post["/{storeName}/commits/{commitId}/sparql"] = ProcessCommitPointQuery;
        }

        private object ProcessCommitPointQuery(dynamic parameters)
        {
            var requestObject = BindSparqlRequestObject();
            ulong c;
            if (UInt64.TryParse(parameters["commitId"], out c))
            {
                return
                    Negotiate.WithModel(new SparqlQueryProcessingModel(parameters["storeName"], c, _brightstar,
                                                                       requestObject));
            }
            return HttpStatusCode.BadRequest;
        }

        private SparqlRequestObject BindSparqlRequestObject()
        {
            var requestObject = this.Bind<SparqlRequestObject>();
            dynamic defaultGraphUri;
            if (Request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase)
                && (MediaRange.FromString("application/sparql-query").Matches(Request.Headers.ContentType)))
            {
                using (var streamReader = new StreamReader(Request.Body))
                {
                    requestObject.Query = streamReader.ReadToEnd();
                }
            }

            if (Request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase)
                && (MediaRange.FromString("application/x-www-form-urlencoded").Matches(Request.Headers.ContentType)))
            {
                // Bind graph parameters from form
                defaultGraphUri = Request.Form["default-graph-uri"];
            }
            else
            {
                // Bind graph parameters from 
                defaultGraphUri = Request.Query["default-graph-uri"];
            }
            if (defaultGraphUri.HasValue)
            {
                if (defaultGraphUri.Value is string)
                {
                    requestObject.DefaultGraphUri =
                        (defaultGraphUri.Value as string).Split(',').Select(s => s.Trim()).ToArray();
                }
                else if (defaultGraphUri.Value is IEnumerable<string>)
                {
                    requestObject.DefaultGraphUri = (defaultGraphUri.Value as IEnumerable<string>).ToArray();
                }
            }
            return requestObject;
        }

        private Negotiator ProcessQuery(string storeName, SparqlRequestObject requestObject)
        {
            //SparqlResultsFormat requestedFormat =
            //    String.IsNullOrEmpty(requestObject.Format)
            //        ? SparqlResultsFormat.Xml
            //        : (
            //              SparqlResultsFormat.GetResultsFormat(requestObject.Format) ??
            //              SparqlResultsFormat.Xml);
            //RdfFormat graphFormat =
            //    String.IsNullOrEmpty(requestObject.Format)
            //        ? RdfFormat.RdfXml
            //        : (RdfFormat.GetResultsFormat(requestObject.Format) ?? RdfFormat.RdfXml);

            try
            {
                var model = new SparqlResultModel(storeName, _brightstar, requestObject,
                                                  //requestedFormat, graphFormat);
                                                  null, null);

                return Negotiate
                    .WithMediaRangeModel(MediaRange.FromString("text/html"), model).WithView("SparqlResult")
                    .WithModel(new SparqlQueryProcessingModel(storeName, _brightstar, requestObject));
            }
            catch (VDS.RDF.Parsing.RdfParseException ex)
            {
                return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithReasonPhrase(ex.Message);
            }
        }
    }
}
