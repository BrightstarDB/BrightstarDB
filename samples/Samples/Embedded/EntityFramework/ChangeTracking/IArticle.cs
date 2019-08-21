using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.ChangeTracking
{
    [Entity]
    interface IArticle : ITrackable
    {
        string Id { get; }
        string Title { get; set; }
        string BodyText { get; set; }
    }
}
