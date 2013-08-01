using System;
using System.Text;

namespace BrightstarDB
{
    /// <summary>
    /// A local implementation of the .NET framework System.ServiceModel.ExceptionDetail class
    /// to allow BrightstarDB mobile apps to use the same client API as desktop apps.
    /// </summary>
    public class ExceptionDetail
    {
        /// <summary>
        /// The type string for the exception passed to the constructor
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// The inner exception information
        /// </summary>
        public ExceptionDetail InnerException { get; private set; }

        /// <summary>
        /// The stack trace of the exception
        /// </summary>
        public string StackTrace { get; private set; }

        /// <summary>
        /// The exception message
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionDetail"/> class from the exception
        /// </summary>
        /// <param name="exception">The exception to be serialized as an <see cref="ExceptionDetail"/> object.  </param>
        public ExceptionDetail(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");
            Type = exception.GetType().ToString();
            if (exception.InnerException != null)
            {
                InnerException = new ExceptionDetail(exception.InnerException);
            }
            StackTrace = exception.StackTrace;
            Message = exception.Message;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        private string ToString(int indent)
        {
            var indentString = new string(' ', indent*4);
            StringBuilder exceptionDetail = new StringBuilder();
            exceptionDetail.Append(indentString);
            exceptionDetail.Append(Type);
            if (!String.IsNullOrEmpty(Message))
            {
                exceptionDetail.Append(": ");
                exceptionDetail.Append(Message);
            }
            if (!String.IsNullOrEmpty(StackTrace))
            {
                exceptionDetail.Append("\r\n");
                exceptionDetail.Append(indentString);
                exceptionDetail.Append(StackTrace);
            }
            if (InnerException != null)
            {
                exceptionDetail.Append("\r\n");
                exceptionDetail.Append(indent);
                exceptionDetail.Append("Cause:\r\n");
                exceptionDetail.Append(InnerException.ToString(indent + 1));
            }
            return exceptionDetail.ToString();
        }
    }
}