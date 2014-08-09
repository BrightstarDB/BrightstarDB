using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework.InverseProperty
{
    [Entity]
    public interface IProductionPerson
    {
        string Id { get; }
        
        string Name { get; set; }
    }
}