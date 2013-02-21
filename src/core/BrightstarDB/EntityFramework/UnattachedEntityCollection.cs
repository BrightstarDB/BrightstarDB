using System.Collections.Generic;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Collection class used for tracking related entities on an unattached entity object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UnattachedEntityCollection<T> :List<T>, IEntityCollection<T>
    {
        #region Implementation of IEntityCollection

        /// <summary>
        /// Updates this collection to only contain the specified items
        /// </summary>
        /// <param name="items"></param>
        public void Set(ICollection<T> items)
        {
            Clear();
            AddRange(items);
        }

        /// <summary>
        /// Returns true if the entity collection is already loaded
        /// </summary>
        public bool IsLoaded
        {
            get { return true; }
        }

        /// <summary>
        /// Loads the entity collection from the store
        /// </summary>
        public void Load()
        {
            return;
        }

        #endregion
    }
}