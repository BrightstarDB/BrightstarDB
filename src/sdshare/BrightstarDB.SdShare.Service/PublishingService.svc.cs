using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using System.Xml.Linq;

namespace BrightstarDB.SdShare.Service
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(Namespace = "http://www.brightstardb.com/schemas/sdshare", 
                     InstanceContextMode = InstanceContextMode.Single,
                     IncludeExceptionDetailInFaults = true,
                     ConcurrencyMode=ConcurrencyMode.Multiple)]
    public class PublishingService : IPublishingService
    {
        private readonly ServerCore _serverCore;

        public PublishingService()
        {
            _serverCore = new ServerCore();
        }

        public Stream Status()
        {
            var config = ConfigurationReader.Configuration;

            var html = "<html><body><h1>SdShare Server 1.0</h1>";

            html += "<h2>Feed Sources</h2>";
            html += "<p>Lists all the SdShare Feeds that this server is consuming. Changes in the fragments feed for the collection are passed on the the client adaptors registered for each feed</p>";

            html += "<ol>";

            foreach (var fs in config.FeedSources) 
            {
                html += "<li><p>";
                html += "<h3>" + fs.Name + "</h3>";
                html += "<p><a href=\"" + fs.CollectionUri +"\">" + fs.CollectionUri + "</a></p>";
                html += "<p>Check Period (ms): " + fs.CheckPeriod + "</p>";

                html += "<h4>Client Adaptors</h4>";
                html += "<ol>";

                foreach (var clientAdaptor in config.ClientAdaptors.Where(ca => ca.FeedName.Equals(fs.Name)))
                {
                    html += "<li><p>" + clientAdaptor.GetType().Name + "</p></li>";
                }

                html += "</ol>";

                html += "<p>Actions: <a href=\"/status?checkfeed=true\" alt=\"Will fetch all changes from the fragments feed and trigger any clients consuming that source.\">Check Changes</a></p>";
                html += "</p></li>";
            }

            html += "</ol>";
            
            html += "</body></html>";
            
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(html));

            var ctx = WebOperationContext.Current;
            ctx.OutgoingResponse.ContentType = "text/html";

            return ms;
        }

        public string Ping()
        {
            return "SdShare Server is started.";
        }

        public void ResourceSink(string resourceuri, Stream data)
        {
            using (var sr = new StreamReader(data))
            {
                string content = sr.ReadToEnd();
                var resourceData = XDocument.Parse(content);
            }
        }

        public Atom10FeedFormatter GetCollectionsFeed()
        {
            return _serverCore.GetCollectionsFeed();
        }

        private static void RaiseNotFound(string message)
        {
            Logging.LogWarning(1, message);
            var woc = WebOperationContext.Current;
            woc.OutgoingResponse.SetStatusAsNotFound(message);
            throw new WebFaultException<string>(message, HttpStatusCode.NotFound);            
        }

        private static void RaiseServerException(Exception ex, string message)
        {
            Logging.LogError(1, "Error Processing GetCollectionFeed {0} {1}", ex.Message, ex.StackTrace);
            var woc = WebOperationContext.Current;
            woc.OutgoingResponse.SetStatusAsNotFound(message);
            throw new WebFaultException<string>(message, HttpStatusCode.InternalServerError);                        
        }

        public Atom10FeedFormatter GetCollectionFeed(string collectionId)
        {
            try
            {
                var feed = _serverCore.GetCollectionFeed(collectionId);
                if (feed == null) RaiseNotFound("No collection with id " + collectionId);
                return feed;
            } catch(Exception ex)
            {
                if (!(ex is WebFaultException<string>))
                {
                    RaiseServerException(ex,
                                         "Error processing GetCollectionFeed for " + collectionId +
                                         " see server logs for details.");
                    return null;
                }
                throw;
            }
        }

        public Atom10FeedFormatter GetCollectionSnapshotsFeed(string collectionId)
        {
            try
            {
                var feed = _serverCore.GetSnapshotsFeed(collectionId);
                if (feed == null) RaiseNotFound("No collection with id " + collectionId);
                return feed;
           } catch(Exception ex)
            {
                if (!(ex is WebFaultException<string>))
                {
                    RaiseServerException(ex,
                                         "Error processing GetCollectionSnapshotsFeed for " + collectionId +
                                         " see server logs for details.");
                    return null;
                }
                throw;
            }
        }

        public Stream GetCollectionSnapshot(string collectionId, string id)
        {
            try
            {
                var format = GetRequestedFormat();

                if (format.Equals("nt"))
                {
                    var woc = WebOperationContext.Current;
                    woc.OutgoingResponse.ContentType = "text/plain";
                }
                else
                {
                    var woc = WebOperationContext.Current;
                    woc.OutgoingResponse.ContentType = "application/xml";
                    woc.OutgoingResponse.Format = WebMessageFormat.Xml;
                }
                var snapshot = _serverCore.GetSnapshot(collectionId, id, format);
                if (snapshot == null) RaiseNotFound("No collection with id " + collectionId + " or snapshot " + id);
                return snapshot;
            }
            catch (Exception ex)
            {
                if (!(ex is WebFaultException<string>))
                {
                    RaiseServerException(ex,
                                         "Error processing GetCollectionSnapshot for " + collectionId + " " + id + 
                                         " see server logs for details.");
                    return null;
                }
                throw;
            }
        }

        public Atom10FeedFormatter GetCollectionFragmentsFeed(string collectionId, string since, string before, string page)
        {
            try {
                var sinceDt = DateTime.MinValue;
                if (since != null) DateTime.TryParse(since, out sinceDt);

                var beforeDt = DateTime.MinValue;
                if (before != null) DateTime.TryParse(before, out beforeDt);

                var pageInt = 1;
                if (!String.IsNullOrEmpty(page)) pageInt = int.Parse(page);

                var feed = _serverCore.GetFragmentsFeed(collectionId, sinceDt, beforeDt, pageInt);
                if (feed == null) RaiseNotFound("No collection with id " + collectionId);
                return feed;
            }
            catch (Exception ex)
            {
                if (!(ex is WebFaultException<string>))
                {
                    RaiseServerException(ex,
                                         "Error processing GetCollectionFragmentsFeed for " + collectionId +
                                         " see server logs for details.");
                    return null;
                }
                throw;
            }
        }

        public Stream GetFragment(string collectionId, string fragmentId)
        {
            try
            {
                var format = GetRequestedFormat();
                if (format.Equals("nt"))
                {
                    var woc = WebOperationContext.Current;
                    woc.OutgoingResponse.ContentType = "text/plain";
                }
                else
                {
                    var woc = WebOperationContext.Current;
                    woc.OutgoingResponse.ContentType = "application/xml; charset=utf-8";
                    woc.OutgoingResponse.Format = WebMessageFormat.Xml;
                }
                var fragment = _serverCore.GetFragment(collectionId, fragmentId, format);
                if (fragment == null)
                {
                    RaiseNotFound("No collection with id " + collectionId + " or fragement " + fragmentId);
                }            
                return fragment;
            } catch (Exception ex) {
                if (!(ex is WebFaultException<string>))
                {
                    RaiseServerException(ex,
                                         "Error processing GetFragment for " + collectionId + " fragment id " + fragmentId +
                                         " see server logs for details.");
                    return null;
                }
                throw;
            }
        }

        private string GetRequestedFormat()
        {
            if (WebOperationContext.Current == null)
            {
                return "nt";
            }

            var woc = WebOperationContext.Current;
            var request = woc.IncomingRequest;
            var requestFormatString = request.UriTemplateMatch.QueryParameters["format"];
            if (!String.IsNullOrEmpty(requestFormatString))
            {
                switch (requestFormatString.ToLower())
                {
                    case "xml":
                        return "xml";
                    case "nt":
                        return "nt";
                    default:
                        throw new Exception("Unrecognised format requested.");
                }
            }

            var acceptHeaders = request.GetAcceptHeaderElements();
            foreach (var acceptToken in acceptHeaders)
            {
                var token = acceptToken;
                foreach (var entry in SupportedContentTypes)
                {
                    if (entry.Value.Any(ct => ct.Matches(token)))
                    {
                        return entry.Key;
                    }
                }
            }

            return "nt";
        }

        protected Dictionary<string, List<ContentType>> SupportedContentTypes =
            new Dictionary<string, List<ContentType>>
                        {
                            {
                                "nt", new List<ContentType>
                                                 {
                                                     new ContentType {MediaType = "text/plain"}
                                                 }
                                },
                                {
                                "xml", new List<ContentType>
                                                {
                                                    new ContentType {MediaType = "application/rdf+xml"}
                                                }
                                }
                        };

    }
}
