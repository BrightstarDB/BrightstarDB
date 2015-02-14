using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    internal sealed class TripleCollection : ITripleCollection
    {
        private readonly Dictionary<string, Dictionary<string, HashSet<ITriple>>> _tripleIndex;

        public TripleCollection()
        {
            _tripleIndex = new Dictionary<string, Dictionary<string, HashSet<ITriple>>>();
        }

        public void Add(ITriple triple)
        {
            if (triple == null) throw new ArgumentNullException("triple");
            Dictionary<string, HashSet<ITriple>> subjIndex;
            if (!_tripleIndex.TryGetValue(triple.Subject, out subjIndex))
            {
                _tripleIndex.Add(triple.Subject,
                    new Dictionary<string, HashSet<ITriple>> {{triple.Predicate, new HashSet<ITriple> {triple}}});
            }
            else
            {
                HashSet<ITriple> predTriples;
                if (!subjIndex.TryGetValue(triple.Predicate, out predTriples))
                {
                    subjIndex.Add(triple.Predicate, new HashSet<ITriple> {triple});
                }
                else
                {
                    predTriples.Add(triple);
                }
            }
        }

        public void AddRange(IEnumerable<ITriple> triples)
        {
            if (triples == null) throw new ArgumentNullException("triples");
            foreach (ITriple t in triples) Add(t);
        }

        public void Remove(ITriple triple)
        {
            if (triple.IsLiteral)
            {
                RemoveBySubjectPredicateLiteral(triple.Subject, triple.Predicate, triple.Object, triple.DataType,
                    triple.LangCode);
            }
            else
            {
                RemoveBySubjectPredicateObject(triple.Subject, triple.Predicate, triple.Object);
            }
        }

        public void RemoveBySubject(string subject)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            _tripleIndex.Remove(subject);
        }

        public void RemoveBySubjectPredicate(string subject, string predicate)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (predicate == null) throw new ArgumentNullException("predicate");
            Dictionary<string, HashSet<ITriple>> predicateIndex;
            if (_tripleIndex.TryGetValue(subject, out predicateIndex))
            {
                predicateIndex.Remove(predicate);
            }
        }

        public void RemoveBySubjectPredicateObject(string subject, string predicate, string obj)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (predicate == null) throw new ArgumentNullException("predicate");
            if (obj == null) throw new ArgumentNullException("obj");

            Dictionary<string, HashSet<ITriple>> predicateIndex;
            if (_tripleIndex.TryGetValue(subject, out predicateIndex))
            {
                HashSet<ITriple> triples;
                if (predicateIndex.TryGetValue(predicate, out triples))
                {
                    triples.RemoveWhere(t => t.Object.Equals(obj) && !t.IsLiteral);
                }
            }
        }

        public void RemoveBySubjectPredicateLiteral(string subject, string predicate, string literal, string dataType,
            string langCode)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (predicate == null) throw new ArgumentNullException("predicate");
            if (literal == null) throw new ArgumentNullException("literal");
            if (dataType == null) throw new ArgumentNullException("dataType");

            Dictionary<string, HashSet<ITriple>> predicateIndex;
            if (_tripleIndex.TryGetValue(subject, out predicateIndex))
            {
                HashSet<ITriple> triples;
                if (predicateIndex.TryGetValue(predicate, out triples))
                {
                    triples.RemoveWhere(
                        t =>
                            t.Object.Equals(literal) && t.IsLiteral && dataType.Equals(t.DataType) &&
                            (langCode == null || langCode.Equals(t.LangCode)));
                }
            }
        }

        public void RemoveByPredicateObject(string predicate, string obj)
        {
            if (predicate == null) throw new ArgumentNullException("predicate");
            if (obj == null) throw new ArgumentNullException("obj");
            foreach (var subjectIndex in _tripleIndex.Values)
            {
                HashSet<ITriple> triples;
                if (subjectIndex.TryGetValue(predicate, out triples))
                {
                    triples.RemoveWhere(x => !x.IsLiteral && x.Object.Equals(obj));
                }
            }
        }

        public void RemoveByObject(string obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            foreach (var tripleSet in _tripleIndex.Values.SelectMany(x => x.Values))
            {
                tripleSet.RemoveWhere(x => !x.IsLiteral && x.Object.Equals(obj));
            }
        }

        /// <summary>
        ///     Yields and enumeration over the triples in the collection that match the provided pattern.
        /// </summary>
        /// <param name="matchPattern">
        ///     The match pattern specified as a triple where a wildcard match for Subject, Predicate,
        ///     Object or Graph can be specified using NULL
        /// </param>
        /// <returns></returns>
        public IEnumerable<ITriple> GetMatches(ITriple matchPattern)
        {
            if (matchPattern == null) throw new ArgumentNullException("matchPattern");
            if (matchPattern.Subject == null)
            {
                foreach (ITriple t in _tripleIndex.Values.SelectMany(subjIndex => GetMatches(subjIndex, matchPattern)))
                {
                    yield return t;
                }
            }
            else
            {
                Dictionary<string, HashSet<ITriple>> si;
                if (_tripleIndex.TryGetValue(matchPattern.Subject, out si))
                {
                    foreach (ITriple t in GetMatches(si, matchPattern)) yield return t;
                }
            }
        }

        /// <summary>
        ///     Yields an enumeration over all the triples in the collection with a specified subject URI
        /// </summary>
        /// <param name="subject">The subject of the triples to yield</param>
        /// <returns>An enumeration of <see cref="BrightstarDB.Model.ITriple" /> instances</returns>
        public IEnumerable<ITriple> GetMatches(string subject)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            Dictionary<string, HashSet<ITriple>> si;
            if (_tripleIndex.TryGetValue(subject, out si))
            {
                foreach (ITriple t in si.Values.SelectMany(x => x))
                {
                    yield return t;
                }
            }
        }

        public IEnumerable<ITriple> GetMatches(string subject, string predicate)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (predicate == null) throw new ArgumentNullException("predicate");
            Dictionary<string, HashSet<ITriple>> si;
            if (_tripleIndex.TryGetValue(subject, out si))
            {
                HashSet<ITriple> triples;
                if (si.TryGetValue(predicate, out triples))
                {
                    foreach (ITriple t in triples) yield return t;
                }
            }
        }

        public IEnumerable<ITriple> GetMatches(string subject, string predicate, string obj)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            if (predicate == null) throw new ArgumentNullException("predicate");
            if (obj == null) throw new ArgumentNullException("obj");
            return GetMatches(subject, predicate).Where(t => !t.IsLiteral && t.Object.Equals(obj));
        }

        public IEnumerable<string> Subjects
        {
            get { return _tripleIndex.Keys; }
        }

        public IEnumerable<ITriple> Items
        {
            get { return _tripleIndex.Values.SelectMany(pt => pt.Values.SelectMany(t => t)); }
        }

        public void Clear()
        {
            _tripleIndex.Clear();
        }

        public bool ContainsSubject(string subject)
        {
            if (subject == null) throw new ArgumentNullException("subject");
            return _tripleIndex.ContainsKey(subject);
        }

        /// <summary>
        ///     Get the total number of triples held in this collection
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return _tripleIndex.Values.SelectMany(x => x.Values).Sum(x => x.Count);
        }

        private static IEnumerable<ITriple> GetMatches(Dictionary<string, HashSet<ITriple>> subjectIndex,
            ITriple matchPattern)
        {
            if (matchPattern.Predicate == null)
            {
                foreach (ITriple t in subjectIndex.Values.SelectMany(hs => hs.Where(matchPattern.Matches)))
                {
                    yield return t;
                }
            }
            else
            {
                HashSet<ITriple> triples;
                if (subjectIndex.TryGetValue(matchPattern.Predicate, out triples))
                {
                    foreach (ITriple t in triples.Where(matchPattern.Matches)) yield return t;
                }
            }
        }
    }
}