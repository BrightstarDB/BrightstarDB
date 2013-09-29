using System;

namespace BrightstarDB.Server.Modules.Model
{
    public class StoreResponseObject
    {
        /// <summary>
        /// Get or set the store name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or set the relative path to the commits resource
        /// </summary>
        public string Commits { get; set; }

        /// <summary>
        /// Get or set the relative path to the jobs resource
        /// </summary>
        public string Jobs { get; set; }

        /// <summary>
        /// Get or set the relative path to the transactions resource
        /// </summary>
        public string Transactions { get; set; }

        /// <summary>
        /// Get or set the relative path to the statistics resource
        /// </summary>
        public string Statistics { get; set; }

        public StoreResponseObject(){}

        public StoreResponseObject(string storeName)
        {
            if (storeName == null) throw new ArgumentNullException("storeName");
            if (String.IsNullOrWhiteSpace(storeName)) throw new ArgumentException("storeName");
            Name = storeName;
            Commits = String.Format("{0}/commits", storeName);
            Jobs = String.Format("{0}/jobs", storeName);
            Transactions = String.Format("{0}/transactions", storeName);
            Statistics = String.Format("{0}/statistics", storeName);
        }
    }
}
