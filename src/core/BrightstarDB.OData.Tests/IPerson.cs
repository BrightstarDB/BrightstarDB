using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.OData.Tests
{
    [Entity]
    public interface IPerson
    {
        [Identifier("http://example.org/people/")]
        string Id { get; }
        string Name { get; set; }
        DateTime? DateOfBirth { get; set; }
        int Age { get; set; }
        int Salary { get; set; }

        ICollection<ISkill> Skills { get; set; }

        IDepartment Department { get; set; }

        int EmployeeId { get; set; }

        IJobRole JobRole { get; set; }

        [InverseProperty("Publisher")]
        ICollection<IArticle> Articles { get; set; }

        ICollection<string> CollectionOfStrings { get; set; } 
    }
}
