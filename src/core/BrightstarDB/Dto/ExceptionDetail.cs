using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Dto
{
    /// <summary>
    /// Represents complete service exception information
    /// </summary>
    /// <remarks>This class is used in preference to System.ServiceModel.ExceptionDetail to provide
    /// portability across all target platforms.</remarks>
    public class ExceptionDetailObject
    {
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Provided only for serialization purposes")]
        public ExceptionDetailObject()
        {
            
        }
        /// <summary>
        /// Creates a new instance of the <see cref="ExceptionDetailObject"/> class from the exception.
        /// </summary>
        /// <param name="exception">The exception to be serialized as an <see cref="ExceptionDetailObject"/></param>
        /// <exception cref="ArgumentNullException">The <paramref name="exception"/> parameter is null</exception>
        public ExceptionDetailObject(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");
            Type = exception.GetType().ToString();
            Message = exception.Message;
            StackTrace = exception.StackTrace;
#if !WINDOWS_PHONE && !PORTABLE
            HelpLink = exception.HelpLink;
#endif
            if (exception.InnerException != null)
            {
                InnerException = new ExceptionDetailObject(exception.InnerException);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ExceptionDetailObject"/> class from the exception.
        /// </summary>
        /// <param name="exception">The exception to be serialized as an <see cref="ExceptionDetailObject"/></param>
        /// <param name="data">Additional data to serialize with the type and message of the exception.</param>
        public ExceptionDetailObject(Exception exception, Dictionary<string, string> data) : this(exception)
        {
            Data = new Dictionary<string, string>(data);
        }

        /// <summary>
        /// Get or set the type string for the exception 
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Get or set the message from the exception
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Get or set the stacktrace from the exception
        /// </summary>
        public string StackTrace { get; set; }
        /// <summary>
        /// Get or set the help link from the exception
        /// </summary>
        public string HelpLink { get; set; }

        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, string> Data { get; set; }

        /// <summary>
        /// Get or set the <see cref="ExceptionDetailObject"/> that represents the inner exception.
        /// </summary>
        public ExceptionDetailObject InnerException { get; set; }

        /// <summary>
        /// Get a string representation of the exception detail and inner exception detail (if any)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            this.BuildString(sb, 0);
            return sb.ToString();
        }

        private const string SectionSeparator = "----------";
        private void BuildString(StringBuilder sb, int indentLevel)
        {
            var prefix = indentLevel == 0 ? String.Empty : new string('\t', indentLevel);
            sb.Append(prefix);
            sb.Append(Type);
            sb.Append(": ");
            sb.AppendLine(Message);
            if (!String.IsNullOrEmpty(StackTrace))
            {
                sb.Append(prefix);
                sb.AppendLine(StackTrace);
            }
            if (Data != null && Data.Count > 0)
            {
                sb.AppendLine(SectionSeparator);
                sb.Append(prefix);
                sb.AppendLine("Data:");
                foreach (var kv in Data.OrderBy(x => x.Key))
                {
                    sb.Append(prefix);
                    sb.Append(kv.Key);
                    sb.Append(" = ");
                    sb.AppendLine(kv.Value);
                }
                sb.AppendLine(SectionSeparator);
            }
            if (InnerException != null)
            {
                sb.AppendLine(SectionSeparator);
                InnerException.BuildString(sb, indentLevel+1);
            }
        }
    }
}
