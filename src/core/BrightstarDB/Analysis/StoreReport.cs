using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BrightstarDB.Analysis
{
    /// <summary>
    /// Manages crawler information about a Brightstar store
    /// </summary>
    [XmlRoot("StoreReport")]
    public class StoreReport
    {

        /// <summary>
        /// The path to the store directory
        /// </summary>
        [XmlAttribute]
        public String StorePath { get; set; }
        
        /// <summary>
        /// The date/time when this report was generated
        /// </summary>
        [XmlAttribute]
        public DateTime ReportTimestamp { get; set; }
        
        /// <summary>
        /// The ID of the store root object
        /// </summary>
        [XmlAttribute]
        public ulong StoreId { get; set; }
        
        /// <summary>
        /// The next object ID to be assigned
        /// </summary>
        [XmlAttribute]
        public ulong NextObjectId { get; set; }

        /// <summary>
        /// The reports for each of the BTree indexes in the store
        /// </summary>
        [XmlElement(typeof(BTreeReport), ElementName = "BTree")]
        public List<BTreeReport> BTrees { get; set; }

        /// <summary>
        /// The timestamp for the last commit on the store at the time the report was generated
        /// </summary>
        public DateTime LastCommitTimestamp { get; set; }

        /// <summary>
        /// The number of distinct predicates managed by the store
        /// </summary>
        public int PredicateCount { get; set; }

        /// <summary>
        /// Creates a new empty report
        /// </summary>
        public StoreReport(){ BTrees = new List<BTreeReport>();}

        /// <summary>
        /// Creates a new initialized report
        /// </summary>
        /// <param name="storePath"></param>
        /// <param name="now"></param>
        /// <param name="storeId"></param>
        /// <param name="nextObjectId"></param>
        /// <param name="commitTime"></param>
        public StoreReport(string storePath, DateTime now, ulong storeId, ulong nextObjectId, DateTime commitTime)
        {
            StorePath = storePath;
            ReportTimestamp = now;
            StoreId = storeId;
            NextObjectId = nextObjectId;
            LastCommitTimestamp = commitTime;
            BTrees = new List<BTreeReport>();
        }
    }
}