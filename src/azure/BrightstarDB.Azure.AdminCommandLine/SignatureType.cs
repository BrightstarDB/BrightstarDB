namespace BrightstarDB.Azure.AdminCommandLine
{
    /// <summary>
    /// Enumeration of the types of signature that can be applied to a B* REST API request
    /// </summary>
    public enum SignatureType
    {
        /// <summary>
        /// Unknown signature type - used to represent a failure to parse a request header correctly
        /// </summary>
        Unknown,
        /// <summary>
        /// SharedKey signature - uses the shared secrect key to generate a message signature that is included in the request header
        /// </summary>
        SharedKey,
        /// <summary>
        /// PlainText signature - this exposes the shared secret key as a request header and should only be used over HTTPS connections
        /// </summary>
        PlainText
    }
}
