using System;
using System.Linq;

namespace BrightstarDB.Storage
{
    internal static class ByteArrayHelper
    {
        internal static void ToByteArray(ulong [] ulongs, byte[] bytes, int offset, int byteCount)
        {
#if UNSAFE
            unsafe
            {
                fixed(ulong * src = ulongs)
                {
                    Marshal.Copy(new IntPtr((void*)src), bytes,  offset, byteCount);
                }
            }
#else
            for(int i = 0; i < byteCount/8; i++)
            {
                BitConverter.GetBytes(ulongs[i]).CopyTo(bytes, offset + i*8);
            }
#endif
        }

        internal static ulong[] ToUlongArray(byte[] bytes, int offset, int ulongCount)
        {
            var ret = new ulong[ulongCount];
            for(int i = 0; i < ulongCount; i++)
            {
                ret[i] = BitConverter.ToUInt64(bytes, offset + i*8);
            }
            return ret;
        }

        /// <summary>
        /// Copys the contents of a 2 dimensional byte array out to a fixed-length sequence of bytes
        /// </summary>
        /// <param name="sourceArrays">The arrays to be copied</param>
        /// <param name="destinationArray">The array to copy into</param>
        /// <param name="destinationOffset">The offset in the destination array to copy to</param>
        /// <param name="sourceArrayCount">The number of source arrays to be copied</param>
        /// <param name="sourceArrayLength">The number of bytes to copy from each source array</param>
        internal static void MultiCopy(byte[][] sourceArrays, byte[] destinationArray, int destinationOffset, int sourceArrayCount, int sourceArrayLength)
        {
            for(int i = 0; i < sourceArrayCount; i++)
            {
                Array.Copy(sourceArrays[i], 0, destinationArray, destinationOffset + (i*sourceArrayLength), sourceArrayLength);
            }
        }

        internal static string Dump(this byte[] array)
        {
            if (array == null)
            {
                return "[null]";
            }
            return "[" + String.Join("", array.Select(x=>x.ToString("X2"))) + "]";
        }

        public static void Increment(byte[] mergedNodeKey)
        {
            for(int ix = 0; ix < mergedNodeKey.Length; ix++)
            {
                if (mergedNodeKey[ix] < 255)
                {
                    mergedNodeKey[ix]++;
                    return;
                }
                mergedNodeKey[ix] = 0;
            }
        }
    }
}
