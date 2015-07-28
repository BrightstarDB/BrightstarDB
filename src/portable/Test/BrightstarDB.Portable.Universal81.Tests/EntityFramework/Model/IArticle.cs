using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
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
