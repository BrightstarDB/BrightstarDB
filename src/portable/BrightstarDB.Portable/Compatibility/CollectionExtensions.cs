using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BrightstarDB.Portable.Compatibility
{
    public static class CollectionExtensions
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
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (test(list[i]))
                {
                    list.RemoveAt(i);
                    count++;
                }
            }
            return count;
        }

        public static ReadOnlyCollection<T> AsReadOnly<T>(this IList<T> list)
        {
            return new ReadOnlyCollection<T>(list);
        }
    }
}
