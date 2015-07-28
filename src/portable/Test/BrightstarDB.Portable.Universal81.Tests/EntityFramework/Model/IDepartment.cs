using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity]
    public interface IDepartment
    {
        string Id { get; }
        string Name { get; set; }

        int DeptId { get; set; }

        [InverseProperty("Department")]
        ICollection<IPerson> Persons { get; }
    }
}
