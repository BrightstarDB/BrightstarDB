using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework.Query;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// A generic, dynamically loaded collection of Brightstar EntityFramework objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class BrightstarEntityCollection<T> : QueryableBase<T>, IBrightstarEntityCollection, IEntityCollection<T>, INotifyCollectionChanged where T:class
    {
        private readonly BrightstarEntityContext _context;
        private readonly BrightstarEntityObject _parent;
        private readonly IDataObject _propertyType;
        private readonly bool _isInverse;
        private readonly string _propertyTypeUri;
        private readonly string _itemTypeUri;

        /// <summary>
        /// Get the URI identifier of the parent of the collection
        /// </summary>
        public string ParentIdentity { get { return _parent.DataObject.Identity; } }

        /// <summary>
        /// Get the URI identifier of the property used to build the collection
        /// </summary>
        public string PropertyIdentity { get { return _propertyType.Identity; } }

        /// <summary>
        /// Get the boolean flag that indicates if the collection is built from an inverse property
        /// </summary>
        public bool IsInverseProperty { get { return _isInverse; } }

        /// <summary>
        /// Creates a new entity collection
        /// </summary>
        /// <param name="context">The context that manages the entities</param>
        /// <param name="parent">The parent entity that contains this collection</param>
        /// <param name="propertyType">The property type that the collection maps to</param>
        /// <param name="isInverse">True if the collection represents the inverse of <paramref name="propertyType"/></param>
        public BrightstarEntityCollection(BrightstarEntityContext context, BrightstarEntityObject parent, string propertyType, bool isInverse = false) :
            base(new EntityFrameworkCollectionQueryProvider(QueryParser.CreateDefault(), new EntityFrameworkQueryExecutor(context)))
        {
            _context = context;
            _parent = parent;
            _propertyTypeUri = propertyType;
            _propertyType = _context.GetDataObject(new Uri(propertyType), false);
            _itemTypeUri = context.MapTypeToUri(typeof (T));
            _isInverse = isInverse;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryProvider"></param>
        /// <param name="expression"></param>
        public BrightstarEntityCollection(IQueryProvider queryProvider, Expression expression) : base(queryProvider, expression)
        {
            
        }

        #region Implementation of IEntityCollection<T>
        /// <summary>
        /// Updates this collection to only contain the specified items
        /// </summary>
        /// <param name="items"></param>
        public void Set(ICollection<T> items)
        {
            if (IsLoaded)
            {
                foreach (var item in items.Cast<BrightstarEntityObject>().Except(LoadedObjects, new BrightstarEntityObjectComparer()))
                {
                    Add(item as T);
                }
                foreach(var item in LoadedObjects.Except(items.Cast<BrightstarEntityObject>(), new BrightstarEntityObjectComparer()).ToList())
                {
                    Remove(item as T);
                }
            }
            else
            {
                var entities = items.Select(x => AssertBrightstarObject(x, "items")).ToList();
                if (_isInverse)
                {
                    _parent.DataObject.RemoveInversePropertiesOfType(_propertyType);
                    foreach (var entity in entities)
                    {
                        entity.DataObject.AddProperty(_propertyType, _parent.DataObject);
                    }
                }
                else
                {
                    _parent.DataObject.RemovePropertiesOfType(_propertyType);
                    foreach (var entity in entities)
                    {
                        _parent.DataObject.AddProperty(_propertyType, entity.DataObject);
                    }
                    SetLoadedObjects(entities);
                }
            }
        }


        #endregion
        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public new IEnumerator<T> GetEnumerator()
        {
            AssertLoaded();
            return LoadedObjects.Cast<T>().GetEnumerator();
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
            var entity = AssertBrightstarObject(item, "item");
            if (_isInverse)
            {
                _context.AddArc(entity, _propertyTypeUri, _parent, false);
            }
            else
            {
                _context.AddArc(_parent, _propertyTypeUri, entity, false);
            }
            if (IsLoaded)
            {
                AddToLoadedObjects(entity);
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            _parent.DataObject.RemovePropertiesOfType(_propertyType);
            if (IsLoaded)
            {
                SetLoadedObjects(new BrightstarEntityObject[0]);
            }
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
            if (!(item is BrightstarEntityObject)) return false;
            var entity = AssertBrightstarObject(item, "item");
            return LoadedObjects.Any(x => x.DataObject.Identity.Equals(entity.DataObject.Identity));
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <typeparamref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            AssertLoaded();
            foreach(var o in LoadedObjects.Cast<T>())
            {
                array[arrayIndex++] = o;
            }
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
            var entityToRemove = AssertBrightstarObject(item, "item");
            _parent.DataObject.RemoveProperty(_propertyType, entityToRemove.DataObject);
            if (IsLoaded)
            {
                RemoveFromLoadedObjects(entityToRemove.DataObject.Identity);
            }
            else
            {
#if WINDOWS_PHONE || PORTABLE
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
#else
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
#endif
            }
            return true;
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
                AssertLoaded();
                return LoadedObjectsCount;
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
        /// Adds all items in the enumeration to this collection
        /// </summary>
        /// <param name="items">The items to be added</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> or one of its members is null.</exception>
        public void AddRange(IEnumerable<T> items)
        {
            foreach(var item in items) Add(item);
        }

        #region Implementation of IEntityCollection

        /// <summary>
        /// Loads the entity collection from the store
        /// </summary>
        public void Load()
        {
            var dataObjects = _isInverse
                                  ? _parent.DataObject.GetInverseOf(_propertyType)
                                  : _parent.DataObject.GetPropertyValues(_propertyType).OfType<IDataObject>();
            dataObjects = dataObjects.Where(x => x.GetTypes().Contains(_itemTypeUri));
            SetLoadedObjects(dataObjects.Select(_context.Bind<T>).Cast<BrightstarEntityObject>());
        }

        /// <summary>
        /// Removes an entity object from the collection
        /// </summary>
        /// <param name="toRemove"></param>
        internal void InternalRemove(IEntityObject toRemove)
        {
            if (!typeof(T).IsAssignableFrom(toRemove.GetType()))
            {
                throw new ArgumentException(String.Format(Strings.InvalidEntityType, typeof(T).FullName), "toRemove");
            }
            if (IsLoaded)
            {
                var entityToRemove = toRemove as BrightstarEntityObject;
                if (entityToRemove != null)
                {
                    RemoveFromLoadedObjects(entityToRemove.DataObject.Identity);
                }
            }
        }

        /// <summary>
        /// Adds an entity object to the collection
        /// </summary>
        /// <param name="toAdd"></param>
        internal void InternalAdd(IEntityObject toAdd)
        {
            if (!(toAdd is BrightstarEntityObject))
            {
                throw new ArgumentException(String.Format(Strings.InvalidEntityType, typeof(BrightstarEntityObject).FullName), "toAdd");
            }
            if (IsLoaded)
            {
                RemoveFromLoadedObjects((toAdd as BrightstarEntityObject).DataObject.Identity);
            }
        }

        #endregion

        private void AssertLoaded()
        {
            if (!IsLoaded) Load();
        }

        private BrightstarEntityObject AssertBrightstarObject(object o, string argumentName)
        {
            var beo = o as BrightstarEntityObject;
            if (beo == null)
            {
                throw new ArgumentException( String.Format(Strings.InvalidEntityType, typeof(BrightstarEntityObject).FullName), argumentName);
            }
            beo.AssertIdentity();
            if (!beo.Context.Equals(_context))
            {
                beo.Attach(_context);
            }
            return beo;
        }

        #region Implementation of IBrightstarEntityCollection
        private List<BrightstarEntityObject> _loadedObjects;

        /// <summary>
        /// Get an enumeration over the loaded Brightstar EntityFramework objects managed by this collection
        /// </summary>
        /// <remarks>If objects are not yet loaded, this property returns an empty enumeration. Use <see cref="IsLoaded"/> to check if objects are loaded.</remarks>
        public IEnumerable<BrightstarEntityObject> LoadedObjects { get { return _loadedObjects ?? (IEnumerable<BrightstarEntityObject>)new BrightstarEntityObject[0]; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        public void AddToLoadedObjects(BrightstarEntityObject o)
        {
            if (_loadedObjects == null)
            {
                // Still do the notification, even if the collection is not loaded
#if WINDOWS_PHONE || PORTABLE
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, o, 0));
#else
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, o));
#endif
                return;
            }
            if (!_loadedObjects.Any(x => x.DataObject.Identity.Equals(o.DataObject.Identity)))
            {
                _loadedObjects.Add(o);
#if WINDOWS_PHONE || PORTABLE
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, o, 0));
#else
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, o));
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identity"></param>
        public void RemoveFromLoadedObjects(string identity)
        {
            if (_loadedObjects == null)
            {
                // If the removed item is not currently tracked, notify the remove event with the string identity
                // otherwise with the tracked item.
                var removedItem = _context.GetTrackedObject(identity);
                if (removedItem == null)
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                else
                {
#if WINDOWS_PHONE || PORTABLE
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                                             removedItem, 0));
#else
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                            removedItem));

#endif
                }
                return;
            }
            var toRemove = _loadedObjects.Where(o => o.DataObject.Identity.Equals(identity)).ToList();
            _loadedObjects.RemoveAll(o => o.DataObject.Identity.Equals(identity));
#if WINDOWS_PHONE || PORTABLE
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, toRemove, 0));
#else
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, toRemove));
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        public void SetLoadedObjects(IEnumerable<BrightstarEntityObject> entities)
        {
            _loadedObjects = entities.ToList();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Returns the number of loaded objects managed by this collection
        /// </summary>
        /// <remarks>Returns zero if the collection is not yet loaded. Use the method <see cref="IsLoaded"/> to check 
        /// whether the object collection is loaded or not.</remarks>
        public int LoadedObjectsCount { get { return _loadedObjects == null ? 0 : _loadedObjects.Count; } }

        /// <summary>
        /// Returns a flag indicating if the object collection is currently loaded
        /// </summary>
        public bool IsLoaded { get { return _loadedObjects != null; } }
        #endregion

        #region Implementation of INotifyCollectionChanged

        /// <summary>
        /// Occurs when items are added to or removed from the collection or the collection is reset
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }
        #endregion
    }

}
