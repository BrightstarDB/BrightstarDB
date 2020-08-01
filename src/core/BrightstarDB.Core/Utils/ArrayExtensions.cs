namespace BrightstarDB.Utils
{
    internal static class ArrayExtensions
    {
        /// <summary>
        /// Shifts a range of items in the array down one place
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arry">The array to be modified</param>
        /// <param name="removedItemIndex">The index of the item to be overwritten by this operation, all items one or more places higher than this will shift down one place</param>
        public static void ShiftDown<T>(this T[] arry, int removedItemIndex)
        {
            for(int i = removedItemIndex; i < (arry.Length - 1); i++)
            {
                arry[i] = arry[i + 1];
            }
        }

        public static int Compare(this byte[]arry, byte[] other)
        {
            int ret = 0;
            for(int i =arry.Length-1; i >=0 && ret == 0; i--)
            {
                ret = arry[i] - other[i];
            }
            return ret;
        }

        public static int Compare(this byte[] arry, int offset, byte[] other, int otherOffset, int len)
        {
            int ret = 0;
            for (int i = len - 1; i >= 0 && ret == 0; i--)
            {
                ret = arry[i + offset] - other[i + otherOffset];
            }
            return ret;
        }
    }
}
