using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IUriEntity
    {
        [Identifier("")]
        string Id { get; }

        [PropertyType("http://www.w3.org/2000/01/rdf-schema#label")]
        string Label { get; set; }
    }
}
