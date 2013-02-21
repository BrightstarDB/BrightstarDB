using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData.Tests
{
    [Entity]
    public interface IDepartment
    {
        [Identifier("http://example.org/departments/")]
        string Id { get; }
        string Name { get; set; }

        int DeptId { get; set; }

        [InverseProperty("Department")]
        ICollection<IPerson> Persons { get; }

        ICompany Company { get; set; }
    }
}
