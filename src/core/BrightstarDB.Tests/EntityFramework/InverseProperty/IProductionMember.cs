using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework.InverseProperty
{
    [Entity]
    public interface IProductionMember
    {
        string Id { get; }
        [PropertyType("http://example.org/person")]
        IProductionPerson Person { get; set; }

        [PropertyType("http://example.org/production")]
        IProduction Production { get; set; }
    }

}
