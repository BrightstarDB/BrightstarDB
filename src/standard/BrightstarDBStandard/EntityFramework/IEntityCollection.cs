using System.Collections.Generic;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// The interface for a collection of entities that appear as the value of a property on an entity instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityCollection<T> : ICollection<T>, IEntityCollection
    {
        /// <summary>
        /// Updates this collection to only contain the specified items
        /// </summary>
        /// <param name="items"></param>
        void Set(ICollection<T> items);
    }

    /// <summary>
    /// The non-generic base for the <see cref="IEntityCollection{T}"/> interface
    /// </summary>
    public interface IEntityCollection 
    {
        /// <summary>
        /// Returns true if the entity collection is already loaded
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Loads the entity collection from the store
        /// </summary>
        void Load();
    }
}