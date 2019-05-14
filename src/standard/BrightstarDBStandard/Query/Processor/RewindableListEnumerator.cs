using System;
using System.Collections;
using System.Collections.Generic;

namespace BrightstarDB.Query.Processor
{
    internal class RewindableListEnumerator<T> : IEnumerator<T>
    {
        private List<T> _list;
        private int _index;
        private int _mark;

        public RewindableListEnumerator(List<T> list)
        {
            _list = list;
            _index = -1;
            _mark = -1;
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _list = null;
        }

        #endregion

        #region Implementation of IEnumerator

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            if (_index == _list.Count) return false;
            _index++;
            return !(_index >= _list.Count);
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            _index = -1;
            _mark = -1;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public T Current
        {
            get { if (_index < 0) throw new InvalidOperationException("The enumerator is positioned before the first element of the list");
            if (_index >= _list.Count) throw new InvalidOperationException("The enumerator is positioned after the last element of the list");
                return _list[_index];
            }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception><filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        #endregion

        /// <summary>
        /// Sets the rewind mark to the current position of the enumerator in the list
        /// </summary>
        public void SetMark()
        {
            _mark = _index;
        }

        /// <summary>
        /// Rewinds the enumerator to the last marked position in the list
        /// </summary>
        public void RewindToMark()
        {
            _index = _mark;
        }
    }
}
