namespace BrightstarDB.Client
{
    /// <summary>
    /// Class of exception raised when an attempt is made to save changes on
    /// a BrightstarDB store that is marked as read-only.
    /// </summary>
    public class BrightstarStoreIsReadOnlyException : BrightstarClientException
    {
        internal BrightstarStoreIsReadOnlyException() : base(Strings.BrightstarServiceClient_StoreIsReadOnly){}
    }
}