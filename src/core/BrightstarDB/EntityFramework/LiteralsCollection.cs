using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using BrightstarDB.Client;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Provides access to a list of literal values attached to a BrightstarEntityObject.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LiteralsCollection<T> : ICollection<T>, INotifyCollectionChanged
    {
        private readonly bool _isAttached;
        private readonly BrightstarEntityObject _beo;
        private readonly string _propertyTypeUri;
#if SILVERLIGHT
        private readonly List<T> _items;
#else
        private readonly HashSet<T> _items;
#endif

        /// <summary>
        /// Initializes an literals collection for an unattached entity
        /// </summary>
        /// <param name="initialValues"></param>
        internal LiteralsCollection(IEnumerable<T> initialValues)
        {
#if SILVERLIGHT
            _items = new List<T>(initialValues);
#else
            _items = new HashSet<T>(initialValues);
#endif
            _isAttached = false;
        }

        internal LiteralsCollection(BrightstarEntityObject beo, string propertyTypeUri)
        {
            _beo = beo;
            _propertyTypeUri = propertyTypeUri;
            _isAttached = true;
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            return GetPropertyValues().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Implementation of ICollection<T>

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public void Add(T item)
        {
            if (!typeof(T).IsValueType && item == null) throw new ArgumentNullException("item");
            if (_isAttached)
            {
                _beo.DataObject.AddProperty(_propertyTypeUri, item);
            }
            else
            {
                _items.Add(item);
            }
#if WINDOWS_PHONE
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, 0));
#else
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
#endif
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            if (_isAttached)
            {
                _beo.DataObject.RemovePropertiesOfType(_propertyTypeUri);
            }
            else
            {
                _items.Clear();
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        public bool Contains(T item)
        {
            if (_isAttached)
            {
                return GetPropertyValues().Any(x => x.Equals(item));
            }
            return _items.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException">
        /// <paramref name="array"/> is multidimensional.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type T cannot be cast automatically to the type of the destination <paramref name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            GetPropertyValues().ToList().CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public bool Remove(T item)
        {
            if (_isAttached)
            {
                if (GetPropertyValues().Any(x => x.Equals(item)))
                {
                    _beo.DataObject.RemoveProperty(_propertyTypeUri, item);
#if WINDOWS_PHONE
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
#else
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
#endif
                    return true;
                }
                return false;
            }
            bool removed = _items.Remove(item);
            if (removed)
            {
#if WINDOWS_PHONE
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
#else
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
#endif
            }
            return removed;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count
        {
            get
            {
                return GetPropertyValues().Count();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
        /// </returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        /// <summary>
        /// Adds a collection of items to this collection
        /// </summary>
        /// <param name="items">The items to be added</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is null or one of its members is null.</exception>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            var addItems = items.ToList();
            foreach(var item in addItems) Add(item);
#if WINDOWS_PHONE
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addItems, 0));
#else
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addItems));
#endif
        }

        private IEnumerable<T> GetPropertyValues()
        {
            if (!_isAttached) return _items;
            if (typeof (T) == typeof (Uri))
            {
                return
                    _beo.DataObject.GetPropertyValues(_propertyTypeUri)
                        .OfType<IDataObject>()
                        .Select(dataObject => new Uri(dataObject.Identity))
                        .Cast<T>();
            }
            return _beo.DataObject.GetPropertyValues(_propertyTypeUri).OfType<T>();
        }

        #region Implementation of INotifyCollectionChanged

        /// <summary>
        /// Occurrs when an item is added, removed, changed, moved or the entire list is refreshed
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with the provided arguments
        /// </summary>
        /// <param name="e">Arguments of the event being raised</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }
        #endregion
    }
}
