using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrightstarDB.Server.Modules.Model
{
    public class CommitPointResponseObject
    {
        public ulong Id { get; set; }
        public string StoreName { get; set; }
        public DateTime CommitTime { get; set; }
        public Guid JobId { get; set; }
    }
}
