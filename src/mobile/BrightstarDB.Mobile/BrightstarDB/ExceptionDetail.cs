using System;

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
    }
}