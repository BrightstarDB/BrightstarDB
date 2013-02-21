using SmartAssembly.Attributes;

namespace BrightstarDB
{
    ///<summary>
    /// Exception raised when an IfNotModifiedSince parameter is provided to a call to ExecuteQuery
    /// and the store has not been updated since the time specified in that parameter.
    ///</summary>
    [DoNotObfuscate]
    public class BrightstarStoreNotModifiedException : BrightstarException
    {
        internal BrightstarStoreNotModifiedException() : base ("Store not modified"){}
    }
}