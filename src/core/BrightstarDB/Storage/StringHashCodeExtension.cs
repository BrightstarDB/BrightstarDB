namespace BrightstarDB.Storage
{
    internal static class StringHashCodeExtension
    {
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
