using System.Collections.Generic;
using BrightstarDB.Client;

namespace BrightstarDB.Dynamic
{
    /// <summary>
    /// A context that exposes brightstardb data via dynamic objects. 
    /// </summary>
    public class BrightstarDynamicContext
    {
        private readonly IDataObjectContext _context;

        /// <summary>
        /// Creates a new context with a specific IDataObjectContext
        /// </summary>
        /// <param name="context">The underlying context that this dynamic context will use.</param>
        public BrightstarDynamicContext(IDataObjectContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Open a new dynamic store
        /// </summary>
        /// <param name="storeName">The name of the store to open</param>
        /// <param name="namespaceMappings">RDF namespace mappings</param>
        /// <param name="optimisticLockingEnabled">Indicates if this context should enforce optimistic locking</param>
        /// <returns>A new DynamicStore</returns>
        public DynamicStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = new bool?())
        {
            return new DynamicStore(_context.OpenStore(storeName, namespaceMappings, optimisticLockingEnabled));
        }

        /// <summary>
        /// Checks if a named store exists
        /// </summary>
        /// <param name="storeName">The name of the store to check</param>
        /// <returns>True if the store exists otherwise false.</returns>
        public bool DoesStoreExist(string storeName)
        {
            return _context.DoesStoreExist(storeName);
        }

        /// <summary>
        /// Creates a new BrightstarDB store
        /// </summary>
        /// <param name="storeName">The name of the new store to create.</param>
        /// <param name="namespaceMappings">RDF namespace mappings</param>
        /// <param name="optimisticLockingEnabled">Indicates if this context should enforce optimistic locking</param>
        /// <returns>A DynamicStore</returns>
        public DynamicStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = new bool?())
        {
            return new DynamicStore(_context.CreateStore(storeName, namespaceMappings, optimisticLockingEnabled));
        }

        /// <summary>
        /// Deletes the named store
        /// </summary>
        /// <param name="storeName">Deletes the named store</param>
        public void DeleteStore(string storeName)
        {
            _context.DeleteStore(storeName);
        }

        /// <summary>
        /// IsOptimisticLockingEnabled
        /// </summary>
        public bool OptimisticLockingEnabled
        {
            get { return _context.OptimisticLockingEnabled; }
        }
    }
}
