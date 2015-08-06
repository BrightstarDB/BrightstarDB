using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
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
