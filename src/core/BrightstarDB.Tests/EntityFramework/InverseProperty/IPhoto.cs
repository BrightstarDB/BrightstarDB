using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework.InverseProperty
{
    [Entity]
    public interface IPhoto
    {
        string Id { get; }

        [PropertyType("http://example.org/production")]
        IProduction Production { get; set; }
    }
}