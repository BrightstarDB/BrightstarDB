using System.Collections.Generic;
using BrightstarDB.EntityFramework;
using BrightstarDB.Rdf;

namespace BrightstarDB.Portable.Tests.EntityFramework.Model
{
    [Entity("http://www.w3.org/2004/02/skos/core#Concept")]
    public interface IConcept
    {
        string Id { get; }

        [PropertyType("http://www.w3.org/2004/02/skos/core#prefLabel")]
        ICollection<PlainLiteral> PrefLabel { get; set; }
    }
}
