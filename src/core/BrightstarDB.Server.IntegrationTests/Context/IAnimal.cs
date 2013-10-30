using BrightstarDB.EntityFramework;

namespace BrightstarDB.Server.IntegrationTests.Context
{
    [Entity]
    public interface IAnimal
    {
        [Identifier("bsi:Animals/")]
        string Id { get; }
        
        string Name { get; set; }

        [InverseProperty("Pet")]
        IPerson Owner { get; set; }
    }
}
