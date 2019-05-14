using System.Collections.Generic;
using VDS.RDF.Query.Patterns;

namespace BrightstarDB.Query.Processor
{
    internal abstract class CollapseGroup
    {
        protected List<string> SortVariables;
        public List<string> Variables; 

        protected CollapseGroup(ITriplePattern tp, IEnumerable<string> sortVariables)
        {
            Variables = new List<string>(tp.Variables);
            SortVariables = new List<string>();
            foreach (var sv in sortVariables)
            {
                if (tp.Variables.Contains(sv))
                {
                    SortVariables.Add(sv);
                }
            }
        }

        public abstract void AddTriplePattern(TriplePattern tp);

        public abstract void Evaluate();

        public abstract IAccumulator BuildAccumulator(IEnumerable<string> variables);

    }
}