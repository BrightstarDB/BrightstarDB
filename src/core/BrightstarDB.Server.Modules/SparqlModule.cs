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
using VDS.RDF.Parsing;

namespace BrightstarDB.Server.Modules
{
    public class SparqlModule : NancyModule
    {
        private static readonly MediaRange SparqlQueryMediaRange = new MediaRange("application/sparql-query");
        private static readonly MediaRange FormMediaRange = new MediaRange("application/x-www-form-urlencoded");
        private readonly IBrightstarService _brightstar;

        public SparqlModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider permissionsProvider)
        {
            this.RequiresBrightstarStorePermission(permissionsProvider, get:StorePermissions.Read, post:StorePermissions.Read);
            _brightstar = brightstarService;

            Get["/{storeName}/sparql"] = parameters =>
                {
                    try
                    {
                        var requestObject = BindSparqlRequestObject();
                        return ProcessQuery(parameters["storeName"], requestObject);
                    }
                    catch (RdfParseException rdfParseException)
                    {
                        var r = (Response) rdfParseException.Message;
                        r.StatusCode = HttpStatusCode.BadRequest;
                        return r;
                    }
                };
            Post["/{storeName}/sparql"] = parameters =>
                {
                    try
                    {
                        var requestObject = BindSparqlRequestObject();
                        return ProcessQuery(parameters["storeName"], requestObject);
                    }
                    catch (RdfParseException rdfParseException)
                    {
                        var r = (Response)rdfParseException.Message;
                        r.StatusCode = HttpStatusCode.BadRequest;
                        return r;
                    }
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
                && (SparqlQueryMediaRange.Matches(Request.Headers.ContentType)))
            {
                using (var streamReader = new StreamReader(Request.Body))
                {
                    requestObject.Query = streamReader.ReadToEnd();
                }
            }

            if (Request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase)
                && (FormMediaRange.Matches(Request.Headers.ContentType)))
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
                var singleValue = defaultGraphUri.Value as string;
                if (singleValue != null)
                {
                    requestObject.DefaultGraphUri =
                        singleValue.Split(',').Select(s => s.Trim()).ToArray();
                }
                else
                {
                    var valueCollection = defaultGraphUri.Value as IEnumerable<string>;
                    if (valueCollection != null)
                    {
                        requestObject.DefaultGraphUri = valueCollection.ToArray();
                    }
                }
            }
            return requestObject;
        }

        private Negotiator ProcessQuery(string storeName, SparqlRequestObject requestObject)
        {
            return Negotiate
                .WithView("SparqlResult")
                .WithModel(new SparqlQueryProcessingModel(storeName, _brightstar, requestObject));
        }
    }
}
