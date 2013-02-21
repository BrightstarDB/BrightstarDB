using System.Collections.Generic;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// The base interface implemented by entity framework implementation classes
    /// </summary>
    [DoNotObfuscateType, DoNotPruneType]
    public interface IEntityObject
    {
        /// <summary>
        /// Returns true if the object is currently attached to a context
        /// </summary>
        bool IsAttached { get; }

        /// <summary>
        /// Returns true if the object has been locally modified
        /// </summary>
        /// <remarks>This flag is not set if a property marked with the <see cref="InversePropertyAttribute"/> is modified
        /// as changing this property does not actually modify the underlying RDF resource. In such cases, the value that
        /// is added to / removed from the property will be marked as modified instead.</remarks>
        bool IsModified { get; }

        /// <summary>
        /// Returns the context that the item is currently attached to
        /// </summary>
        EntityContext Context { get; }

        /// <summary>
        /// Invoked by generated class prior to changing the value of a scalar property
        /// </summary>
        /// <param name="propertyName">The name of the property being modified</param>
        /// <param name="newValue">The new value that will be assigned to the property</param>
        void ReportPropertyChanging(string propertyName, object newValue);

        /// <summary>
        /// Invoked by the generated class after the value of a scalar property has been modified
        /// </summary>
        /// <param name="propertyName">The name of the property that has been modified</param>
        void ReportPropertyChanged(string propertyName);

        /// <summary>
        /// Invoked by the generated class to change the value of a property whose type
        /// is another entity
        /// </summary>
        /// <typeparam name="T">The type of the related entity</typeparam>
        /// <param name="propertyName">The name of the property that represents the relationship</param>
        /// <param name="value">The new related entity</param>
        void SetRelatedObject<T>(string propertyName, T value) where T : class;

        /// <summary>
        /// Invoked by the generated class to retrieve the value of a property whose
        /// type is another entity
        /// </summary>
        /// <typeparam name="T">The type of the related entity</typeparam>
        /// <param name="propertyName">The name of the property that represents the relationship</param>
        /// <returns>The related entity or null if there is no related entity</returns>
        T GetRelatedObject<T>(string propertyName) where T : class;

        /// <summary>
        /// Invoked by the generated class to retrieve the collection of related entities
        /// for a specific property
        /// </summary>
        /// <typeparam name="T">The type of entity expected</typeparam>
        /// <param name="propertyName">The name of the property</param>
        /// <returns></returns>
        IEntityCollection<T> GetRelatedObjects<T>(string propertyName) where T : class;

        /// <summary>
        /// Sets the collection of related entities for a specific property
        /// </summary>
        /// <typeparam name="T">The related entity type</typeparam>
        /// <param name="propertyName">The name of the property to be updated</param>
        /// <param name="relatedObjects">The new collection of related entities</param>
        void SetRelatedObjects<T>(string propertyName, ICollection<T> relatedObjects) where T : class;

        /// <summary>
        /// Attaches the object to the specified context
        /// </summary>
        /// <param name="context"></param>
        void Attach(EntityContext context);

        /// <summary>
        /// Removes the object from its current context
        /// </summary>
        void Detach();
    }
}
