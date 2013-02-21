using System;
using VDS.RDF;

namespace BrightstarDB.Query
{
#if !SILVERLIGHT
    [Serializable]
#endif
    internal class BrightstarUriNode : UriNode
    {
        public BrightstarUriNode(Uri resource) : base(null, resource)
        {
        }

        public override int CompareTo(IUriNode other)
        {
            return Uri.ToString().CompareTo(other.Uri.ToString());
        }

        public override bool Equals(IUriNode other)
        {
            return Uri.Equals(other.Uri);
        }

        /*
        public override int GetHashCode()
        {
            return Uri.GetHashCode();
        }
         */
    }
}