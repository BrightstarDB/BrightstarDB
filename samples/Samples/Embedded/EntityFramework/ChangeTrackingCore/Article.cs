using System;

namespace BrightstarDB.Samples.EntityFramework.ChangeTrackingCore
{
    internal partial class Article
    {
        public override string ToString()
        {
            return String.Format("Article: '{0}'. Created: {1}. Last Modified {2}", Title, Created, LastModified);
        }
    }
}
