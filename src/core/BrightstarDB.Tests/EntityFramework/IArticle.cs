using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IArticle : ITrackable
    {
        string Id { get; }

        string Title { get; set; }
        string BodyText { get; set; }

        IPerson Publisher { get; set; }
    }
}
