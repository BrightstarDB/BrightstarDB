using Nancy;

namespace BrightstarDB.Server.Modules
{
    public class DocumentationModule : NancyModule
    {
        public DocumentationModule()
        {
            Get["/documentation"] = parameters => View["documentation.sshtml"];
        }
    }
}