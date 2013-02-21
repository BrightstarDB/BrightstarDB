namespace BrightstarDB.EntityFramework.Query
{
    internal class TripleInfo
    {
        public GraphNode SubjectType { get; private set; }
        public GraphNode VerbType { get; private set; }
        public GraphNode ObjectType { get; private set; }
        public string Subject { get; private set; }
        public string Verb { get; private set; }
        public string Object { get; private set; }

        public TripleInfo(GraphNode subjectType, string subject, GraphNode verbType, string verb, GraphNode objectType, string obj)
        {
            SubjectType = subjectType;
            Subject = subject;
            VerbType = verbType;
            Verb = verb;
            ObjectType = objectType;
            Object = obj;
        }
    }
}