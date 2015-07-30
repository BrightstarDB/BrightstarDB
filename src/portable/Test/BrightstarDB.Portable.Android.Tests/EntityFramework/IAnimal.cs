using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Android.Tests.EntityFramework
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
