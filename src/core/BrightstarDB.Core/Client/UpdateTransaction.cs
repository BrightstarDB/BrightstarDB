namespace BrightstarDB.Client
{
    /// <summary>
    /// Structure that encapsulates the data passed in a conditional update transaction
    /// </summary>
    public class UpdateTransactionData
    {
        /// <summary>
        /// The NTriple/NQuads patterns that MUST exist in the store for the update to be applied
        /// </summary>
        public string ExistencePreconditions { get; set; }
        /// <summary>
        /// The NTriples/NQuads patterns that MUST NOT exist in the store for the update to be applied
        /// </summary>
        public string NonexistencePreconditions { get; set; }

        /// <summary>
        /// The NTriples/NQuads patterns to be removed from the store
        /// </summary>
        public string DeletePatterns { get; set; }

        /// <summary>
        /// The NTriples/NQuads to be added to the store
        /// </summary>
        public string InsertData { get; set; }

        /// <summary>
        /// The default graph to use to convert NTriples to NQuads for the update
        /// </summary>
        public string DefaultGraphUri { get; set; }
    }
}
