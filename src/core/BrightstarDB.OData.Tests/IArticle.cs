using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData.Tests
{
    [Entity]
    public interface IArticle
    {
        [Identifier("http://example.org/articles/")]
        string Id { get; }

        string Title { get; set; }
        string BodyText { get; set; }

        IPerson Publisher { get; set; }
    }
}
