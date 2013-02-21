using System.Linq;
using System.Linq.Expressions;
using BrightstarDB.EntityFramework.Query;

namespace BrightstarDB.EntityFramework.Tests
{
    public class MockLdoSet<T> : EntityFrameworkQueryable<T>{
        public MockLdoSet(EntityContext context) : base(context)
        {
        }

        public MockLdoSet(IQueryProvider provider, Expression expression) : base(provider, expression)
        {
        }

    }
}
