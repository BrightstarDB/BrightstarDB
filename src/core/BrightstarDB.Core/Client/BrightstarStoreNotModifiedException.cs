namespace BrightstarDB.Client
{
    ///<summary>
    /// Exception raised when an IfNotModifiedSince parameter is provided to a call to ExecuteQuery
    /// and the store has not been updated since the time specified in that parameter.
    ///</summary>
    public class BrightstarStoreNotModifiedException : BrightstarClientException
    {
        /// <summary>
        /// Create a new exception instance with a default message
        /// </summary>
        public BrightstarStoreNotModifiedException() : base ("Store not modified"){}
    }
}