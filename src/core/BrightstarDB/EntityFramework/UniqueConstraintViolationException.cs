using System.Collections.Generic;
using System.Linq;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Class of exception raised when an attempt is made to create a new entity resource in a store
    /// with an identifier that matches that of another resource of the same type already in the store.
    /// </summary>
    public class UniqueConstraintViolationException : EntityFrameworkException
    {
        private readonly IList<string> _nonUniqueIdentifiers;
 
        internal UniqueConstraintViolationException() : base(Strings.EntityFramework_UniqueConstraintViolation){}
        internal UniqueConstraintViolationException(IEnumerable<string> violationIds)
            : base(Strings.EntityFramework_UniqueConstraintViolation)
        {
            _nonUniqueIdentifiers = violationIds.Distinct().ToList();
        }

        /// <summary>
        /// Gets an enumeration over the identifiers that violated unique constraints
        /// </summary>
        public IEnumerable<string> NonUniqueIdentifiers { get { return _nonUniqueIdentifiers; } }
    }
}
