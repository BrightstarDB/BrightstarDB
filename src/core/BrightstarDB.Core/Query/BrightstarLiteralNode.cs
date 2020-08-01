using System;
using BrightstarDB.Rdf;

namespace BrightstarDB.Query
{
#if !SILVERLIGHT && !PORTABLE && !NETCORE
    [Serializable]
#endif
    internal class BrightstarLiteralNode : VDS.RDF.LiteralNode
    {
        private BrightstarLiteralNode(string value, string langCode)  : base(null, value, langCode ?? String.Empty)
        {            
        }

        private BrightstarLiteralNode(string value, Uri datatype) : base(null, value, datatype){}

        public static BrightstarLiteralNode Create(string value, string datatype, string languageCode)
        {
            if (datatype == null || datatype.Equals(RdfDatatypes.PlainLiteral))
            {
                return new BrightstarLiteralNode(value, languageCode);
            }
            return new BrightstarLiteralNode(value, new Uri(datatype));
        }
    }
}