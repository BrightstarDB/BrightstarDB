using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// The base interface for the collections of entities provided by a <see cref="EntityContext"/>
    /// </summary>
    /// <typeparam name="T">The type of entity in the collection</typeparam>
    public interface IEntitySet<T> : IQueryable<T>, IEntitySet
    {
        /// <summary>
        /// Creates a new entity instance and adds it to the collection
        /// </summary>
        /// <returns>The new entity instance</returns>
        T Create();
        
        /// <summary>
        /// Adds a new item to the entity set
        /// </summary>
        /// <param name="item">The item to be added</param>
        /// <remarks>If the item does not yet have an identity, one will be generated for it</remarks>
        /// <exception cref="EntityFrameworkException">Throw if <paramref name="item"/> is attached to a context different to the one that this entity set is attached to</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="item"/> is not an instance of a class derived from <see cref="BrightstarEntityObject"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is null</exception>
        void Add(T item);

        /// <summary>
        /// Adds a new item to the entity set, attaching it to the specified resource address
        /// </summary>
        /// <param name="item">The item to be added</param>
        /// <param name="resourceAddress">The resource address that the item is to be attached to</param>
        /// <exception cref="EntityFrameworkException">Throw if <paramref name="item"/> is attached to a context different to the one that this entity set is attached to</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="item"/> is not an instance of a class derived from <see cref="BrightstarEntityObject"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is null</exception>
        void Add(T item, string resourceAddress);

        /// <summary>
        /// Adds a collection of entities to the store collection
        /// </summary>
        /// <param name="items">The entities to be added</param>
        /// <exception cref="EntityFrameworkException">Throw if one of the <paramref name="items"/> is attached to a context different to the one that this entity set is attached to</exception>
        /// <exception cref="ArgumentException">Thrown if one of the <paramref name="items"/> is not an instance of a class derived from <see cref="BrightstarEntityObject"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is null or one of its members is NULL</exception>
        void AddRange(IEnumerable<T> items);

        /// <summary>
        /// Adds a new item to the entity set or updates an existing item
        /// </summary>
        /// <remarks>
        /// <p>
        /// If <paramref name="item"/> has a non-null and non-empty <see cref="BrightstarEntityObject.Identity"/> property
        /// then any existing entity then the existing entity will be updated with all of the properties of <paramref name="item"/>.
        /// If <paramref name="item"/> has a null or empty <see cref="BrightstarEntityObject.Identity"/> property
        /// then a new entity will be added and the Identity of <paramref name="item"/> updated accordingly.
        /// </p>
        /// </remarks>
        /// <param name="item">The item to be added or updated</param>
        /// <exception cref="EntityFrameworkException">Throw if <paramref name="item"/> is attached to a context different to the one that this entity set is attached to</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="item"/> is not an instance of a class derived from <see cref="BrightstarEntityObject"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is null</exception>
        void AddOrUpdate(T item);

        /// <summary>
        /// Adds or updates all members of <paramref name="items"/>.
        /// </summary>
        /// <remarks>This is a convenience method that is equivalent to calling the <see cref="AddOrUpdate"/> method for all members of <paramref name="items"/>.</remarks>
        /// <param name="items">An enumeration yielding the items to be added or updated.</param>
        /// <exception cref="EntityFrameworkException">Throw if one of the <paramref name="items"/> is attached to a context different to the one that this entity set is attached to</exception>
        /// <exception cref="ArgumentException">Thrown if one of the <paramref name="items"/> is not an instance of a class derived from <see cref="BrightstarEntityObject"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is null or one of its members is NULL</exception>
        void AddOrUpdateRange(IEnumerable<T> items);

    }

    /// <summary>
    /// This is just a non-generic marker interface for the generic
    /// <see cref="IEntitySet{T}"/> interface
    /// </summary>
    public interface IEntitySet
    {
        
    }
}
