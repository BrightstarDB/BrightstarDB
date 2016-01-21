using System;
using System.Collections.Generic;
using BrightstarDB.Storage.Statistics;
using VDS.RDF;
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Query.Patterns;

namespace BrightstarDB.Query.Processor
{
    /* TODO:
        This still produces some suboptimal (even harmful) triple pattern orderings.
        It looks like the problem is that the ordering here treats each pattern independently
        rather than taking into account variables that are bound by preceeding triple patterns
        in the same BGP.
        The solution could be to first find the triple pattern that is the most specific,
        and then incrementally select the most specific of the remaining patterns given the
        variables bound in the previous ones.
        This would be a completely different approach to the DNR BaseQueryOptimiser.
    */


    /// <summary>
    /// Code based on the dotNetRDF WeightedOptimiser to perform triple pattern reordering based on computed pattern weight,
    /// using an IStoreStatistics object to provide predicate counts
    /// </summary>
    internal class BrightstarWeightedOptimiser : BaseQueryOptimiser
    {
        private StoreWeightings _weights;

        /// <summary>
        /// Default Weight for Subject Terms
        /// </summary>
        public const double DefaultSubjectWeight = 0.8d;

        /// <summary>
        /// Default Weight for Predicate Terms
        /// </summary>
        public const double DefaultPredicateWeight = 0.4d;

        /// <summary>
        /// Default Weight for Object Terms
        /// </summary>
        public const double DefaultObjectWeight = 0.6d;

        /// <summary>
        /// Default Weight for Variables
        /// </summary>
        public const double DefaultVariableWeight = 1d;

        /// <summary>
        /// Creates a new Weighted Optimiser
        /// </summary>
        public BrightstarWeightedOptimiser(StoreStatistics storeStatistics)
        {
            _weights = new StoreWeightings(storeStatistics);
        }

        /// <summary>
        /// Gets the comparer used to order the Triple Patterns based on their computed weightings
        /// </summary>
        /// <returns></returns>
        protected override IComparer<ITriplePattern> GetRankingComparer()
        {
            return new StoreWeightingComparer(_weights);
        }

    }

    internal class StoreWeightings
    {
        private StoreStatistics _statistics;

        private double _defSubjWeight = WeightedOptimiser.DefaultSubjectWeight;
        private double _defPredWeight = WeightedOptimiser.DefaultPredicateWeight;
        private double _defObjWeight = WeightedOptimiser.DefaultObjectWeight;
        private double _defVarWeight = WeightedOptimiser.DefaultVariableWeight;

        public StoreWeightings(StoreStatistics s)
        {
            if (s == null) return;
            _statistics = s;
        }


        public double PredicateWeighting(INode n)
        {
            var u = n as IUriNode;
            PredicateStatistics temp;
            if (u != null && _statistics.PredicateStatistics.TryGetValue(u.Uri.ToString(), out temp))
            {
                return (double)temp.TripleCount/_statistics.TripleCount;
            }
            else
            {
                // Note: this assumes stats are complete and therefore this predicate will never match
                return 0d;
            }
        }

        public double PredicateObjectWeighting(INode n)
        {
            var u = n as IUriNode;
            PredicateStatistics temp;
            if (u != null && _statistics.PredicateStatistics.TryGetValue(u.Uri.ToString(), out temp))
            {
                return (temp.ObjectPredicateTripleCount) / _statistics.TripleCount;
            }
            else
            {
                return 0d;
            }
        }

        public double PredicateSubjectWeighting(INode n)
        {
            var u = n as IUriNode;
            PredicateStatistics temp;
            if (u != null && _statistics.PredicateStatistics.TryGetValue(u.Uri.ToString(), out temp))
            {
                return temp.SubjectPredicateTripleCount / _statistics.TripleCount;
            }
            else
            {
                return 0d;
            }
        }

        public double SubjectWeighting(INode n)
        {
            // Currently no statistics available so always return default
            return 1d - this._defSubjWeight;
        }

        public double ObjectWeighting(INode n)
        {
            // Currently no statistics available so always return default
            return 1d - this._defSubjWeight;
        }

        public double DefaultSubjectWeighting
        {
            get { return this._defSubjWeight; }
            set { this._defSubjWeight = Math.Min(Math.Max(Double.Epsilon, value), 1d); }
        }

        public double DefaultPredicateWeighting
        {
            get { return this._defPredWeight; }
            set { this._defPredWeight = Math.Min(Math.Max(Double.Epsilon, value), 1d); }
        }

        public double DefaultObjectWeighting
        {
            get { return this._defObjWeight; }
            set { this._defObjWeight = Math.Min(Math.Max(Double.Epsilon, value), 1d); }
        }

        public double DefaultVariableWeighting
        {
            get { return this._defVarWeight; }
        }
    }

    internal class StoreWeightingComparer
        : IComparer<ITriplePattern>
    {
        private StoreWeightings _weights;

        public StoreWeightingComparer(StoreWeightings weights)
        {
            if (weights == null) throw new ArgumentNullException("weights");
            this._weights = weights;
        }

        public int Compare(ITriplePattern x, ITriplePattern y)
        {
            double xSubj, xPred, xObj;
            double ySubj, yPred, yObj;

            this.GetSelectivities(x, out xSubj, out xPred, out xObj);
            this.GetSelectivities(y, out ySubj, out yPred, out yObj);

            double xSel = xSubj*xPred*xObj;
            double ySel = ySubj*yPred*yObj;

            int c = xSel.CompareTo(ySel);
            if (c == 0)
            {
                //Fall back to standard ordering if selectivities are equal
                c = x.CompareTo(y);
            }
#if DEBUG
            Logging.LogInfo("Compare: {0}[{1}] - {2}[{3}] = {4}", x.ToString(), xSel, y.ToString(), ySel, c);
#endif
            return c;
        }

        private void GetSelectivities(ITriplePattern x, out double subj, out double pred, out double obj)
        {
            switch (x.PatternType)
            {
                case TriplePatternType.Match:
                    IMatchTriplePattern p = (IMatchTriplePattern) x;
                    switch (p.IndexType)
                    {
                        case TripleIndexType.NoVariables:
                            // Most selective
                            subj = pred = obj = 0d;
                            break;
                        case TripleIndexType.Object:
                            subj = this._weights.DefaultVariableWeighting;
                            pred = this._weights.DefaultVariableWeighting;
                            obj = this._weights.DefaultObjectWeighting;
                            break;
                        case TripleIndexType.Predicate:
                            subj = this._weights.DefaultVariableWeighting;
                            pred = this._weights.PredicateWeighting(((NodeMatchPattern) p.Predicate).Node);
                            obj = this._weights.DefaultVariableWeighting;
                            break;
                        case TripleIndexType.PredicateObject:
                            subj = this._weights.DefaultVariableWeighting;
                            pred = this._weights.PredicateObjectWeighting(((NodeMatchPattern) p.Predicate).Node);
                            obj = 1d;
                            break;
                        case TripleIndexType.Subject:
                            subj = this._weights.DefaultSubjectWeighting;
                            pred = this._weights.DefaultVariableWeighting;
                            obj = this._weights.DefaultVariableWeighting;
                            break;
                        case TripleIndexType.SubjectObject:
                            subj = this._weights.DefaultSubjectWeighting;
                            pred = this._weights.DefaultVariableWeighting;
                            obj = this._weights.DefaultObjectWeighting;
                            break;
                        case TripleIndexType.SubjectPredicate:
                            subj = 1d;
                            pred = this._weights.PredicateSubjectWeighting(((NodeMatchPattern) p.Predicate).Node);
                            obj = this._weights.DefaultVariableWeighting;
                            break;
                        default:
                            //Shouldn't see an unknown index type but have to keep the compiler happy
                            subj = 1d;
                            pred = 1d;
                            obj = 1d;
                            break;
                    }
                    break;
                default:
                    //Otherwise all are considered to have equivalent selectivity
                    subj = 1d;
                    pred = 1d;
                    obj = 1d;
                    break;
            }
        }
    }
}
