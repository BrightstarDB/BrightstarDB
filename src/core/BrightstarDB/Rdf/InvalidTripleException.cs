#if REST_CLIENT

namespace BrightstarDB.Rdf
{
    internal class InvalidTripleException : BrightstarException
    {
        public InvalidTripleException(string message) : base(message){}
    }
}
#else
using BrightstarDB.Server;

namespace BrightstarDB.Rdf
{
    internal class InvalidTripleException : BrightstarClientDataException
    {
        public InvalidTripleException(string message) : base(message)
        {
            
        }
    }
}
#endif