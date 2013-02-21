using System.Web.Script.Serialization;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Structure representing the source for an import or export job
    /// </summary>
    public class BlobImportSource
    {
        /// <summary>
        /// Blob storage connection string.
        /// </summary>
        /// <remarks>Required if retrieving data from / writing data to a non-public blob</remarks>
        public string ConnectionString { get; set; }
        /// <summary>
        /// The URI of the resource to read from / write to
        /// </summary>
        public string BlobUri { get; set; }
        /// <summary>
        /// Set to true to decompress when reading and compress when writing
        /// </summary>
        public bool IsGZiped { get; set; }

        /// <summary>
        /// The URI identifier of the graph to import into or export from
        /// </summary>
        public string Graph { get; set; }

        /// <summary>
        /// Returns a JSON representation of this structure
        /// </summary>
        /// <returns></returns>
        public string ToJsonString()
        {
            var ser = new JavaScriptSerializer();
            return ser.Serialize(this);
        }
    }
}