using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Utils
{
    
    internal static class IEnumeratorExtensions
    {
        /// <summary>
        /// Retrieves up to <paramref name="max"/> elements from the enumerator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerator"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static IEnumerable<T> Next<T>(this IEnumerator<T> enumerator, int max)
        {
            int count = 0;
            while(count < max && enumerator.MoveNext())
            {
                yield return enumerator.Current;
                count++;
            }
        }
    }
}
