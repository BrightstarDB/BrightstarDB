using System;
using System.Collections.Generic;
using BrightstarDB.Storage.BPlusTreeStore;

namespace BrightstarDB
{
    public static class SilverlightCollectionExtensions
    {
        public static int FindIndex<T>(this List<T> list, Predicate<T> test)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (test(list[i])) return i;
            }
            return -1;
        }

        public static int RemoveAll<T>(this List<T> list, Predicate<T> test)
        {
            int count = 0;
            for(int i = list.Count-1; i >= 0; i--)
            {
                if (test(list[i]))
                {
                    list.RemoveAt(i);
                    count++;
                }
            }
            return count;
        }
    }

    public static class Array
    {
        public static int IndexOf<T>(T[] array, T value)
        {
            return System.Array.IndexOf(array, value);
        }

        public static void Copy(System.Array sourceArray, System.Array destinationArray, int count)
        {
            System.Array.Copy(sourceArray, destinationArray, count);
        }

        public static void Copy(System.Array sourceArray, int sourceIndex, System.Array destinationArray, int destinationIndex, int count)
        {
            System.Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, count);
        }

        public static int BinarySearch<T>(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
            return System.Array.BinarySearch(array, index, length, value, comparer);
        }

        public static void ConstrainedCopy(System.Array source, int srcOffset, System.Array destination, int destOffset, int count)
        {
            for(int i = 0; i < count; i++)
            {
                destination.SetValue(source.GetValue(srcOffset + i), destOffset + i);
            }
        }

    }
}
