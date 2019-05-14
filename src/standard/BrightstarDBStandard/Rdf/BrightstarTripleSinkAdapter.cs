using BrightstarDB.Model;

namespace BrightstarDB.Rdf
{
    /// <summary>
    /// A class which enables an instance of <see cref="ITripleSink"/>
    /// to accept a sequence of <see cref="Triple"/> objects 
    /// </summary>
    internal class BrightstarTripleSinkAdapter
    {
        private readonly ITripleSink _sink;
        public BrightstarTripleSinkAdapter(ITripleSink rdfTripleSink)
        {
            _sink = rdfTripleSink;
        }

        public void Triple(ITriple t)
        {
            string dt = null;
            if (t.IsLiteral && t.DataType != null) dt = t.DataType.ToString();
            _sink.Triple(t.Subject, t.Subject.StartsWith("_:"),
                         t.Predicate, t.Predicate.StartsWith("_:"),
                         t.Object.ToString(), !t.IsLiteral && t.ToString().StartsWith("_:"),
                         t.IsLiteral, dt, t.LangCode, t.Graph.ToString());
        }
    }
}
