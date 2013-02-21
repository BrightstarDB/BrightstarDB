using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData
{
    [Entity]
    public interface ISkill
    {
        [Identifier("http://www.brightstardb.com/.well-known/genid/")]
        string Id { get; }
        string Name { get; set; }

        [InverseProperty("MainSkill")]
        IPerson MainExpert { get; set; }

        [InverseProperty("BackupSkills")]
        ICollection<IPerson> BackupPeople { get; set; }
    }
}