using System.Collections.Generic;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// A dynamically loaded collection of Brightstar Entity Framework instances
    /// </summary>
    public interface IBrightstarEntityCollection
    {
        /// <summary>
        /// Get an enumeration over the loaded Brightstar EntityFramework objects managed by this collection
        /// </summary>
        /// <remarks>If objects are not yet loaded, this property returns an empty enumeration. Use <see cref="IsLoaded"/> to check if objects are loaded.</remarks>
        IEnumerable<BrightstarEntityObject> LoadedObjects { get; }

        /// <summary>
        /// Returns the number of loaded objects managed by this collection
        /// </summary>
        /// <remarks>Returns zero if the collection is not yet loaded. Use the method <see cref="IsLoaded"/> to check 
        /// whether the object collection is loaded or not.</remarks>
        int LoadedObjectsCount { get; }

        /// <summary>
        /// Returns a flag indicating if the object collection is currently loaded
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        void AddToLoadedObjects(BrightstarEntityObject o);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="identity"></param>
        void RemoveFromLoadedObjects(string identity);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        void SetLoadedObjects(IEnumerable<BrightstarEntityObject> entities);

        /// <summary>
        /// Get the URI identifier of the parent of the collection
        /// </summary>
        string ParentIdentity { get; }

        /// <summary>
        /// Get the URI identifier of the property used to build the collection
        /// </summary>
        string PropertyIdentity { get; }

        /// <summary>
        /// Get the boolean flag that indicates if the collection is built from an inverse property
        /// </summary>
        bool IsInverseProperty { get; }
    }
}