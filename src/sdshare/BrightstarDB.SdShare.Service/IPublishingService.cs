using System.IO;
using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;

namespace BrightstarDB.SdShare.Service
{
    [ServiceContract]
    public interface IPublishingService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/status")]
        Stream Status();

        [OperationContract]
        [WebGet(UriTemplate = "/ping")]
        string Ping();

        [OperationContract]
        [WebInvoke(UriTemplate = "/resourcesink?uri={resourceuri}", Method = "POST")]
        void ResourceSink(string resourceuri, Stream data);

        [OperationContract]
        [WebGet(UriTemplate = "/collections", ResponseFormat = WebMessageFormat.Xml)]
        Atom10FeedFormatter GetCollectionsFeed();

        [OperationContract]
        [WebGet(UriTemplate = "/collections/{collectionId}", ResponseFormat = WebMessageFormat.Xml)]
        Atom10FeedFormatter GetCollectionFeed(string collectionId);

        [OperationContract]
        [WebGet(UriTemplate = "/collections/{collectionId}/snapshots", ResponseFormat = WebMessageFormat.Xml)]
        Atom10FeedFormatter GetCollectionSnapshotsFeed(string collectionId);

        [OperationContract]
        [WebGet(UriTemplate = "/collections/{collectionId}/snapshots/{id}", ResponseFormat = WebMessageFormat.Xml)]
        Stream GetCollectionSnapshot(string collectionId, string id);

        [OperationContract]
        [WebGet(UriTemplate = "/collections/{collectionId}/fragments?since={since}&before={before}&page={page}", ResponseFormat = WebMessageFormat.Xml)]
        Atom10FeedFormatter GetCollectionFragmentsFeed(string collectionId, string since, string before, string page);

        [OperationContract]
        [WebGet(UriTemplate = "/collections/{collectionId}/fragment?id={fragmentId}", ResponseFormat = WebMessageFormat.Xml)]
        Stream GetFragment(string collectionId, string fragmentId);
    }
}