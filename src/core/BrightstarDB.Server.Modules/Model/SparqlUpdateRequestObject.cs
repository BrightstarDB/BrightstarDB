namespace BrightstarDB.Server.Modules.Model
{
    public class SparqlUpdateRequestObject
    {
        public string StoreName { get; set; }
        public string Update { get; set; }
        public string[] UsingGraphUri { get; set; }
        public string[] UsingNamedGraphUri { get; set; }
    }
}