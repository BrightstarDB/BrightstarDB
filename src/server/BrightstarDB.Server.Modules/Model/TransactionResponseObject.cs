using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrightstarDB.Server.Modules.Model
{
    public class TransactionResponseObject
    {
        /// <summary>
        /// Get the name of the store that this transaction applies to
        /// </summary>
        public string StoreName { get; set; }

        /// <summary>
        /// Get the store-unique identifier for this transaction
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Get the type of transaction
        /// </summary>
        public string TransactionType { get; set; }

        /// <summary>
        /// Get the status of the transaction
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Get the unique identifier of the job that processed this transaction
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// Get the date/time when processing started on the transaction
        /// </summary>
        public DateTime StartTime { get; set; }
    }
}
