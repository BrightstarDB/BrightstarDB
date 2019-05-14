using System;

namespace BrightstarDB.Rdf
{
    /// <summary>
    /// Class of exception raised if the triple sink that the parser is connected to throws an error
    /// </summary>
    /// <remarks>This class is just used internally to differentiate between parser errors caused by syntax problems
    /// and parser errors caused by the sink throwing an exception. This exception should not be thrown outside
    /// of this assembly and instead caught and its inner exception rethrown.</remarks>
    internal sealed class TripleSinkException : Exception
    {
        internal TripleSinkException(Exception inner) : base("Error raised in triple sink", inner)
        {
        }
    }
}
