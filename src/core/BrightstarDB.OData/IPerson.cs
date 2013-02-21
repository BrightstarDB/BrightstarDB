using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData
{
    [Entity]
    public interface IPerson
    {
        [Identifier("http://www.brightstardb.com/.well-known/genid/")]
        string Id { get; }
        string Name { get; set; }
        string Email { get; set; }
        int EmployeeNumber { get; set; }

        ICollection<ISkill> Skills { get; set; }
        ISkill MainSkill { get; set; }
        ICollection<ISkill> BackupSkills { get; set; }

        [InverseProperty("Members")]
        IDepartment Department { get; set; }
    }
}