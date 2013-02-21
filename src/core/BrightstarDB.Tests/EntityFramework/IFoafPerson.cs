using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity("http://xmlns.com/foaf/0.1/Person")]
    [ClassAttribute("[DisplayName(\"Person\")]")]
    public interface IFoafPerson : IFoafAgent
    {
        [Identifier("http://www.networkedplanet.com/people/")]
        string Id { get; }

        [PropertyType("http://xmlns.com/foaf/0.1/nick")]
        [DisplayName("Also Known As")]
        string Nickname { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/name")]
        [Required]
        [CustomValidation(typeof(MyCustomValidator), "ValidateName", ErrorMessage="Custom error message")]
        string Name { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/Organization")]
        string Organisation { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/knows")]
        ICollection<IFoafPerson> Knows { get; set; }

        [InversePropertyType("http://xmlns.com/foaf/0.1/knows")]
        ICollection<IFoafPerson> KnownBy { get; set; }

        [PropertyType("http://dbpedia.org/ontology/birthDate")]
        [DataType(DataType.Date)]
        DateTime? BirthDate { get; set; }

        
    }

    public class MyCustomValidator
    {

    }
}
