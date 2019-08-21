using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.EntityFramework.ChangeTrackingCore
{
    [Entity]
    interface IArticle : ITrackable
    {
        string Id { get; }
        string Title { get; set; }
        string BodyText { get; set; }
    }
}
