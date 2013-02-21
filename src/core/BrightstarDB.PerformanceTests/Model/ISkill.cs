using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.PerformanceTests.Model
{
    [Entity]
    public interface ISkill
    {
        [Identifier("http://www.examplevocab.com/schema/skill/")]
        string Id { get; }
        
        string Description { get; set; }
        string Title { get; set; }
        
        [InverseProperty("Skills")]
        ICollection<IPerson> SkilledPeople { get; set; }
    }
}
