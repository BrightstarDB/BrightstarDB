namespace BrightstarDB.Utils
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Calculate a 4-byte hash code for a string
        /// </summary>
        /// <remarks>This extension method provides a consistent hash code algorithm across different .NET platforms and versions where
        /// the built-in hashcode method may vary.</remarks>
        /// <param name="theString">The string to be hashed</param>
        /// <returns>The string hashcode</returns>
        public static uint GetBrightstarHashCode(this string theString)
        {
            // djb2 variant
            uint h = 5381;
            var arry = theString.ToCharArray();
            for (int i = 0; i < arry.Length; i++)
            {
                h = (h << 5) + h ^ arry[i];
            }
            return h;
        }
    }
}
