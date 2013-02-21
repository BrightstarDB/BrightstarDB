using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BrightstarDB.EntityFramework.Query;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Class used to provide access to all domain objects of a particular type from
    /// within a domain context object
    /// </summary>
    /// <typeparam name="T">The type of domain object that this set provides access to</typeparam>
    public class BrightstarEntitySet<T> : EntityFrameworkQueryable<T>, IEntitySet<T> where T: class
    {
        private readonly BrightstarEntityContext _context;

        /// <summary>
        /// Creates a new entity set attached to the specified context
        /// </summary>
        /// <param name="context">The parent context for the entity set. Must be an instance of <see cref="BrightstarEntityContext"/>.</param>
        public BrightstarEntitySet(EntityContext context) : base(context)
        {
            if (context == null) throw new ArgumentNullException("context");
            _context = context as BrightstarEntityContext;
            if (_context == null)
            {
                throw new ArgumentException("A BrightstarEntitySet must be attached to a BrightstarEntityContext");
            }
        }

        ///<summary>
        /// Creates a new entity set connected to a LINQ expression and query provider
        ///</summary>
        ///<param name="provider">The LINQ query provider</param>
        ///<param name="expression">The LINQ expression to evaluate</param>
        public BrightstarEntitySet(IQueryProvider provider, Expression expression) : base(provider, expression)
        {
        }

        /// <summary>
        /// Creates a new instance and adds it to this set
        /// </summary>
        /// <returns></returns>
        public T Create()
        {
            return _context.CreateObject<T>();
        }

        /// <summary>
        /// Adds a new entity to the store collection
        /// </summary>
        /// <param name="item">The new entity to be added</param>
        /// <exception cref="EntityFrameworkException">Throw in <paramref name="item"/> is attached to a context different to the one that this entity set is attached to</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="item"/> is not an instance of a class derived from <see cref="BrightstarEntityObject"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is NULL</exception>
        public void Add(T item)
        {
            Add(item, null);
        }

        /// <summary>
        /// Adds a collection of entities to the store collection
        /// </summary>
        /// <param name="items">The entities to be added</param>
        /// <exception cref="EntityFrameworkException">Throw if one of the <paramref name="items"/> is attached to a context different to the one that this entity set is attached to</exception>
        /// <exception cref="ArgumentException">Thrown if one of the <paramref name="items"/> is not an instance of a class derived from <see cref="BrightstarEntityObject"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is null or one of its members is NULL</exception>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            foreach(var t in items) Add(t, null);
        }

        /// <summary>
        /// Adds a new item to the entity set, attaching it to the specified resource address
        /// </summary>
        /// <param name="item">The item to be added</param>
        /// <param name="resourceAddress">The resource address that the item is to be attached to</param>
        public void Add(T item, string resourceAddress)
        {
            if (item == null) throw new ArgumentNullException("item");
            var beo = item as BrightstarEntityObject;
            if(beo == null)
            {
                throw new EntityFrameworkException("Only items of type {0} can be added to an BrightstarEntitySet", typeof(BrightstarEntityObject).FullName);
            }
            if (beo.IsAttached)
            {
                if (!beo.Context.Equals(_context))
                {
                    throw new EntityFrameworkException(
                        "Object is already attached to a different context. It must be detached from its current context before adding it to a new context.");
                }
            }
            else
            {
                beo.AssertIdentity(resourceAddress);
                beo.Attach(_context);
            }
        }
    }
}
