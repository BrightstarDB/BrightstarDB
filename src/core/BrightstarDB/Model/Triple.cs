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

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Equality comparison. Unlike the Match operation, the Equals operation requires direct value equality.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as Triple;
            if (other == null) return false;
            return (Subject == null && other.Subject == null || Subject != null && Subject.Equals(other.Subject)) &&
                   (Predicate == null && other.Predicate == null ||
                    Predicate != null && Predicate.Equals(other.Predicate)) &&
                   IsLiteral.Equals(other.IsLiteral) &&
                   (Object == null && other.Object == null || Object != null && Object.Equals(other.Object)) &&
                   (Graph == null && other.Graph == null || Graph != null && Graph.Equals(other.Graph)) && 
                   (DataType == null && other.DataType == null ||
                    DataType != null && DataType.Equals(other.DataType)) &&
                   (LangCode == null && other.LangCode == null ||
                    LangCode != null && LangCode.Equals(other.LangCode));
        }
    }
}
