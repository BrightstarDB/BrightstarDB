using System;

namespace BrightstarDB.Service
{
#if !SILVERLIGHT
    /// <summary>
    /// Exception raised for missing or invalid configuration of a BrighstarDB server
    /// </summary>
    [Serializable]
#endif
    internal class BrighstarConfigurationException : BrightstarException
    {
        public BrighstarConfigurationException(string msg) : base(msg)
        {
            
        }
    }
}