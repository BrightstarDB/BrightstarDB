using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace BrightstarDB.Dynamic
{
    /// <summary>
    /// A Dynamic Collection is returned when a property is accessed on a dynamic brightstar object.
    /// </summary>
    public class DynamicCollection : DynamicObject, IEnumerable<object>
    {
        private readonly List<object> _collection;
        internal DynamicCollection(IEnumerable<object> collection)
        {
            _collection = collection.ToList();
        }

        /// <summary>
        /// Returns the first value in the collection
        /// </summary>
        /// <returns>The first object in the collecion.</returns>
        public object FirstOrDefault()
        {
            return _collection.FirstOrDefault();
        }

        /// <summary>
        /// Used to index into the collection
        /// </summary>
        /// <param name="ix">Position in the collection</param>
        /// <returns>Object as specified position</returns>
        public object this[int ix]
        {
            get { return _collection[ix]; }
        }

        /// <summary>
        /// Number of items in the collection
        /// </summary>
        public int Count { get { return _collection.Count; } }

        /// <summary>
        /// Returns an enumerator of items in the collection
        /// </summary>
        /// <returns></returns>
        public IEnumerator<object> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
    }
}