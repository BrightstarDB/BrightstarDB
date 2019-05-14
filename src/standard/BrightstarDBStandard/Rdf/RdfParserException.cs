using System;
using System.Text;

namespace BrightstarDB.Rdf
{
    /// <summary>
    /// Class of exception thrown by the NetworkedPlanet RDF parsers
    /// </summary>
    public sealed class RdfParserException : Exception
    {
        /// <summary>
        /// The line number in the input stream where the parser error occurred
        /// </summary>
        public int LineNumber { get; private set; }

        /// <summary>
        /// A boolean flag that indicates if the <see cref="LineNumber"/> property
        /// has been set to the number of the line where the parser error occurred.
        /// </summary>
        public bool HaveLineNumber { get; private set; }

        internal RdfParserException(string msg) : base(msg){}

        internal RdfParserException(int lineNumber, string msg, Exception inner = null) : base(msg, inner)
        {
            HaveLineNumber = true;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <returns>
        /// The error message that explains the reason for the exception, or an empty string("").
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override string Message
        {
            get
            {
                var msg = new StringBuilder();
                msg.Append("RDF parser error");
                if (HaveLineNumber)
                {
                    msg.Append(" at line ");
                    msg.Append(LineNumber);
                }
                msg.Append(": ");
                msg.Append(base.Message);
                if (InnerException != null)
                {
                    msg.Append(" : ");
                    msg.Append(InnerException.Message);
                }
                return msg.ToString();
            }
        }

        internal RdfParserException(string msg, Exception inner) : base(msg, inner){}
    }
}
