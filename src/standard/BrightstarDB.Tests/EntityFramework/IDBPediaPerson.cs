using System;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity("http://xmlns.com/foaf/0.1/")]
    public interface IDBPediaPerson
    {
        [Identifier("http://dbpedia.org/resource/")]
        string Id { get; }

        [PropertyType("http://dbpedia.org/ontology/birthDate")]
        DateTime BirthDate { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/name")]
        string Name { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/givenName")]
        string GivenName { get; set; }

        [PropertyType("http://xmlns.com/foaf/0.1/surname")]
        string Surname { get; set; }
    }
}
