using System;
using System.Text;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Object used to cache SPARQL query results.
    /// </summary>
    public class CachedQueryResult
    {
        /// <summary>
        /// Timestamp for the cached results object
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// The cached results serialized as a string
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Creates a new cached result
        /// </summary>
        /// <param name="timestamp">The timestamp for the result</param>
        /// <param name="result">The result serialized as a string</param>
        public CachedQueryResult(DateTime timestamp, string result)
        {
            Timestamp = timestamp;
            Result = result;
        }

        /// <summary>
        /// Deserializes a byte array and recreates the original CachedQueryResult object
        /// </summary>
        /// <param name="bytes"></param>
        /// <remarks>The array is expected to consist of a 64-bit signed integer that deserializes as a DateTime value, followed by a UTF-8 encoded string</remarks>
        /// <returns></returns>
        public static CachedQueryResult FromBinary(byte[] bytes)
        {
            var timestamp = DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));
            var result = Encoding.UTF8.GetString(bytes, 8, bytes.Length - 8);
            return new CachedQueryResult(timestamp, result);
        }
        /// <summary>
        /// Return a byte array containing the timestamp and result string.
        /// </summary>
        /// <remarks>The first 8 bytes of the byte array contain the timestamp as a Ticks value (long). The remainder of the byte array is the UTF-8 encoded result string.</remarks>
        /// <returns></returns>
        public byte[] ToBinary()
        {
            var resultByteSize = Encoding.UTF8.GetByteCount(Result);
            BitConverter.GetBytes(Timestamp.ToBinary());
            byte[] buff = new byte[resultByteSize+8];
            Array.Copy(BitConverter.GetBytes(Timestamp.Ticks), buff, 8);
            Encoding.UTF8.GetBytes(Result, 0, Result.Length, buff, 8);
            return buff;
        }
    }
}
