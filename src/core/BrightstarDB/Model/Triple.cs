namespace BrightstarDB.Model
{
    internal class Triple : ITriple
    {
        public Triple()
        {
            Graph = Constants.DefaultGraphUri;
        }

        public string Graph { get; set; }
        public string Subject { get; set; }
        public string Predicate { get; set; }
        public string Object { get; set; }
        public bool IsLiteral { get; set; }
        public string DataType { get; set; }
        public string LangCode { get; set; }

        /// <summary>
        /// Returns true if this triple matches the specified triple allowing
        /// NULL in Graph, Subject, Predicate an Object to stand for a wildcard
        /// </summary>
        /// <param name="other">The other triple to match with</param>
        /// <returns>True if there is a match in the non-null parts of both triples, false otherwise</returns>
        public bool Matches(ITriple other)
        {
            return NullOrMatch(Graph, other.Graph) &&
                   NullOrMatch(Subject, other.Subject) &&
                   NullOrMatch(Predicate, other.Predicate) &&
                   (Object == null || other.Object == null ||
                    (
                        IsLiteral == other.IsLiteral &&
                        DataType == other.DataType &&
                        LangCode == other.LangCode &&
                        Object == other.Object
                    ));
        }

        private static bool NullOrMatch(string x, string y)
        {
            return x == null || y == null || x.Equals(y);
        }

        public override string ToString()
        {
            if (IsLiteral)
            {
                return string.Format("<{0}> <{1}> {2}^^{3}@{4}", Subject, Predicate, Object, DataType, LangCode);
            }
            return string.Format("<{0}> <{1}> <{2}>", Subject, Predicate, Object);
        }
    }
}
