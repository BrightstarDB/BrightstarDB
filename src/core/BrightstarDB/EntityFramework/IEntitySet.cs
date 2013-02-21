using System.Linq;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// The base interface for the collections of entities provided by a <see cref="EntityContext"/>
    /// </summary>
    /// <typeparam name="T">The type of entity in the collection</typeparam>
    [DoNotObfuscateType, DoNotPruneType]
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
        void Add(T item);

        /// <summary>
        /// Adds a new item to the entity set, attaching it to the specified resource address
        /// </summary>
        /// <param name="item">The item to be added</param>
        /// <param name="resourceAddress">The resource address that the item is to be attached to</param>
        void Add(T item, string resourceAddress);
    }

    /// <summary>
    /// This is just a non-generic marker interface for the generic
    /// <see cref="IEntitySet{T}"/> interface
    /// </summary>
    public interface IEntitySet
    {
        
    }
}
