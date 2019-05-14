using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface ISkill
    {
        [Identifier("http://example.org/skills#")]
        string Id { get; }

        string Name { get; set; }

        [InverseProperty("MainSkill")]
        IPerson Expert { get; set; }
        
        [PropertyType("broader")]
        ISkill Parent { get; set; }

        [InversePropertyType("broader")]
        ICollection<ISkill> Children { get; set; }

        [InverseProperty("Skills")]
        ICollection<IPerson> SkilledPeople { get; set; }
    }
}
