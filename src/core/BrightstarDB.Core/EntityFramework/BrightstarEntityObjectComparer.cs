using System.Collections.Generic;

namespace BrightstarDB.EntityFramework
{
    internal class BrightstarEntityObjectComparer : IEqualityComparer<BrightstarEntityObject>
    {
        #region Implementation of IEqualityComparer<in BrightstarEntityObject>

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        /// <param name="x">The first object of type <see cref="BrightstarEntityObject"/> to compare.</param>
        /// <param name="y">The second object of type <see cref="BrightstarEntityObject"/> to compare.</param>
        public bool Equals(BrightstarEntityObject x, BrightstarEntityObject y)
        {
            return x.DataObject.Identity.Equals(y.DataObject.Identity);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param><exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
        public int GetHashCode(BrightstarEntityObject obj)
        {
            return obj.DataObject.Identity.GetHashCode();
        }

        #endregion
    }
}
