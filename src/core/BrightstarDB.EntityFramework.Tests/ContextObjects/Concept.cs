using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Rdf;

namespace BrightstarDB.EntityFramework.Tests.ContextObjects
{
    [Entity("Concept")]
    public interface IConcept
    {
        [Identifier]
        string Id { get; }

        [PropertyType("prefLabel")]
        PlainLiteral PrefLabel { get; set; }

        [PropertyType("altLabel")]
        ICollection<PlainLiteral> AltLabels { get; set; } 
    }

    class Concept : MockEntityObject, IConcept
    {
        public string Id { get; private set; }
        public PlainLiteral PrefLabel { get; set; }
        public ICollection<PlainLiteral> AltLabels { get; set; }
    }
}
