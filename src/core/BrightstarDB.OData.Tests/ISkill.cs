using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData.Tests
{
    [Entity]
    public interface ISkill
    {
        [Identifier("http://example.org/skills/")]
        string Id { get; }

        string Name { get; set; }

        [InverseProperty("Children")]
        ISkill Parent { get; set; }

        ICollection<ISkill> Children { get; set; }

        [InverseProperty("Skills")]
        ICollection<IPerson> SkilledPeople { get; set; }
    }
}
