using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.PerformanceTests.Model
{
    [Entity]
    public interface IPerson
    {
        [Identifier("http://www.examplevocab.com/schema/person/")]
        string Id { get; }
        
        string Fullname { get; set; }
        int Age { get; set; }
        int Salary { get; set; }
        DateTime DateOfBirth { get; set; }
        int EmployeeNumber { get; set; }

        ICollection<ISkill> Skills { get; set; }

        IDepartment Department { get; set; }

        IJobRole JobRole { get; set; }

        [InverseProperty("Publisher")]
        ICollection<IArticle> Articles { get; }
    }
}
