namespace BrightstarDB.Server.Modules.Model
{
    public class SparqlRequestObject
    {
        public string Query { get; set; }
        public string CommitId { get; set; }
        public string[] DefaultGraphUri { get; set; }
        public string[] NamedGraphUri { get; set; }
        public string Format { get; set; }
    }
}
